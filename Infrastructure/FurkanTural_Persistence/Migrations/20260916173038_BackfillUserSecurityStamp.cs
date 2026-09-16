using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FurkanTural_Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillUserSecurityStamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE [Users] SET [SecurityStamp] = LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), '-', '')) " +
                "WHERE [SecurityStamp] IS NULL OR [SecurityStamp] = ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
