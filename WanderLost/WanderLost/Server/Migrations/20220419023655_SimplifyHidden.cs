using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WanderLost.Server.Migrations;

public partial class SimplifyHidden : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "Hidden",
            table: "ActiveMerchants",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql("UPDATE ActiveMerchants SET [Hidden] = 1 WHERE Discriminator = 'HiddenMerchant'");

        migrationBuilder.DropColumn(
            name: "Discriminator",
            table: "ActiveMerchants");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Discriminator",
            table: "ActiveMerchants",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "");

        migrationBuilder.Sql("UPDATE ActiveMerchants SET Discriminator = 'HiddenMerchant' WHERE [Hidden] = 1");
        migrationBuilder.Sql("UPDATE ActiveMerchants SET Discriminator = 'ActiveMerchant' WHERE [Hidden] <> 1");

        migrationBuilder.DropColumn(
            name: "Hidden",
            table: "ActiveMerchants");
    }
}
