using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WanderLost.Server.Migrations
{
    /// <inheritdoc />
    public partial class MultiItemSelect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActiveMerchants_Cards",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ActiveMerchantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rarity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiveMerchants_Cards", x => new { x.ActiveMerchantId, x.Name });
                    table.ForeignKey(
                        name: "FK_ActiveMerchants_Cards_ActiveMerchants_ActiveMerchantId",
                        column: x => x.ActiveMerchantId,
                        principalTable: "ActiveMerchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActiveMerchants_Rapports",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ActiveMerchantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rarity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiveMerchants_Rapports", x => new { x.ActiveMerchantId, x.Name });
                    table.ForeignKey(
                        name: "FK_ActiveMerchants_Rapports_ActiveMerchants_ActiveMerchantId",
                        column: x => x.ActiveMerchantId,
                        principalTable: "ActiveMerchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(@"
INSERT INTO ActiveMerchants_Cards
(ActiveMerchantId, Name, Rarity)
SELECT Id, Card_Name, Card_Rarity
FROM ActiveMerchants
");

            migrationBuilder.Sql(@"
INSERT INTO ActiveMerchants_Rapports
(ActiveMerchantId, Name, Rarity)
SELECT Id, Rapport_Name, Rapport_Rarity
FROM ActiveMerchants
");

            migrationBuilder.DropColumn(
                name: "Card_Name",
                table: "ActiveMerchants");

            migrationBuilder.DropColumn(
                name: "Card_Rarity",
                table: "ActiveMerchants");

            migrationBuilder.DropColumn(
                name: "Rapport_Name",
                table: "ActiveMerchants");

            migrationBuilder.DropColumn(
                name: "Rapport_Rarity",
                table: "ActiveMerchants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActiveMerchants_Cards");

            migrationBuilder.DropTable(
                name: "ActiveMerchants_Rapports");

            migrationBuilder.AddColumn<string>(
                name: "Card_Name",
                table: "ActiveMerchants",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Card_Rarity",
                table: "ActiveMerchants",
                type: "int",
                nullable: false,
                defaultValue: 0);

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
    }
}
