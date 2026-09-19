using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace FurkanTural_MailTest;

internal sealed class DomainCheck(HttpClient http, TimeSpan timeout)
{
    private const int LookupLimit = 10;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly Dictionary<string, int> TypeCodes = new() { ["A"] = 1, ["MX"] = 15, ["TXT"] = 16, ["AAAA"] = 28 };

    private readonly HttpClient _http = http;
    private readonly TimeSpan _timeout = timeout;
    private readonly Dictionary<string, IReadOnlyList<string>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _unevaluated = new(StringComparer.OrdinalIgnoreCase);
    private int _lookups;

    public async Task RunAsync(string domain, IReadOnlyCollection<IPAddress> serverAddresses)
    {
        try
        {
            await CheckSpfAsync(domain, serverAddresses);
            await CheckDmarcAsync(domain);
        }
        catch (HttpRequestException ex)
        {
            Report.Warn($"DNS-over-HTTPS sorgusu yapılamadı ({ex.Message}). Bu adımı atlamak için --no-dns verin.");
        }
        catch (OperationCanceledException)
        {
            Report.Warn($"DNS-over-HTTPS sorgusu {_timeout.TotalSeconds:0} saniye içinde yanıt vermedi.");
        }
    }

    private async Task CheckSpfAsync(string domain, IReadOnlyCollection<IPAddress> serverAddresses)
    {
        string? record;

        try
        {
            record = await GetSpfRecordAsync(domain);
        }
        catch (SpfError ex)
        {
            Report.Fail(ex.Message);
            return;
        }

        if (record is null)
        {
            Report.Fail($"{domain} için SPF kaydı yok. Alıcı sunucular bu alan adından gelen postayı spama atar ya da reddeder.");
            return;
        }

        Report.Ok($"SPF: {record}");

        foreach (var address in serverAddresses)
        {
            _lookups = 0;
            _unevaluated.Clear();

            try
            {
                var (result, reason) = await EvaluateAsync(domain, address, 0);

                if (result == "pass")
                    Report.Ok($"SMTP sunucusunun adresi {address} SPF'den geçiyor ({reason}).");
                else
                    Report.Warn($"SMTP sunucusunun adresi {address} SPF'den geçmiyor: {result} ({reason}). Sunucu dışarıya bu adresten çıkıyorsa alıcılar postayı reddeder ya da spama atar; başka bir adresten çıkıyor olabilir, kesin sonucu alınan postanın Authentication-Results başlığı verir.");
            }
            catch (SpfError ex)
            {
                Report.Fail($"SPF değerlendirilemedi (permerror): {ex.Message}");
            }

            if (_unevaluated.Count > 0)
                Report.Info($"Değerlendirilmeyen mekanizmalar: {string.Join(", ", _unevaluated)}");
        }
    }

    private async Task CheckDmarcAsync(string domain)
    {
        var record = (await QueryAsync($"_dmarc.{domain}", "TXT"))
            .Select(JoinTxt)
            .FirstOrDefault(x => x.StartsWith("v=DMARC1", StringComparison.OrdinalIgnoreCase));

        if (record is null)
        {
            Report.Warn($"_dmarc.{domain} kaydı yok. Zorunlu değil ama büyük posta sağlayıcıları DMARC kaydı olmayan alan adlarından gelen postaya daha az güvenir.");
            return;
        }

        Report.Ok($"DMARC: {record}");

        var policy = record.Split(';')
            .Select(x => x.Trim())
            .FirstOrDefault(x => x.StartsWith("p=", StringComparison.OrdinalIgnoreCase))?[2..]
            .ToLowerInvariant();

        if (policy is "reject" or "quarantine")
            Report.Info($"Politika \"{policy}\": SPF ya da DKIM alan adıyla hizalanmazsa posta {(policy == "reject" ? "reddedilir" : "spama düşer")}. DKIM imzası bu araçla doğrulanamaz; alınan postanın başlığına bakın.");
    }

    private async Task<(string Result, string Reason)> EvaluateAsync(string domain, IPAddress address, int depth)
    {
        if (depth > LookupLimit)
            throw new SpfError("include/redirect zinciri çok derin.");

        var record = await GetSpfRecordAsync(domain);
        if (record is null)
            return ("none", $"{domain} için SPF kaydı yok");

        string? redirect = null;

        foreach (var term in record.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1))
        {
            if (term.StartsWith("redirect=", StringComparison.OrdinalIgnoreCase))
            {
                redirect = term["redirect=".Length..];
                continue;
            }

            if (term.Contains('='))
                continue;

            var qualifier = term[0] is '+' or '-' or '~' or '?' ? term[0] : '+';
            var mechanism = term[0] == qualifier ? term[1..] : term;

            if (await MatchesAsync(domain, mechanism, address, depth))
                return (qualifier switch { '-' => "fail", '~' => "softfail", '?' => "neutral", _ => "pass" }, $"{domain} → {term}");
        }

        if (redirect is null)
            return ("neutral", $"{domain} kaydında eşleşen mekanizma yok");

