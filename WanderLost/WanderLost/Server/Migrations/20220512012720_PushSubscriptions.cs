using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WanderLost.Server.Migrations;

public partial class PushSubscriptions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "RequiresProcessing",
            table: "ActiveMerchants",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateTable(
            name: "PushSubscriptions",
            columns: table => new
            {
                Token = table.Column<string>(type: "nvarchar(450)", nullable: false),
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Server = table.Column<string>(type: "nvarchar(450)", nullable: false),
                WeiVoteThreshold = table.Column<int>(type: "int", nullable: false),
                WeiNotify = table.Column<bool>(type: "bit", nullable: false),
                RapportVoteThreshold = table.Column<int>(type: "int", nullable: false),
                LegendaryRapportNotify = table.Column<bool>(type: "bit", nullable: false),
                SendTestNotification = table.Column<bool>(type: "bit", nullable: false),
                LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PushSubscriptions", x => x.Token);
                table.UniqueConstraint("AK_PushSubscriptions_Id", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "SentPushNotifications",
            columns: table => new
            {
                MerchantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SubscriptionId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SentPushNotifications", x => new { x.MerchantId, x.SubscriptionId });
                table.ForeignKey(
                    name: "FK_SentPushNotifications_ActiveMerchants_MerchantId",
                    column: x => x.MerchantId,
                    principalTable: "ActiveMerchants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SentPushNotifications_PushSubscriptions_SubscriptionId",
                    column: x => x.SubscriptionId,
                    principalTable: "PushSubscriptions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ActiveMerchants_RequiresProcessing",
            table: "ActiveMerchants",
            column: "RequiresProcessing",
            filter: "[RequiresProcessing] = 1");

        migrationBuilder.CreateIndex(
            name: "IX_PushSubscriptions_SendTestNotification",
            table: "PushSubscriptions",
            column: "SendTestNotification",
            filter: "[SendTestNotification] = 1");

        migrationBuilder.CreateIndex(
            name: "IX_PushSubscriptions_Server",
            table: "PushSubscriptions",
            column: "Server");

        migrationBuilder.CreateIndex(
            name: "IX_SentPushNotifications_SubscriptionId",
            table: "SentPushNotifications",
            column: "SubscriptionId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SentPushNotifications");

        migrationBuilder.DropTable(
            name: "PushSubscriptions");

        migrationBuilder.DropIndex(
            name: "IX_ActiveMerchants_RequiresProcessing",
            table: "ActiveMerchants");

        migrationBuilder.DropColumn(
            name: "RequiresProcessing",
            table: "ActiveMerchants");
    }
}
