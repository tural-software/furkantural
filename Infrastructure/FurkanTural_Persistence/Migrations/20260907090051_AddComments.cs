using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FurkanTural_Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BlogId = table.Column<int>(type: "int", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    AuthorName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AuthorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotifyOnReply = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_Blogs_BlogId",
                        column: x => x.BlogId,
                        principalTable: "Blogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_Comments_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommentNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommentId = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
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
                    table.PrimaryKey("PK_CommentNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentNotifications_Comments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommentNotifications_CommentId",
                table: "CommentNotifications",
                column: "CommentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommentNotifications_Status",
                table: "CommentNotifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CommentNotifications_TokenHash",
                table: "CommentNotifications",
                column: "TokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_BlogId_Status",
                table: "Comments",
                columns: new[] { "BlogId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ParentId",
                table: "Comments",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_Status",
                table: "Comments",
                column: "Status");

            migrationBuilder.InsertData(
                table: "MailTemplateTypes",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsActive", "Name", "SortOrder", "UpdatedAt", "UpdatedBy" },
                values: new object[] { 7, "CommentReply", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Bir yoruma yanıt geldiğinde, yanıtlanan yorumun sahibine giden bildirim. Yalnızca bildirimi açıkça isteyenlere gider.", true, "Yorum — Yanıt Bildirimi", 7, null, null });

            migrationBuilder.Sql(TemplateSql("CommentReply", "Yorum Yanıtı — Blog", "{{PostTitle}} yazısındaki yorumunuza yanıt geldi", BodyShell));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE t FROM [MailTemplates] t
                JOIN [MailTemplateTypes] ty ON ty.[Id] = t.[MailTemplateTypeId]
                JOIN [AppSources] a ON a.[Id] = t.[AppSourceId]
                WHERE ty.[Code] = N'CommentReply' AND a.[Code] = N'Blog';
                """);

            migrationBuilder.DropTable(
                name: "CommentNotifications");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DeleteData(
                table: "MailTemplateTypes",
                keyColumn: "Id",
                keyValue: 7);
        }

        /// <summary>Şablon ham SQL ile eklenir; gerekçesi <c>AddNewsletterIssues</c> ile aynıdır — HtmlContent tohum verisi olarak taşınamayacak kadar büyüktür ve model anlık görüntüsünü şişirir. Ekleme koşulludur: aynı tür ve proje çifti için satır zaten varsa dokunulmaz, böylece elle düzenlenmiş bir şablonun üstüne yazılmaz.</summary>
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

        /// <summary>Kabuk düz ham dizedir, enterpolasyonlu değil: içindeki <c>{{Ad}}</c> yer tutucuları posta motoruna aittir ve enterpolasyonlu bir dizede C# ifadesi sanılırdı.<para>Bülten kabuğundan iki farkı var. Birincisi gövde alıntı kutusunda durur ve kaçışla yerleştirilir — metni yazan yönetici değil ziyaretçidir. İkincisi altbilgideki bağlantı abonelikten değil <b>yalnızca yanıt bildiriminden</b> çıkarır; ikisini aynı metinle sunmak, bülteni de kapattığı izlenimini verirdi.</para></summary>
        private const string BodyShell = """
            <!DOCTYPE html>
            <html lang="tr">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Yorumunuza yanıt geldi</title>
            </head>
            <body style="margin:0;padding:0;background-color:#0d1117;font-family:Arial,Helvetica,sans-serif;color:#e6edf3;">
                <table width="100%" cellpadding="0" cellspacing="0" border="0" role="presentation" style="width:100%;background-color:#0d1117;padding:40px 16px;">
                    <tr>
                        <td align="center">
                            <table width="640" cellpadding="0" cellspacing="0" border="0" role="presentation" style="width:640px;max-width:100%;background-color:#161b22;border:1px solid #232a33;border-radius:16px;overflow:hidden;">
                                <tr>
                                    <td style="padding:40px 40px 8px;">
                                        <div style="margin-bottom:32px;color:#e6edf3;font-size:18px;font-weight:800;letter-spacing:-0.5px;">Furkan<span style="color:#38bdf8;">Tural</span></div>
                                        <div style="margin-bottom:8px;color:#e6edf3;font-size:24px;line-height:32px;font-weight:700;">Yorumunuza yanıt geldi</div>
                                        <div style="margin-bottom:24px;color:#9aa7b4;font-size:14px;line-height:22px;">Merhaba {{RecipientName}}, <strong style="color:#c9d4df;">{{PostTitle}}</strong> yazısındaki yorumunuza {{ReplyAuthorName}} yanıt verdi.</div>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding:0 40px 8px;">
                                        <table width="100%" cellpadding="0" cellspacing="0" border="0" role="presentation" style="width:100%;background-color:#0d1117;border-left:3px solid #38bdf8;border-radius:8px;">
                                            <tr>
                                                <td style="padding:18px 20px;color:#c9d4df;font-size:15px;line-height:25px;">
                                                    {{ReplyBody}}
                                                    <div style="margin-top:14px;color:#6e7d8c;font-size:12px;">{{ReplyAuthorName}} · {{ReplyDate}}</div>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding:24px 40px 32px;">
                                        <a href="{{PostUrl}}" style="display:inline-block;padding:12px 24px;background-color:#38bdf8;color:#0d1117;font-size:14px;font-weight:700;text-decoration:none;border-radius:8px;">Yanıtı sayfada gör</a>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding:28px 40px 36px;border-top:1px solid #232a33;color:#9aa7b4;font-size:12px;line-height:20px;">
                                        Bu bildirimi {{Email}} adresine, yorumunuzu bırakırken yanıtlardan haberdar olmayı seçtiğiniz için gönderdik.<br>
                                        <a href="{{UnsubscribeUrl}}" style="color:#38bdf8;text-decoration:underline;">Yanıt bildirimlerini kapat</a><br><br>
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