        CountLookup();
        return await EvaluateAsync(redirect, address, depth + 1);
    }

    private async Task<bool> MatchesAsync(string domain, string mechanism, IPAddress address, int depth)
    {
        var colon = mechanism.IndexOf(':');
        var slash = mechanism.IndexOf('/');
        var nameEnd = colon >= 0 && (slash < 0 || colon < slash) ? colon : slash >= 0 ? slash : mechanism.Length;
        var name = mechanism[..nameEnd].ToLowerInvariant();
        var argument = nameEnd < mechanism.Length && mechanism[nameEnd] == ':' ? mechanism[(nameEnd + 1)..] : mechanism[nameEnd..];

        switch (name)
        {
            case "all":
                return true;

            case "ip4":
            case "ip6":
                return InRange(address, argument);

            case "include":
                CountLookup();
                return (await EvaluateAsync(argument, address, depth + 1)).Result == "pass";

            case "a":
            {
                CountLookup();
                var (host, prefix) = HostAndPrefix(argument, domain, address);
                return await HostMatchesAsync(host, prefix, address);
            }

            case "mx":
            {
                CountLookup();
                var (host, prefix) = HostAndPrefix(argument, domain, address);

                foreach (var exchange in await QueryAsync(host, "MX"))
                {
                    var target = exchange.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.TrimEnd('.');
                    if (target is not null && await HostMatchesAsync(target, prefix, address))
                        return true;
                }

                return false;
            }

            default:
                _unevaluated.Add(mechanism);
                return false;
        }
    }

    private async Task<bool> HostMatchesAsync(string host, int prefix, IPAddress address)
    {
        var type = address.AddressFamily == AddressFamily.InterNetworkV6 ? "AAAA" : "A";

        foreach (var data in await QueryAsync(host, type))
        {
            if (IPAddress.TryParse(data, out var candidate) && InRange(address, candidate, prefix))
                return true;
        }

        return false;
    }

    private async Task<string?> GetSpfRecordAsync(string domain)
    {
        var records = (await QueryAsync(domain, "TXT"))
            .Select(JoinTxt)
            .Where(x => x.Equals("v=spf1", StringComparison.OrdinalIgnoreCase) || x.StartsWith("v=spf1 ", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return records.Count switch
        {
            0 => null,
            1 => records[0],
            _ => throw new SpfError($"{domain} için {records.Count} ayrı SPF kaydı var; alıcılar bunu geçersiz sayar. Kayıtları tek satırda birleştirin.")
        };
    }

    private async Task<IReadOnlyList<string>> QueryAsync(string name, string type)
    {
        var key = $"{type} {name}";
        if (_cache.TryGetValue(key, out var cached))
            return cached;

        using var cancellation = new CancellationTokenSource(_timeout);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://cloudflare-dns.com/dns-query?name={Uri.EscapeDataString(name)}&type={type}");
        request.Headers.Accept.ParseAdd("application/dns-json");

        using var response = await _http.SendAsync(request, cancellation.Token);
        response.EnsureSuccessStatusCode();

        await using var body = await response.Content.ReadAsStreamAsync(cancellation.Token);
        var parsed = await JsonSerializer.DeserializeAsync<DnsResponse>(body, JsonOptions, cancellation.Token);
        var code = TypeCodes[type];

        IReadOnlyList<string> answers = parsed?.Answer?.Where(x => x.Type == code).Select(x => x.Data).ToList() ?? [];
        _cache[key] = answers;
        return answers;
    }

    private void CountLookup()
    {
        if (++_lookups > LookupLimit)
            throw new SpfError($"Değerlendirme {LookupLimit} DNS sorgusu sınırını aşıyor; alıcılar kaydı geçersiz sayar.");
    }

    private static (string Host, int Prefix) HostAndPrefix(string argument, string domain, IPAddress address)
    {
        var parts = argument.Split("//");
        var slash = parts[0].IndexOf('/');
        var host = slash >= 0 ? parts[0][..slash] : parts[0];
        var prefixV4 = slash >= 0 && int.TryParse(parts[0][(slash + 1)..], out var v4) ? v4 : 32;
        var prefixV6 = parts.Length > 1 && int.TryParse(parts[1], out var v6) ? v6 : 128;

        return (host.Length == 0 ? domain : host, address.AddressFamily == AddressFamily.InterNetworkV6 ? prefixV6 : prefixV4);
    }

    private static bool InRange(IPAddress address, string cidr)
    {
        var slash = cidr.IndexOf('/');
        if (!IPAddress.TryParse(slash >= 0 ? cidr[..slash] : cidr, out var network))
            return false;

        var bits = network.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32;
        var prefix = slash >= 0 && int.TryParse(cidr[(slash + 1)..], out var parsed) ? parsed : bits;
        return InRange(address, network, prefix);
    }

    private static bool InRange(IPAddress address, IPAddress network, int prefix)
    {
        if (address.AddressFamily != network.AddressFamily)
            return false;

        var left = address.GetAddressBytes();
        var right = network.GetAddressBytes();
        prefix = Math.Clamp(prefix, 0, left.Length * 8);

        var whole = prefix / 8;
        if (!left.AsSpan(0, whole).SequenceEqual(right.AsSpan(0, whole)))
            return false;

        var remainder = prefix % 8;
        if (remainder == 0)
            return true;

        var mask = (byte)(0xFF << (8 - remainder));
        return (left[whole] & mask) == (right[whole] & mask);
    }

    private static string JoinTxt(string data)
        => data.Contains('"') ? string.Concat(data.Split('"').Where((_, index) => index % 2 == 1)) : data;

    private sealed record DnsResponse(int Status, List<DnsAnswer>? Answer);

    private sealed record DnsAnswer(string Name, int Type, string Data);

    private sealed class SpfError(string message) : Exception(message);
}
