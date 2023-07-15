using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WanderLost.Server.Migrations
{
    /// <inheritdoc />
    public partial class DiscordNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscordNotifications",
                columns: table => new
                {
                    UserId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Server = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CardVoteThreshold = table.Column<int>(type: "int", nullable: false),
                    SendTestNotification = table.Column<bool>(type: "bit", nullable: false),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordNotifications", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "DiscordCardNotifications",
                columns: table => new
                {
                    DiscordNotificationUserId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    CardName = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordCardNotifications", x => new { x.DiscordNotificationUserId, x.CardName });
                    table.ForeignKey(
                        name: "FK_DiscordCardNotifications_DiscordNotifications_DiscordNotificationUserId",
                        column: x => x.DiscordNotificationUserId,
                        principalTable: "DiscordNotifications",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SentDiscordNotifications",
                columns: table => new
                {
                    MerchantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiscordNotificationUserId = table.Column<decimal>(type: "decimal(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SentDiscordNotifications", x => new { x.MerchantId, x.DiscordNotificationUserId });
                    table.ForeignKey(
                        name: "FK_SentDiscordNotifications_DiscordNotifications_DiscordNotificationUserId",
                        column: x => x.DiscordNotificationUserId,
                        principalTable: "DiscordNotifications",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscordCardNotifications_CardName",
                table: "DiscordCardNotifications",
                column: "CardName");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordNotifications_SendTestNotification",
                table: "DiscordNotifications",
                column: "SendTestNotification",
                filter: "[SendTestNotification] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordNotifications_Server",
                table: "DiscordNotifications",
                column: "Server");

            migrationBuilder.CreateIndex(
                name: "IX_SentDiscordNotifications_DiscordNotificationUserId",
                table: "SentDiscordNotifications",
                column: "DiscordNotificationUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscordCardNotifications");

            migrationBuilder.DropTable(
                name: "SentDiscordNotifications");

            migrationBuilder.DropTable(
                name: "DiscordNotifications");
        }
    }
}
