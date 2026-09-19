using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FurkanTural_MailTest;

internal sealed class SmtpProbe(SmtpSettings settings, CancellationToken cancellationToken) : IDisposable
{
    private readonly SmtpSettings _settings = settings;
    private readonly CancellationToken _cancellationToken = cancellationToken;
    private readonly TcpClient _tcp = new();
    private Stream _stream = Stream.Null;
    private StreamReader _reader = StreamReader.Null;
    private SslPolicyErrors _certificateErrors;

    public IPAddress? RemoteAddress { get; private set; }

    public static async Task<IPAddress?> RunAsync(SmtpSettings settings, string? recipient, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        using var probe = new SmtpProbe(settings, cancellation.Token);

        try
        {
            await probe.RunAsync(recipient);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            Report.Fail($"Sunucu {timeout.TotalSeconds:0} saniye içinde yanıt vermedi.");
        }
        catch (Exception ex)
        {
            Report.Fail($"{ex.GetType().Name}: {ex.Message}");
        }

        return probe.RemoteAddress;
    }

    public void Dispose()
    {
        _reader.Dispose();
        _stream.Dispose();
        _tcp.Dispose();
    }

    private async Task RunAsync(string? recipient)
    {
        var host = _settings.Host.Value!;
        var port = _settings.PortNumber;
        var watch = Stopwatch.StartNew();

        try
        {
            await _tcp.ConnectAsync(host, port, _cancellationToken);
        }
        catch (SocketException ex)
        {
            Report.Fail($"{host}:{port} bağlantısı kurulamadı: {ex.Message} Port bu ağdan kapalı olabilir; bazı ağlar ve barındırıcılar posta portlarını engeller.");
            return;
        }

        if (_tcp.Client.RemoteEndPoint is IPEndPoint endPoint)
            RemoteAddress = endPoint.Address.IsIPv4MappedToIPv6 ? endPoint.Address.MapToIPv4() : endPoint.Address;

        Report.Ok($"TCP bağlantısı kuruldu: {RemoteAddress}:{port} ({watch.ElapsedMilliseconds} ms)");
        Attach(_tcp.GetStream());

        if (port == 465)
            await StartTlsAsync(host);

        var greeting = await ReadReplyAsync();
        if (greeting.Code != 220)
        {
            Report.Fail($"Sunucu karşılama yerine {greeting.Code} döndü: {greeting.Text}");
            return;
        }

        var capabilities = await HelloAsync();
        if (capabilities is null)
            return;

        if (_stream is not SslStream)
        {
            if (!Offers(capabilities, "STARTTLS"))
            {
                Report.Fail("Sunucu STARTTLS sunmuyor. EmailService EnableSsl = true ile çalıştığı için bu sunucuya posta gönderemez.");
                return;
            }

            var reply = await CommandAsync("STARTTLS");
            if (reply.Code != 220)
            {
                Report.Fail($"STARTTLS reddedildi: {reply.Code} {reply.Text}");
                return;
            }

            await StartTlsAsync(host);

            capabilities = await HelloAsync();
            if (capabilities is null)
                return;
        }

        if (await AuthenticateAsync(capabilities) && recipient is not null)
            await CheckRecipientAsync(recipient);

        await SendAsync("QUIT");
    }

