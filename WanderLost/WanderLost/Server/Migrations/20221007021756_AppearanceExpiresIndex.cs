using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WanderLost.Server.Migrations
{
    public partial class AppearanceExpiresIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActiveMerchants_UploadedBy",
                table: "ActiveMerchants");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantGroups_AppearanceExpires",
                table: "MerchantGroups",
                column: "AppearanceExpires");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MerchantGroups_AppearanceExpires",
                table: "MerchantGroups");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveMerchants_UploadedBy",
                table: "ActiveMerchants",
                column: "UploadedBy");
        }
    }
}
