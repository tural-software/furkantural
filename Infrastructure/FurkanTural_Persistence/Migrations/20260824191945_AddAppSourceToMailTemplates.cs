using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FurkanTural_Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppSourceToMailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MailTemplates_MailTemplateTypeId",
                table: "MailTemplates");

            migrationBuilder.AddColumn<int>(
                name: "AppSourceId",
                table: "MailTemplates",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSources", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AppSources",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "Description", "IsActive", "Name", "SortOrder", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, "Portfolio", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Genel portfolyo sitesi; iletişim formu buradadır.", true, "Portfolyo", 1, null, null },
                    { 2, "Blog", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Genel blog sitesi.", true, "Blog", 2, null, null },
                    { 3, "Chat", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Sohbet uygulaması; kullanıcı hesapları buradadır.", true, "Chatural", 3, null, null },
                    { 4, "Admin", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Yönetim paneli; app-token'ı yoktur, adı hiçbir claim'de geçmez.", true, "Yönetim Paneli", 4, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MailTemplates_AppSourceId",
                table: "MailTemplates",
                column: "AppSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_MailTemplates_MailTemplateTypeId_AppSourceId",
                table: "MailTemplates",
                columns: new[] { "MailTemplateTypeId", "AppSourceId" },
                unique: true,
                filter: "[IsActive] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppSources_Code",
                table: "AppSources",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MailTemplates_AppSources_AppSourceId",
                table: "MailTemplates",
                column: "AppSourceId",
                principalTable: "AppSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                DECLARE @typeId int = (SELECT [Id] FROM [MailTemplateTypes] WHERE [Code] = N'AccountActivation');
                DECLARE @appSourceId int = (SELECT [Id] FROM [AppSources] WHERE [Code] = N'Chat');

                IF @typeId IS NOT NULL AND @appSourceId IS NOT NULL
                AND NOT EXISTS (SELECT 1 FROM [MailTemplates] WHERE [MailTemplateTypeId] = @typeId AND [AppSourceId] = @appSourceId)
                INSERT INTO [MailTemplates]
                    ([MailTemplateTypeId], [AppSourceId], [Name], [Subject], [HtmlContent], [FileName],
                     [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsActive], [IsDeleted], [DeletedAt])
                VALUES
                    (@typeId, @appSourceId, N'Hesap Aktivasyonu — Chatural', N'Hesabınızı yeniden açın - Chatural', N'
                <!DOCTYPE html>
                <html lang="tr">
                <head>
                    <meta charset="UTF-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <title>Hesabınızı Yeniden Açın</title>
                </head>
                
                <body style="margin:0;padding:0;background-color:#0f1117;font-family:Arial,Helvetica,sans-serif;color:#f9fafb;">
                    <table width="100%" cellpadding="0" cellspacing="0" border="0" role="presentation" style="width:100%;background-color:#0f1117;padding:40px 16px;">
                        <tr>
                            <td align="center">
                                <table width="720" cellpadding="0" cellspacing="0" border="0" role="presentation" style="width:720px;max-width:100%;background-color:#161922;background-image:linear-gradient(145deg,#1e2028,#161922);border:1px solid #2a2e3a;border-radius:16px;overflow:hidden;box-shadow:0 20px 60px rgba(0,0,0,0.45);">
                
                                    <tr>
                                        <td style="padding:44px 44px 28px;">
                                            <table width="100%" cellpadding="0" cellspacing="0" border="0" role="presentation">
                                                <tr>
                                                    <td valign="top">
                                                        <div style="margin-bottom:38px;color:#f9fafb;font-size:18px;font-weight:800;letter-spacing:-0.5px;">
                                                            Cha<span style="color:#38bdf8;">tural</span>
                                                        </div>
                
                                                        <div style="margin-bottom:6px;color:#f9fafb;font-size:34px;line-height:42px;font-weight:700;">
                                                            Tekrar hoş geldiniz!
                                                        </div>
                
                                                        <div style="color:#38bdf8;font-size:18px;line-height:28px;font-weight:700;">
                                                            Hesabınız sizi bekliyor.
                                                        </div>
                                                    </td>
                
                                                    <td width="150" align="right" valign="top">
                                                        <table width="104" height="104" cellpadding="0" cellspacing="0" border="0" role="presentation" style="width:104px;height:104px;background-color:#12283a;border:4px solid #38bdf8;border-radius:999px;">
                                                            <tr>
                                                                <td align="center" valign="middle" style="color:#38bdf8;font-size:44px;line-height:44px;font-weight:700;">
                                                                    &#8635;
                                                                </td>
                                                            </tr>
                                                        </table>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                
                                    <tr>
                                        <td style="padding:0 44px 28px;">
                                            <table width="100%" cellpadding="0" cellspacing="0" border="0" role="presentation" style="width:100%;background-color:#1a1d26;border:1px solid #2a2e3a;border-radius:10px;">
                                                <tr>
                                                    <td style="padding:30px 28px;color:#d1d5db;font-size:16px;line-height:26px;">
                                                        <p style="margin:0 0 22px;">
                                                            Merhaba <strong style="color:#f9fafb;">{{DisplayName}}</strong>,
                                                        </p>
                
                                                        <p style="margin:0 0 22px;">
                                                            Hesabınız kapalı durumda ve az önce yeniden açılması için bir istek aldık. Bu isteği siz yaptıysanız aşağıdaki düğmeye dokunmanız yeterli; sohbetleriniz, arkadaş listeniz ve geçmişiniz olduğu gibi sizi bekliyor.
                                                        </p>
                
                                                        <p style="margin:0;">
                                                            Görüşmek üzere.<br>
                                                            <strong style="color:#f9fafb;">Chatural</strong>
                                                        </p>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                
                                    <tr>
                                        <td align="center" style="padding:0 44px 12px;">
                                            <table cellpadding="0" cellspacing="0" border="0" role="presentation">
                                                <tr>
                                                    <td align="center" bgcolor="#38bdf8" style="background-color:#38bdf8;border-radius:10px;">
                                                        <a href="{{ActivationUrl}}" target="_blank" rel="noopener noreferrer" style="display:inline-block;padding:17px 46px;color:#0f172a;font-size:17px;font-weight:700;line-height:20px;text-decoration:none;">
                                                            Hesabımı Yeniden Aç
                                                        </a>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                
                                    <tr>
                                        <td align="center" style="padding:0 44px 28px;color:#9ca3af;font-size:14px;line-height:22px;">
                                            Bu bağlantı <strong style="color:#d1d5db;">{{ExpiresAt}}</strong> tarihine kadar ve yalnızca bir kez geçerlidir.
                                        </td>
                                    </tr>
                
                                    <tr>
                                        <td style="padding:0 44px 14px;">
                                            <table cellpadding="0" cellspacing="0" border="0" role="presentation">
                                                <tr>
                                                    <td width="32" height="32" align="center" valign="middle" style="width:32px;height:32px;background-color:#12283a;border-radius:8px;color:#38bdf8;font-size:17px;">
                                                        &#8801;
                                                    </td>
                
                                                    <td style="padding-left:12px;color:#f9fafb;font-size:16px;font-weight:700;">
                                                        İsteğin Geldiği Yer
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                
                                    <tr>
                                        <td style="padding:0 44px 34px;">
                                            <table width="100%" cellpadding="0" cellspacing="0" border="0" role="presentation" style="width:100%;background-color:#1a1d26;border:1px solid #2a2e3a;border-radius:10px;border-collapse:separate;border-spacing:0;overflow:hidden;">
                                                <tr>
                                                    <td width="32%" style="padding:16px 22px;border-right:1px solid #2a2e3a;border-bottom:1px solid #2a2e3a;color:#9ca3af;font-size:15px;">
                                                        IP Adresi
                                                    </td>
                
                                                    <td style="padding:16px 22px;border-bottom:1px solid #2a2e3a;color:#f9fafb;font-size:15px;word-break:break-word;">
                                                        {{IpAddress}}
                                                    </td>
                                                </tr>
                
                                                <tr>
                                                    <td width="32%" valign="top" style="padding:16px 22px;border-right:1px solid #2a2e3a;color:#9ca3af;font-size:15px;line-height:23px;">
                                                        Tarayıcı
                                                    </td>
                
                                                    <td valign="top" style="padding:16px 22px;color:#d1d5db;font-size:15px;line-height:23px;word-break:break-word;">
                                                        {{Browser}}
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                
                                    <tr>
                                        <td style="padding:0 44px 30px;">
                                            <table width="100%" cellpadding="0" cellspacing="0" border="0" role="presentation" style="width:100%;background-color:#1a1d26;border:1px solid #2a2e3a;border-left:3px solid #38bdf8;border-radius:10px;">
                                                <tr>
                                                    <td style="padding:20px 24px;color:#9ca3af;font-size:14px;line-height:23px;">
                                                        <strong style="color:#f9fafb;">Bu isteği siz yapmadıysanız</strong> hiçbir şey yapmanıza gerek yok: hesabınız kapalı kalır ve bağlantı kendiliğinden geçersiz olur. Yine de durum size tuhaf geldiyse
                                                        <a href="mailto:{{ContactEmail}}" style="color:#38bdf8;text-decoration:none;">{{ContactEmail}}</a>
                                                        adresinden bize yazın.
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                
                                    <tr>
                                        <td style="padding:24px 44px 8px;border-top:1px solid #24272f;text-align:center;">
                                            <div style="margin-bottom:10px;color:#9ca3af;font-size:14px;line-height:22px;">
                                                Düğme çalışmıyorsa aşağıdaki adresi tarayıcınızın adres çubuğuna yapıştırın.
                                            </div>
                
                                            <div style="color:#6b7280;font-size:13px;line-height:21px;word-break:break-all;">
                                                {{ActivationUrl}}
                                            </div>
                                        </td>
                                    </tr>
                
                                    <tr>
                                        <td style="padding:20px 44px 24px;text-align:center;">
                                            <div style="color:#6b7280;font-size:14px;">
                                                &copy; {{CurrentYear}} Chatural. Tüm hakları saklıdır.
                                            </div>
                                        </td>
                                    </tr>
                
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>
                ', N'activation-template.html',
                     SYSUTCDATETIME(), NULL, NULL, NULL, 1, 0, NULL);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(
                """
                DELETE t FROM [MailTemplates] t
                JOIN [MailTemplateTypes] ty ON ty.[Id] = t.[MailTemplateTypeId]
                JOIN [AppSources] a ON a.[Id] = t.[AppSourceId]
                WHERE ty.[Code] = N'AccountActivation' AND a.[Code] = N'Chat';
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_MailTemplates_AppSources_AppSourceId",
                table: "MailTemplates");

            migrationBuilder.DropTable(
                name: "AppSources");

            migrationBuilder.DropIndex(
                name: "IX_MailTemplates_AppSourceId",
                table: "MailTemplates");

            migrationBuilder.DropIndex(
                name: "IX_MailTemplates_MailTemplateTypeId_AppSourceId",
                table: "MailTemplates");

            migrationBuilder.DropColumn(
                name: "AppSourceId",
                table: "MailTemplates");

            migrationBuilder.CreateIndex(
                name: "IX_MailTemplates_MailTemplateTypeId",
                table: "MailTemplates",
                column: "MailTemplateTypeId",
                unique: true,
                filter: "[IsActive] = 1 AND [IsDeleted] = 0");
        }
    }
}
