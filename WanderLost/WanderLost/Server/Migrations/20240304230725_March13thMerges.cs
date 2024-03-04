using Microsoft.EntityFrameworkCore.Migrations;
using WanderLost.Shared;

#nullable disable

namespace WanderLost.Server.Migrations
{
    /// <inheritdoc />
    public partial class March13thMerges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var pair in Utils.ServerMerges)
            {
                migrationBuilder.Sql($"UPDATE PushSubscriptions SET Server = '{pair.Value}' WHERE Server = '{pair.Key}';");
                migrationBuilder.Sql($"UPDATE DiscordNotifications SET Server = '{pair.Value}' WHERE Server = '{pair.Key}';");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
