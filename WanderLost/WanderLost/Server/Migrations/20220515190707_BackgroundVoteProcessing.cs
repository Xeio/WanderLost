using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WanderLost.Server.Migrations
{
    public partial class BackgroundVoteProcessing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresVoteProcessing",
                table: "ActiveMerchants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ActiveMerchants_RequiresVoteProcessing",
                table: "ActiveMerchants",
                column: "RequiresVoteProcessing",
                filter: "[RequiresVoteProcessing] = 1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActiveMerchants_RequiresVoteProcessing",
                table: "ActiveMerchants");

            migrationBuilder.DropColumn(
                name: "RequiresVoteProcessing",
                table: "ActiveMerchants");
        }
    }
}
