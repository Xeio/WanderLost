using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WanderLost.Server.Migrations
{
    public partial class MerchGroupsReOrderAlternateKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AK_MerchantGroups_Server_MerchantName_AppearanceExpires",
                table: "MerchantGroups");

            migrationBuilder.DropIndex(
                name: "IX_MerchantGroups_AppearanceExpires",
                table: "MerchantGroups");

            migrationBuilder.AlterColumn<string>(
                name: "Server",
                table: "PushSubscriptions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_MerchantGroups_AppearanceExpires_Server_MerchantName",
                table: "MerchantGroups",
                columns: new[] { "AppearanceExpires", "Server", "MerchantName" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AK_MerchantGroups_AppearanceExpires_Server_MerchantName",
                table: "MerchantGroups");

            migrationBuilder.AlterColumn<string>(
                name: "Server",
                table: "PushSubscriptions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_MerchantGroups_Server_MerchantName_AppearanceExpires",
                table: "MerchantGroups",
                columns: new[] { "Server", "MerchantName", "AppearanceExpires" });

            migrationBuilder.CreateIndex(
                name: "IX_MerchantGroups_AppearanceExpires",
                table: "MerchantGroups",
                column: "AppearanceExpires");
        }
    }
}
