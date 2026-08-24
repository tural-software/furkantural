using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Tür, proje ve şablon okumaları küresel süzgeçten geçer, dolayısıyla pasif bir satır hiç görünmez. "Tür ve proje çifti başına tek etkin şablon" kuralı veri tabanındaki süzgeçli tekil indekste durduğu için buradaki tek satırlık okumalar yeterlidir: ikinci bir etkin şablonun varlığı zaten yazma anında engellenmiştir.<para>Şablon iki adımda aranır: önce isteği yapan projenin kendi sürümü, sonra projesi boş bırakılmış genel sürüm. Tanınmayan bir kaynak adı hataya değil genel sürüme düşer — postanın hiç gitmemesindense markasız gitmesi yeğdir; ayrıca app_source claim'i istemcinin bildirdiği bir değerdir ve gönderimi ona bağlamak, yazım hatasıyla postayı susturmak demek olurdu.</para><para>SMTP hatası yakalanır ve sonuca çevrilir. İstisnayı yukarı bırakmak, postanın gönderilememesi yüzünden asıl işlemin — iletişim mesajının kaydı, hesabın açılması — düşmesi demek olurdu; hangisinin kabul edilebilir olduğuna çağıran karar verir.</para></summary>
public class MailSender(IUnitOfWork unitOfWork, IMailRenderer renderer, IEmailService emailService) : IMailSender
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMailRenderer _renderer = renderer;
    private readonly IEmailService _emailService = emailService;

    public async Task<Result> SendAsync(string typeCode, string? appSourceCode, string? toEmail, object payload, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            return Result.Fail("Posta gönderilemedi.", $"Alıcı adresi boş ({typeCode}).");

        var type = await _unitOfWork.MailTemplateTypes.GetAsync(x => x.Code == typeCode, cancellationToken);
        if (type is null)
            return Result.Fail("Posta gönderilemedi.", $"Posta türü bulunamadı ya da pasif: {typeCode}.", 500);

        var template = await ResolveTemplateAsync(type.Id, appSourceCode, cancellationToken);
        if (template is null || string.IsNullOrWhiteSpace(template.HtmlContent))
            return Result.Fail("Posta gönderilemedi.", $"{typeCode} türü için etkin şablon yok ({appSourceCode ?? "genel"}).", 500);

        var subject = _renderer.Render(template.Subject, payload);
        var body = _renderer.Render(template.HtmlContent, payload);

        try
        {
            await _emailService.SendAsync(toEmail!, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result.Fail("Posta gönderilemedi.", $"SMTP hatası ({typeCode}): {ex.Message}", 502);
        }

        return Result.Ok();
    }

    private async Task<FurkanTural_Domain.Entities.MailTemplate?> ResolveTemplateAsync(int typeId, string? appSourceCode, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(appSourceCode))
        {
            var appSource = await _unitOfWork.AppSources.GetAsync(x => x.Code == appSourceCode, cancellationToken);

            if (appSource is not null)
            {
                var owned = await _unitOfWork.MailTemplates
                    .GetAsync(x => x.MailTemplateTypeId == typeId && x.AppSourceId == appSource.Id, cancellationToken);

                if (owned is not null) return owned;
            }
        }

        return await _unitOfWork.MailTemplates
            .GetAsync(x => x.MailTemplateTypeId == typeId && x.AppSourceId == null, cancellationToken);
    }
}
