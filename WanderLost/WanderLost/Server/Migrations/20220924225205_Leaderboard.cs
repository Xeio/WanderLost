using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WanderLost.Server.Migrations;

public partial class Leaderboard : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "RequiresLeaderboardProcessing",
            table: "ActiveMerchants",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateTable(
            name: "Leaderboards",
            columns: table => new
            {
                UserId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                DisplayName = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                TotalVotes = table.Column<int>(type: "int", nullable: false),
                TotalSubmissions = table.Column<int>(type: "int", nullable: false),
                PrimaryServer = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Leaderboards", x => x.UserId);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ActiveMerchants_RequiresLeaderboardProcessing",
            table: "ActiveMerchants",
            column: "RequiresLeaderboardProcessing",
            filter: "[RequiresLeaderboardProcessing] = 1");

        migrationBuilder.Sql(@"
UPDATE ActiveMerchants
SET RequiresLeaderboardProcessing = 1
WHERE UploadedByUserId IS NOT NULL
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Leaderboards");

        migrationBuilder.DropIndex(
            name: "IX_ActiveMerchants_RequiresLeaderboardProcessing",
            table: "ActiveMerchants");

        migrationBuilder.DropColumn(
            name: "RequiresLeaderboardProcessing",
            table: "ActiveMerchants");
    }
}