    private async Task StartTlsAsync(string host)
    {
        var ssl = new SslStream(_stream, false, (_, _, _, errors) =>
        {
            _certificateErrors = errors;
            return true;
        });

        await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = host }, _cancellationToken);
        Attach(ssl);

        Report.Ok($"TLS kuruldu: {ssl.SslProtocol}, {ssl.NegotiatedCipherSuite}");

        if (ssl.RemoteCertificate is X509Certificate2 certificate)
        {
            Report.Info($"Sertifika: {certificate.Subject}; veren: {certificate.Issuer}; bitiş: {certificate.NotAfter:yyyy-MM-dd}");

            if (certificate.NotAfter < DateTime.Now.AddDays(14))
                Report.Warn($"Sertifikanın süresi {certificate.NotAfter:yyyy-MM-dd} tarihinde doluyor.");
        }

        if (_certificateErrors != SslPolicyErrors.None)
            Report.Fail($"Sertifika doğrulanamadı ({_certificateErrors}). EmailService sertifikayı doğruladığı için bu sunucuya bağlanırken aynı hatayla düşer.");
    }

    private async Task<IReadOnlyList<string>?> HelloAsync()
    {
        var reply = await CommandAsync($"EHLO {Dns.GetHostName()}");
        if (reply.Code == 250)
            return reply.Lines.Skip(1).Select(x => x.ToUpperInvariant()).ToList();

        Report.Fail($"EHLO reddedildi: {reply.Code} {reply.Text}");
        return null;
    }

    private async Task<bool> AuthenticateAsync(IReadOnlyList<string> capabilities)
    {
        var mechanisms = capabilities
            .Where(x => x.StartsWith("AUTH ", StringComparison.Ordinal) || x.StartsWith("AUTH=", StringComparison.Ordinal))
            .SelectMany(x => x[5..].Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Distinct()
            .ToList();

        if (mechanisms.Count == 0)
        {
            Report.Fail("Sunucu kimlik doğrulama (AUTH) sunmuyor; EmailService kullanıcı adı ve parolayla giriş yapamaz.");
            return false;
        }

        Report.Info($"Kimlik doğrulama yöntemleri: {string.Join(", ", mechanisms)}");

        if (!mechanisms.Contains("LOGIN"))
        {
            if (mechanisms.Contains("NTLM") || mechanisms.Contains("GSSAPI"))
                Report.Warn("Sunucu LOGIN sunmuyor; SmtpClient NTLM ya da Negotiate dener. Bu adım yalnızca LOGIN'i sınar, sonucu 4. adım belirler.");
            else
                Report.Fail("Sunucu LOGIN, NTLM ya da GSSAPI sunmuyor. .NET SmtpClient yalnızca bunlarla giriş yapabildiği için EmailService giriş yapamaz.");

            return false;
        }

        var password = _settings.Password.Value;
        if (password is null)
        {
            Report.Fail("Parola olmadığı için giriş denenmedi.");
            return false;
        }

        var username = _settings.Username.Value ?? "";

        var reply = await CommandAsync("AUTH LOGIN");
        if (reply.Code == 334)
            reply = await CommandAsync(Base64(username), "(kullanıcı adı, base64)");
        if (reply.Code == 334)
            reply = await CommandAsync(Base64(password), "(parola gizlendi)");

        if (reply.Code == 235)
        {
            Report.Ok($"Giriş başarılı: {username}");
            return true;
        }

        Report.Fail($"Giriş reddedildi: {reply.Code} {reply.Text}. Kullanıcı adı ya da parola yanlış, hesap kilitli ya da SMTP erişimi kapalı olabilir.");
        return false;
    }

    private async Task CheckRecipientAsync(string recipient)
    {
        var from = _settings.From.Value;

        var reply = await CommandAsync($"MAIL FROM:<{from}>");
        if (reply.Code != 250)
        {
            Report.Fail($"Gönderen reddedildi: {reply.Code} {reply.Text}. Sunucu {from} adına göndermeye izin vermiyor olabilir.");
            return;
        }

        reply = await CommandAsync($"RCPT TO:<{recipient}>");
        if (reply.Code is 250 or 251)
            Report.Ok($"Alıcı kabul edildi: {recipient}");
        else
            Report.Fail($"Alıcı reddedildi: {reply.Code} {reply.Text}");

        await CommandAsync("RSET");
    }

    private async Task<SmtpReply> CommandAsync(string command, string? display = null)
    {
        await SendAsync(command, display);
        return await ReadReplyAsync();
    }

    private async Task SendAsync(string command, string? display = null)
    {
        Report.Trace($"→ {display ?? command}");
        await _stream.WriteAsync(Encoding.UTF8.GetBytes(command + "\r\n"), _cancellationToken);
        await _stream.FlushAsync(_cancellationToken);
    }

    private async Task<SmtpReply> ReadReplyAsync()
    {
        var lines = new List<string>();

        while (true)
        {
            var line = await _reader.ReadLineAsync(_cancellationToken) ?? throw new IOException("Sunucu bağlantıyı kapattı.");
            Report.Trace($"← {line}");

            if (line.Length < 3 || !int.TryParse(line.AsSpan(0, 3), out var code))
                throw new InvalidDataException($"Beklenmeyen yanıt: {line}");

            lines.Add(line.Length > 4 ? line[4..] : "");

            if (line.Length < 4 || line[3] != '-')
                return new SmtpReply(code, lines);
        }
    }

    private void Attach(Stream stream)
    {
        _reader.Dispose();
        _stream = stream;
        _reader = new StreamReader(stream, Encoding.UTF8, false, 1024, leaveOpen: true);
    }

    private static bool Offers(IReadOnlyList<string> capabilities, string keyword)
        => capabilities.Any(x => x == keyword || x.StartsWith(keyword + " ", StringComparison.Ordinal));

    private static string Base64(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    private sealed record SmtpReply(int Code, IReadOnlyList<string> Lines)
    {
        public string Text => string.Join(" | ", Lines);
    }
}
