using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WanderLost.Server.Migrations;

public partial class BanProcessor : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "BannedAt",
            table: "AspNetUsers",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "PostProcessComplete",
            table: "ActiveMerchants",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql("UPDATE ActiveMerchants SET PostProcessComplete = 1");

        migrationBuilder.CreateIndex(
            name: "IX_ActiveMerchants_PostProcessComplete",
            table: "ActiveMerchants",
            column: "PostProcessComplete",
            filter: "[PostProcessComplete] = 0");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ActiveMerchants_PostProcessComplete",
            table: "ActiveMerchants");

        migrationBuilder.DropColumn(
            name: "BannedAt",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "PostProcessComplete",
            table: "ActiveMerchants");
    }
}
