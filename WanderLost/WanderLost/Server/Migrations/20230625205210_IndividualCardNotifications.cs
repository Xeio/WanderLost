using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WanderLost.Server.Migrations
{
    /// <inheritdoc />
    public partial class IndividualCardNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WeiVoteThreshold",
                table: "PushSubscriptions",
                newName: "CardVoteThreshold");

            migrationBuilder.CreateTable(
                name: "CardNotifications",
                columns: table => new
                {
                    PushSubscriptionId = table.Column<int>(type: "int", nullable: false),
                    CardName = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardNotifications", x => new { x.PushSubscriptionId, x.CardName });
                    table.ForeignKey(
                        name: "FK_CardNotifications_PushSubscriptions_PushSubscriptionId",
                        column: x => x.PushSubscriptionId,
                        principalTable: "PushSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex( 
                name: "IX_CardNotifications_CardName",
                table: "CardNotifications",
                column: "CardName");

            migrationBuilder.Sql(@"
INSERT INTO CardNotifications
(PushSubscriptionId, CardName)
    SELECT Id, 'Wei'
    FROM PushSubscriptions
    WHERE WeiNotify = 1
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardNotifications");

            migrationBuilder.RenameColumn(
                name: "CardVoteThreshold",
                table: "PushSubscriptions",
                newName: "WeiVoteThreshold");
        }
    }
}
