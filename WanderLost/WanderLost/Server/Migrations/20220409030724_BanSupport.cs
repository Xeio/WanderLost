using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WanderLost.Server.Migrations
{
    public partial class BanSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UploadedBy",
                table: "ActiveMerchants",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "ActiveMerchants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Bans",
                columns: table => new
                {
                    ClientId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bans", x => new { x.ClientId, x.ExpiresAt });
                });

            migrationBuilder.Sql("UPDATE [ActiveMerchants] SET [Discriminator] = 'ActiveMerchant'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bans");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "ActiveMerchants");

            migrationBuilder.AlterColumn<string>(
                name: "UploadedBy",
                table: "ActiveMerchants",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(60)",
                oldMaxLength: 60);
        }
    }
}
