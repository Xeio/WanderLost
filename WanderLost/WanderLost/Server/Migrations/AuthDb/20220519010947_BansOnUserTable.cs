using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WanderLost.Server.Migrations.AuthDb;

public partial class BansOnUserTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "BanExpires",
            table: "AspNetUsers",
            type: "datetimeoffset",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "BanExpires",
            table: "AspNetUsers");
    }
}
