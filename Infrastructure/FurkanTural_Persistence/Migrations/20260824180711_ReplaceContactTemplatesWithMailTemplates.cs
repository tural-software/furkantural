using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FurkanTural_Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceContactTemplatesWithMailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MailTemplateTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_MailTemplateTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MailTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MailTemplateTypeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    HtmlContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_MailTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MailTemplates_MailTemplateTypes_MailTemplateTypeId",
                        column: x => x.MailTemplateTypeId,
                        principalTable: "MailTemplateTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "MailTemplateTypes",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "Description", "IsActive", "Name", "SortOrder", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, "ContactOwner", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "İletişim formu doldurulduğunda site sahibine düşen bildirim.", true, "İletişim — Site Sahibine", 1, null, null },
                    { 2, "ContactUser", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "İletişim formunu dolduran kişiye giden alındı yanıtı.", true, "İletişim — Gönderene", 2, null, null },
                    { 3, "AccountActivation", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Pasife alınmış bir hesabı yeniden açan doğrulama bağlantısı.", true, "Hesap Aktivasyonu", 3, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MailTemplates_MailTemplateTypeId",
                table: "MailTemplates",
                column: "MailTemplateTypeId",
                unique: true,
                filter: "[IsActive] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MailTemplateTypes_Code",
                table: "MailTemplateTypes",
                column: "Code",
                unique: true);

            migrationBuilder.Sql(@"
INSERT INTO [MailTemplates]
    ([MailTemplateTypeId], [Name], [Subject], [HtmlContent], [FileName],
     [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsActive], [IsDeleted], [DeletedAt])
SELECT
    CASE src.[TemplateType] WHEN 'Owner' THEN 1 ELSE 2 END,
    src.[Name],
    CASE src.[TemplateType]
        WHEN 'Owner' THEN N'Yeni İletişim Mesajı - {{FullName}}'
        ELSE N'Mesajınız Alındı - Furkan Tural'
    END,
    src.[HtmlContent],
    src.[FileName],
    src.[CreatedAt], src.[CreatedBy], src.[UpdatedAt], src.[UpdatedBy],
    CASE WHEN src.[rn] = 1 AND src.[IsActive] = 1 AND src.[IsDeleted] = 0 THEN 1 ELSE 0 END,
    src.[IsDeleted], src.[DeletedAt]
FROM (
    SELECT ct.*,
           ROW_NUMBER() OVER (
               PARTITION BY ct.[TemplateType]
               ORDER BY CASE WHEN ct.[IsActive] = 1 AND ct.[IsDeleted] = 0 THEN 0 ELSE 1 END, ct.[Id] DESC
           ) AS [rn]
    FROM [ContactTemplates] ct
    WHERE ct.[TemplateType] IN ('Owner', 'User')
) src;");

            migrationBuilder.DropTable(
                name: "ContactTemplates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HtmlContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TemplateType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactTemplates", x => x.Id);
                });

            migrationBuilder.Sql(@"
INSERT INTO [ContactTemplates]
    ([Name], [TemplateType], [FileName], [HtmlContent],
     [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsActive], [IsDeleted], [DeletedAt])
SELECT
    mt.[Name],
    CASE mt.[MailTemplateTypeId] WHEN 1 THEN 'Owner' ELSE 'User' END,
    mt.[FileName],
    mt.[HtmlContent],
    mt.[CreatedAt], mt.[CreatedBy], mt.[UpdatedAt], mt.[UpdatedBy],
    mt.[IsActive], mt.[IsDeleted], mt.[DeletedAt]
FROM [MailTemplates] mt
WHERE mt.[MailTemplateTypeId] IN (1, 2);");

            migrationBuilder.DropTable(
                name: "MailTemplates");

            migrationBuilder.DropTable(
                name: "MailTemplateTypes");
        }
    }
}
