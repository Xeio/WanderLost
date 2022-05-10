using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WanderLost.Server.Migrations
{
    public partial class MerchantUploaderIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ActiveMerchants_UploadedBy",
                table: "ActiveMerchants",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveMerchants_UploadedByUserId",
                table: "ActiveMerchants",
                column: "UploadedByUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActiveMerchants_UploadedBy",
                table: "ActiveMerchants");

            migrationBuilder.DropIndex(
                name: "IX_ActiveMerchants_UploadedByUserId",
                table: "ActiveMerchants");
        }
    }
}
