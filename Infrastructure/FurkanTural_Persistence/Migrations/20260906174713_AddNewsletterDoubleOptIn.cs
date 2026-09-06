using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FurkanTural_Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsletterDoubleOptIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "Subscribers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubscriberVerifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubscriberId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestIpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    RequestUserAgent = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriberVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriberVerifications_Subscribers_SubscriberId",
                        column: x => x.SubscriberId,
                        principalTable: "Subscribers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "MailTemplateTypes",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsActive", "Name", "SortOrder", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 4, "NewsletterConfirm", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Bülten listesine giren adrese gönderilen tek kullanımlık doğrulama bağlantısı.", true, "Bülten — Abonelik Onayı", 4, null, null },
                    { 5, "NewsletterUnsubscribe", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Abonelikten çıkmak isteyen adrese gönderilen tek kullanımlık onay bağlantısı.", true, "Bülten — Çıkış Bağlantısı", 5, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriberVerifications_SubscriberId",
                table: "SubscriberVerifications",
                column: "SubscriberId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriberVerifications_TokenHash",
                table: "SubscriberVerifications",
                column: "TokenHash");

            migrationBuilder.Sql(TemplateSql(
                "NewsletterConfirm",
                "Bülten Onayı — Blog",
                "Aboneliğinizi doğrulayın - Furkan Tural",
                Body(
                    "Aboneliğinizi doğrulayın",
                    "{{Email}} adresiyle bülten listesine kaydolma isteği aldık. Bağlantıya tıklamadan listeye eklenmezsiniz.",
                    "Aboneliği doğrula",
                    "{{ConfirmUrl}}",
                    "Bu isteği siz yapmadıysanız hiçbir şey yapmanıza gerek yok; bağlantıya tıklanmazsa adresiniz listeye girmez.")));

            migrationBuilder.Sql(TemplateSql(
                "NewsletterUnsubscribe",
                "Bülten Çıkışı — Blog",
                "Abonelikten çıkış - Furkan Tural",
                Body(
                    "Abonelikten çıkış",
                    "{{Email}} adresi için abonelikten çıkma isteği aldık. Çıkış, aşağıdaki bağlantıya tıklandığında tamamlanır.",
                    "Abonelikten çık",
                    "{{UnsubscribeUrl}}",
                    "Bu isteği siz yapmadıysanız hiçbir şey yapmanıza gerek yok; bağlantıya tıklanmazsa aboneliğiniz sürer.")));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE t FROM [MailTemplates] t
                JOIN [MailTemplateTypes] ty ON ty.[Id] = t.[MailTemplateTypeId]
                JOIN [AppSources] a ON a.[Id] = t.[AppSourceId]
                WHERE ty.[Code] IN (N'NewsletterConfirm', N'NewsletterUnsubscribe') AND a.[Code] = N'Blog';
                """);

            migrationBuilder.DropTable(
                name: "SubscriberVerifications");

            migrationBuilder.DeleteData(
                table: "MailTemplateTypes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "MailTemplateTypes",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "Subscribers");
        }

        /// <summary>Şablonlar ham SQL ile eklenir: HtmlContent tohum verisi olarak taşınamayacak kadar büyüktür ve model anlık görüntüsünü şişirir. Ekleme koşulludur — aynı tür ve proje çifti için satır zaten varsa dokunulmaz, böylece elle düzenlenmiş bir şablonun üstüne yazılmaz.</summary>
        private static string TemplateSql(string typeCode, string name, string subject, string body) =>
            $"""
            DECLARE @typeId int = (SELECT [Id] FROM [MailTemplateTypes] WHERE [Code] = N'{typeCode}');
            DECLARE @appSourceId int = (SELECT [Id] FROM [AppSources] WHERE [Code] = N'Blog');

            IF @typeId IS NOT NULL AND @appSourceId IS NOT NULL
            AND NOT EXISTS (SELECT 1 FROM [MailTemplates] WHERE [MailTemplateTypeId] = @typeId AND [AppSourceId] = @appSourceId)
            INSERT INTO [MailTemplates]
                ([MailTemplateTypeId], [AppSourceId], [Name], [Subject], [HtmlContent], [FileName],
                 [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsActive], [IsDeleted], [DeletedAt])
            VALUES
                (@typeId, @appSourceId, N'{Escape(name)}', N'{Escape(subject)}', N'{Escape(body)}', NULL,
                 SYSUTCDATETIME(), NULL, NULL, NULL, 1, 0, NULL);
            """;

        private static string Escape(string value) => value.Replace("'", "''");

        /// <summary>Gövde iskeleti düz ham dizedir, enterpolasyonlu değil: şablondaki <c>{{Ad}}</c> yer tutucuları posta motoruna aittir ve enterpolasyonlu bir dizede C# ifadesi sanılırdı. Değişen parçalar bu yüzden yüzde işaretli işaretçilerle değiştirilir.</summary>
        private const string BodyShell = """
            <!DOCTYPE html>
            <html lang="tr">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>%HEADING%</title>
            </head>
            <body style="margin:0;padding:0;background-color:#0d1117;font-family:Arial,Helvetica,sans-serif;color:#e6edf3;">
                <table width="100%" cellpadding="0" cellspacing="0" border="0" role="presentation" style="width:100%;background-color:#0d1117;padding:40px 16px;">
                    <tr>
                        <td align="center">
                            <table width="640" cellpadding="0" cellspacing="0" border="0" role="presentation" style="width:640px;max-width:100%;background-color:#161b22;border:1px solid #232a33;border-radius:16px;overflow:hidden;">
                                <tr>
                                    <td style="padding:40px 40px 8px;">
                                        <div style="margin-bottom:32px;color:#e6edf3;font-size:18px;font-weight:800;letter-spacing:-0.5px;">Furkan<span style="color:#38bdf8;">Tural</span></div>
                                        <div style="margin-bottom:10px;color:#e6edf3;font-size:28px;line-height:36px;font-weight:700;">%HEADING%</div>
                                        <div style="color:#9aa7b4;font-size:15px;line-height:24px;">%LEAD%</div>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding:28px 40px 8px;">
                                        <a href="%URL%" style="display:inline-block;padding:14px 28px;background-color:#38bdf8;color:#0f172a;font-size:15px;font-weight:700;text-decoration:none;border-radius:10px;">%BUTTON%</a>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding:20px 40px 8px;color:#9aa7b4;font-size:13px;line-height:21px;">
                                        Düğme çalışmazsa bu adresi tarayıcınıza yapıştırın:<br>
                                        <span style="color:#38bdf8;word-break:break-all;">%URL%</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding:20px 40px 8px;color:#9aa7b4;font-size:13px;line-height:21px;">
                                        Bağlantı {{ExpiresAt}} tarihine kadar geçerlidir ve bir kez kullanılabilir.<br>
                                        %FOOTNOTE%
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding:24px 40px 36px;border-top:1px solid #232a33;color:#9aa7b4;font-size:12px;line-height:20px;">
                                        İstek {{IpAddress}} adresinden geldi.<br>
                                        Soru için: <a href="mailto:{{ContactEmail}}" style="color:#38bdf8;text-decoration:none;">{{ContactEmail}}</a><br>
                                        &copy; {{CurrentYear}} Furkan Tural
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;

        private static string Body(string heading, string lead, string button, string url, string footNote) =>
            BodyShell
                .Replace("%HEADING%", heading)
                .Replace("%LEAD%", lead)
                .Replace("%BUTTON%", button)
                .Replace("%URL%", url)
                .Replace("%FOOTNOTE%", footNote);
    }
}
