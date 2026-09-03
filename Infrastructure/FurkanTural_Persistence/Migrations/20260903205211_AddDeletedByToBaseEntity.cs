using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FurkanTural_Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeletedByToBaseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "UserFriends",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Subscribers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Statuses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Skills",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Roles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Reports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "PushSubscriptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "ProjectImages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Musics",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "MusicImages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "MailTemplateTypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "MailTemplates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Logs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Experiences",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Educations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Contacts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "ChatMessages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "CallPolicies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "CallLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Blogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "BlogImages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "BlogCategories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AppSources",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "AccountActivations",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AppSources",
                keyColumn: "Id",
                keyValue: 1,
                column: "DeletedBy",
                value: null);

            migrationBuilder.UpdateData(
                table: "AppSources",
                keyColumn: "Id",
                keyValue: 2,
                column: "DeletedBy",
                value: null);

            migrationBuilder.UpdateData(
                table: "AppSources",
                keyColumn: "Id",
                keyValue: 3,
                column: "DeletedBy",
                value: null);

            migrationBuilder.UpdateData(
                table: "AppSources",
                keyColumn: "Id",
                keyValue: 4,
                column: "DeletedBy",
                value: null);

            migrationBuilder.UpdateData(
                table: "CallPolicies",
                keyColumn: "Id",
                keyValue: 1,
                column: "DeletedBy",
                value: null);

            migrationBuilder.UpdateData(
                table: "MailTemplateTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "DeletedBy",
                value: null);

            migrationBuilder.UpdateData(
                table: "MailTemplateTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "DeletedBy",
                value: null);

            migrationBuilder.UpdateData(
                table: "MailTemplateTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "DeletedBy",
                value: null);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "DeletedBy",
                value: null);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "DeletedBy",
                value: null);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "DeletedBy",
                value: null);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                column: "DeletedBy",
                value: null);

            migrationBuilder.UpdateData(
                table: "Statuses",
                keyColumn: "Id",
                keyValue: 1,
                column: "DeletedBy",
                value: null);

            migrationBuilder.UpdateData(
                table: "Statuses",
                keyColumn: "Id",
                keyValue: 2,
                column: "DeletedBy",
                value: null);

            migrationBuilder.UpdateData(
                table: "Statuses",
                keyColumn: "Id",
                keyValue: 3,
                column: "DeletedBy",
                value: null);

            migrationBuilder.UpdateData(
                table: "Statuses",
                keyColumn: "Id",
                keyValue: 4,
                column: "DeletedBy",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "UserFriends");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Subscribers");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Statuses");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PushSubscriptions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ProjectImages");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Musics");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "MusicImages");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "MailTemplateTypes");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "MailTemplates");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Experiences");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Educations");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "CallPolicies");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "CallLogs");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "BlogImages");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "BlogCategories");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AppSources");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AccountActivations");
        }
    }
}
