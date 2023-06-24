using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WanderLost.Server.Migrations
{
    /// <inheritdoc />
    public partial class SwapPushSubscriptionPrimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SentPushNotifications_PushSubscriptions_SubscriptionId",
                table: "SentPushNotifications");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PushSubscriptions_Id",
                table: "PushSubscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PushSubscriptions",
                table: "PushSubscriptions");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_PushSubscriptions_Token",
                table: "PushSubscriptions",
                column: "Token");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PushSubscriptions",
                table: "PushSubscriptions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SentPushNotifications_PushSubscriptions_SubscriptionId",
                table: "SentPushNotifications",
                column: "SubscriptionId",
                principalTable: "PushSubscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SentPushNotifications_PushSubscriptions_SubscriptionId",
                table: "SentPushNotifications");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PushSubscriptions_Token",
                table: "PushSubscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PushSubscriptions",
                table: "PushSubscriptions");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_PushSubscriptions_Id",
                table: "PushSubscriptions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PushSubscriptions",
                table: "PushSubscriptions",
                column: "Token");

            migrationBuilder.AddForeignKey(
                name: "FK_SentPushNotifications_PushSubscriptions_SubscriptionId",
                table: "SentPushNotifications",
                column: "SubscriptionId",
                principalTable: "PushSubscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
