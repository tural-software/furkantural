using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FurkanTural_Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsletterIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NewsletterIssues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    QueuedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecipientCount = table.Column<int>(type: "int", nullable: false),
                    SentCount = table.Column<int>(type: "int", nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    SkippedCount = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_NewsletterIssues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewsletterDeliveries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NewsletterIssueId = table.Column<int>(type: "int", nullable: false),
                    SubscriberId = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_NewsletterDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsletterDeliveries_NewsletterIssues_NewsletterIssueId",
                        column: x => x.NewsletterIssueId,
                        principalTable: "NewsletterIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NewsletterDeliveries_Subscribers_SubscriberId",
                        column: x => x.SubscriberId,
                        principalTable: "Subscribers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterDeliveries_NewsletterIssueId_Status",
                table: "NewsletterDeliveries",
                columns: new[] { "NewsletterIssueId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterDeliveries_NewsletterIssueId_SubscriberId",
                table: "NewsletterDeliveries",
                columns: new[] { "NewsletterIssueId", "SubscriberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterDeliveries_SubscriberId",
                table: "NewsletterDeliveries",
                column: "SubscriberId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterIssues_Status",
                table: "NewsletterIssues",
                column: "Status");

            migrationBuilder.InsertData(
                table: "MailTemplateTypes",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsActive", "Name", "SortOrder", "UpdatedAt", "UpdatedBy" },
                values: new object[] { 6, "NewsletterIssue", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Doğrulanmış adreslere dağıtılan bülten sayısı. Gövde yönetim panelinde yazılır, kabuk buradan gelir.", true, "Bülten — Sayı", 6, null, null });

            migrationBuilder.Sql(TemplateSql("NewsletterIssue", "Bülten Sayısı — Blog", "{{Subject}}", BodyShell));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE t FROM [MailTemplates] t
                JOIN [MailTemplateTypes] ty ON ty.[Id] = t.[MailTemplateTypeId]
                JOIN [AppSources] a ON a.[Id] = t.[AppSourceId]
                WHERE ty.[Code] = N'NewsletterIssue' AND a.[Code] = N'Blog';
                """);

            migrationBuilder.DropTable(
                name: "NewsletterDeliveries");

            migrationBuilder.DropTable(
                name: "NewsletterIssues");

            migrationBuilder.DeleteData(
                table: "MailTemplateTypes",
                keyColumn: "Id",
                keyValue: 6);
        }

        /// <summary>Şablon ham SQL ile eklenir: HtmlContent tohum verisi olarak taşınamayacak kadar büyüktür ve model anlık görüntüsünü şişirir. Ekleme koşulludur — aynı tür ve proje çifti için satır zaten varsa dokunulmaz, böylece elle düzenlenmiş bir şablonun üstüne yazılmaz.</summary>
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

        /// <summary>Kabuk düz ham dizedir, enterpolasyonlu değil: içindeki <c>{{Ad}}</c> yer tutucuları posta motoruna aittir ve enterpolasyonlu bir dizede C# ifadesi sanılırdı.<para>Diğer bülten şablonlarından iki farkı var. Birincisi, ortada tek bir düğme değil <c>{{Body}}</c> duruyor: yazının kendisi panelde yazılır ve buraya kaçışsız yerleştirilir. İkincisi, altbilgideki çıkış bağlantısı süs değil zorunluluktur — bağlantı kabuğa gömülü olduğu için sayıyı yazan kişinin onu koymayı unutması mümkün değildir.</para></summary>
        private const string BodyShell = """
            <!DOCTYPE html>
            <html lang="tr">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>{{Subject}}</title>
            </head>
            <body style="margin:0;padding:0;background-color:#0d1117;font-family:Arial,Helvetica,sans-serif;color:#e6edf3;">
                <table width="100%" cellpadding="0" cellspacing="0" border="0" role="presentation" style="width:100%;background-color:#0d1117;padding:40px 16px;">
                    <tr>
                        <td align="center">
                            <table width="640" cellpadding="0" cellspacing="0" border="0" role="presentation" style="width:640px;max-width:100%;background-color:#161b22;border:1px solid #232a33;border-radius:16px;overflow:hidden;">
                                <tr>
                                    <td style="padding:40px 40px 8px;">
                                        <div style="margin-bottom:32px;color:#e6edf3;font-size:18px;font-weight:800;letter-spacing:-0.5px;">Furkan<span style="color:#38bdf8;">Tural</span></div>
                                        <div style="margin-bottom:24px;color:#e6edf3;font-size:28px;line-height:36px;font-weight:700;">{{Subject}}</div>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding:0 40px 8px;color:#c9d4df;font-size:15px;line-height:25px;">
                                        {{Body}}
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding:28px 40px 36px;border-top:1px solid #232a33;color:#9aa7b4;font-size:12px;line-height:20px;">
                                        Bu bülteni {{Email}} adresi bültene abone olduğu için aldınız.<br>
                                        <a href="{{UnsubscribeUrl}}" style="color:#38bdf8;text-decoration:underline;">Abonelikten çık</a><br><br>
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
    }
}
