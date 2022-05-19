using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WanderLost.Server.Migrations.AuthDb
{
    public partial class BansOnUserTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BanExpires",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            //Migrate bans from the old table
            migrationBuilder.Sql(@"
UPDATE U
SET U.BanExpires = B.ExpiresAt 
FROM AspNetUsers U
LEFT JOIN (
    SELECT UserId, MAX(ExpiresAt) ExpiresAt 
    FROM Bans
    WHERE UserId IS NOT NULL
    GROUP BY UserId
) B ON B.UserId = U.Id
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BanExpires",
                table: "AspNetUsers");
        }
    }
}
