using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WanderLost.Server.Migrations;

public partial class ServerMerges2 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
UPDATE PushSubscriptions SET Server = 'Ealyn' WHERE Server = 'Rethramis';
UPDATE PushSubscriptions SET Server = 'Ealyn' WHERE Server = 'Tortoyk';
UPDATE PushSubscriptions SET Server = 'Nia' WHERE Server = 'Moonkeep';
UPDATE PushSubscriptions SET Server = 'Nia' WHERE Server = 'Punika';
UPDATE PushSubscriptions SET Server = 'Arthetine' WHERE Server = 'Agaton';
UPDATE PushSubscriptions SET Server = 'Arthetine' WHERE Server = 'Vern';
UPDATE PushSubscriptions SET Server = 'Blackfang' WHERE Server = 'Gienah';
UPDATE PushSubscriptions SET Server = 'Blackfang' WHERE Server = 'Arcturus';
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {

    }
}
