using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WanderLost.Server.Migrations;

public partial class RapportItems : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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

        migrationBuilder.Sql("UPDATE ActiveMerchants SET Rapport_Rarity = RapportRarity");

        migrationBuilder.DropColumn(
            name: "RapportRarity",
            table: "ActiveMerchants");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "RapportRarity",
            table: "ActiveMerchants",
            type: "int",
            nullable: true);

        migrationBuilder.Sql("UPDATE ActiveMerchants SET RapportRarity = Rapport_Rarity");

        migrationBuilder.DropColumn(
            name: "Rapport_Name",
            table: "ActiveMerchants");

        migrationBuilder.DropColumn(
            name: "Rapport_Rarity",
            table: "ActiveMerchants");

    }
}
