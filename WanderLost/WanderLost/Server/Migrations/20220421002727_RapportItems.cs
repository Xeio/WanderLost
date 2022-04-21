using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WanderLost.Server.Migrations
{
    public partial class RapportItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RapportRarity",
                table: "ActiveMerchants");

            migrationBuilder.AddColumn<string>(
                name: "Rapport_Name",
                table: "ActiveMerchants",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Rapport_Rarity",
                table: "ActiveMerchants",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rapport_Name",
                table: "ActiveMerchants");

            migrationBuilder.DropColumn(
                name: "Rapport_Rarity",
                table: "ActiveMerchants");

            migrationBuilder.AddColumn<int>(
                name: "RapportRarity",
                table: "ActiveMerchants",
                type: "int",
                nullable: true);
        }
    }
}
