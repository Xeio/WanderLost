using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WanderLost.Server.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MerchantGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Server = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MerchantName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NextAppearance = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AppearanceExpires = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantGroups", x => x.Id);
                    table.UniqueConstraint("AK_MerchantGroups_Server_MerchantName_AppearanceExpires", x => new { x.Server, x.MerchantName, x.AppearanceExpires });
                });

            migrationBuilder.CreateTable(
                name: "ActiveMerchants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Zone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Card_Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Card_Rarity = table.Column<int>(type: "int", nullable: false),
                    RapportRarity = table.Column<int>(type: "int", nullable: true),
                    Votes = table.Column<int>(type: "int", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActiveMerchantGroupId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiveMerchants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActiveMerchants_MerchantGroups_ActiveMerchantGroupId",
                        column: x => x.ActiveMerchantGroupId,
                        principalTable: "MerchantGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Votes",
                columns: table => new
                {
                    ActiveMerchantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    VoteType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votes", x => new { x.ActiveMerchantId, x.ClientId });
                    table.ForeignKey(
                        name: "FK_Votes_ActiveMerchants_ActiveMerchantId",
                        column: x => x.ActiveMerchantId,
                        principalTable: "ActiveMerchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActiveMerchants_ActiveMerchantGroupId",
                table: "ActiveMerchants",
                column: "ActiveMerchantGroupId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Votes");

            migrationBuilder.DropTable(
                name: "ActiveMerchants");

            migrationBuilder.DropTable(
                name: "MerchantGroups");
        }
    }
}
