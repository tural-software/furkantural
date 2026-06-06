namespace FurkanTural_Application.Settings;

public sealed class FileStorageSettings
{
    /// <summary>
    /// Yüklemelerin kök dizini (<c>wwwroot</c>). Modül/medya-türü alt klasörleri
    /// (ör. <c>chats/videos</c>) <see cref="Services.Abstract.IFileService"/> tarafından bunun altında oluşturulur.
    /// </summary>
    public string WebRootPath { get; set; } = string.Empty;
}
