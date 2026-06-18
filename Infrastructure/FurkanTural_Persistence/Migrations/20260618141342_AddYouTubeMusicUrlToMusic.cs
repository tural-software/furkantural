using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FurkanTural_Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddYouTubeMusicUrlToMusic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "YouTubeMusicUrl",
                table: "Musics",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YouTubeMusicUrl",
                table: "Musics");
        }
    }
}
