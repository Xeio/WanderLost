using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WanderLost.Server.Migrations
{
    public partial class TrackFirebaseFailures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveFailures",
                table: "PushSubscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsecutiveFailures",
                table: "PushSubscriptions");
        }
    }
}
