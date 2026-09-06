using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FurkanTural_Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSlugToBlogsAndCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Categories",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Blogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql(TempSlugFunction);
            migrationBuilder.Sql(BackfillBlogs);
            migrationBuilder.Sql(BackfillCategories);
            migrationBuilder.Sql("DROP FUNCTION dbo.__FT_TempSlug;");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Categories",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(160)",
                oldMaxLength: 160,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Blogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Blogs_Slug",
                table: "Blogs",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_Slug",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Blogs_Slug",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Blogs");
        }

        private const string TempSlugFunction = @"
CREATE FUNCTION dbo.__FT_TempSlug (@source NVARCHAR(500), @maxLength INT)
RETURNS VARCHAR(200)
AS
BEGIN
    DECLARE @t NVARCHAR(500) = LTRIM(RTRIM(ISNULL(@source, N'')));

    SET @t = REPLACE(@t COLLATE Latin1_General_BIN2, NCHAR(305), N'i');
    SET @t = REPLACE(@t COLLATE Latin1_General_BIN2, NCHAR(304), N'i');
    SET @t = REPLACE(@t COLLATE Latin1_General_BIN2, NCHAR(287), N'g');
    SET @t = REPLACE(@t COLLATE Latin1_General_BIN2, NCHAR(286), N'g');
    SET @t = REPLACE(@t COLLATE Latin1_General_BIN2, NCHAR(351), N's');
    SET @t = REPLACE(@t COLLATE Latin1_General_BIN2, NCHAR(350), N's');
    SET @t = REPLACE(@t COLLATE Latin1_General_BIN2, NCHAR(252), N'u');
    SET @t = REPLACE(@t COLLATE Latin1_General_BIN2, NCHAR(220), N'u');
    SET @t = REPLACE(@t COLLATE Latin1_General_BIN2, NCHAR(246), N'o');
    SET @t = REPLACE(@t COLLATE Latin1_General_BIN2, NCHAR(214), N'o');
    SET @t = REPLACE(@t COLLATE Latin1_General_BIN2, NCHAR(231), N'c');
    SET @t = REPLACE(@t COLLATE Latin1_General_BIN2, NCHAR(199), N'c');
    SET @t = REPLACE(@t COLLATE Latin1_General_BIN2, NCHAR(226), N'a');
    SET @t = REPLACE(@t COLLATE Latin1_General_BIN2, NCHAR(194), N'a');
    SET @t = REPLACE(@t COLLATE Latin1_General_BIN2, NCHAR(238), N'i');
    SET @t = REPLACE(@t COLLATE Latin1_General_BIN2, NCHAR(206), N'i');
    SET @t = REPLACE(@t COLLATE Latin1_General_BIN2, NCHAR(251), N'u');
    SET @t = REPLACE(@t COLLATE Latin1_General_BIN2, NCHAR(219), N'u');
    SET @t = REPLACE(@t COLLATE Latin1_General_BIN2, NCHAR(233), N'e');
    SET @t = REPLACE(@t COLLATE Latin1_General_BIN2, NCHAR(201), N'e');

    DECLARE @a VARCHAR(500) = LOWER(CAST(@t AS VARCHAR(500)) COLLATE SQL_Latin1_General_CP1_CI_AS);

    DECLARE @p INT = PATINDEX('%[^-a-z0-9]%', @a COLLATE Latin1_General_BIN2);
    WHILE @p > 0
    BEGIN
        SET @a = STUFF(@a, @p, 1, '-');
        SET @p = PATINDEX('%[^-a-z0-9]%', @a COLLATE Latin1_General_BIN2);
    END

    WHILE CHARINDEX('--', @a) > 0
        SET @a = REPLACE(@a, '--', '-');

    WHILE LEN(@a) > 0 AND LEFT(@a, 1) = '-'
        SET @a = STUFF(@a, 1, 1, '');
    WHILE LEN(@a) > 0 AND RIGHT(@a, 1) = '-'
        SET @a = LEFT(@a, LEN(@a) - 1);

    IF LEN(@a) > @maxLength
    BEGIN
        SET @a = LEFT(@a, @maxLength);
        IF CHARINDEX('-', REVERSE(@a)) > 0
            SET @a = LEFT(@a, @maxLength - CHARINDEX('-', REVERSE(@a)));
    END

    RETURN @a;
END";

        private const string BackfillBlogs = @"
UPDATE [Blogs] SET [Slug] = dbo.__FT_TempSlug([Title], 200);
UPDATE [Blogs] SET [Slug] = 'yazi' WHERE [Slug] IS NULL OR LEN([Slug]) = 0;

WITH d AS (SELECT [Id], [Slug], ROW_NUMBER() OVER (PARTITION BY [Slug] ORDER BY [Id]) AS rn FROM [Blogs])
UPDATE d SET [Slug] = LEFT([Slug], 200 - LEN(CAST(rn AS VARCHAR(10))) - 1) + '-' + CAST(rn AS VARCHAR(10))
WHERE rn > 1;

WITH d AS (SELECT [Id], [Slug], ROW_NUMBER() OVER (PARTITION BY [Slug] ORDER BY [Id]) AS rn FROM [Blogs])
UPDATE d SET [Slug] = LEFT([Slug], 189) + '-' + CAST([Id] AS VARCHAR(10))
WHERE rn > 1;";

        private const string BackfillCategories = @"
UPDATE [Categories] SET [Slug] = dbo.__FT_TempSlug([Name], 160);
UPDATE [Categories] SET [Slug] = 'kategori' WHERE [Slug] IS NULL OR LEN([Slug]) = 0;

WITH d AS (SELECT [Id], [Slug], ROW_NUMBER() OVER (PARTITION BY [Slug] ORDER BY [Id]) AS rn FROM [Categories])
UPDATE d SET [Slug] = LEFT([Slug], 160 - LEN(CAST(rn AS VARCHAR(10))) - 1) + '-' + CAST(rn AS VARCHAR(10))
WHERE rn > 1;

WITH d AS (SELECT [Id], [Slug], ROW_NUMBER() OVER (PARTITION BY [Slug] ORDER BY [Id]) AS rn FROM [Categories])
UPDATE d SET [Slug] = LEFT([Slug], 149) + '-' + CAST([Id] AS VARCHAR(10))
WHERE rn > 1;";
    }
}
