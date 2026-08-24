using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>Tür ve şablon okumaları küresel süzgeçten geçer, dolayısıyla pasif bir tür ya da pasif bir şablon hiç görünmez. "Tür başına tek etkin şablon" kuralı veri tabanındaki süzgeçli tekil indekste durduğu için buradaki tek satırlık okuma yeterlidir: ikinci bir etkin şablonun varlığı zaten yazma anında engellenmiştir.<para>SMTP hatası yakalanır ve sonuca çevrilir. İstisnayı yukarı bırakmak, postanın gönderilememesi yüzünden asıl işlemin — iletişim mesajının kaydı, hesabın açılması — düşmesi demek olurdu; hangisinin kabul edilebilir olduğuna çağıran karar verir.</para></summary>
public class MailSender(IUnitOfWork unitOfWork, IMailRenderer renderer, IEmailService emailService) : IMailSender
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMailRenderer _renderer = renderer;
    private readonly IEmailService _emailService = emailService;

    public async Task<Result> SendAsync(string typeCode, string? toEmail, object payload, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            return Result.Fail("Posta gönderilemedi.", $"Alıcı adresi boş ({typeCode}).");

        var type = await _unitOfWork.MailTemplateTypes.GetAsync(x => x.Code == typeCode, cancellationToken);
        if (type is null)
            return Result.Fail("Posta gönderilemedi.", $"Posta türü bulunamadı ya da pasif: {typeCode}.", 500);

        var template = await _unitOfWork.MailTemplates.GetAsync(x => x.MailTemplateTypeId == type.Id, cancellationToken);
        if (template is null || string.IsNullOrWhiteSpace(template.HtmlContent))
            return Result.Fail("Posta gönderilemedi.", $"{typeCode} türü için etkin şablon yok.", 500);

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
}
