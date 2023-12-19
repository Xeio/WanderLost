using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WanderLost.Server.Migrations
{
    /// <inheritdoc />
    public partial class MiscItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tradeskill",
                table: "ActiveMerchants");

            migrationBuilder.CreateTable(
                name: "ActiveMerchants_MiscItems",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ActiveMerchantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rarity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiveMerchants_MiscItems", x => new { x.ActiveMerchantId, x.Name });
                    table.ForeignKey(
                        name: "FK_ActiveMerchants_MiscItems_ActiveMerchants_ActiveMerchantId",
                        column: x => x.ActiveMerchantId,
                        principalTable: "ActiveMerchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActiveMerchants_MiscItems");

            migrationBuilder.AddColumn<string>(
                name: "Tradeskill",
                table: "ActiveMerchants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
