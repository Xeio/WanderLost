using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WanderLost.Server.Migrations;

public partial class ServerMerges1 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
UPDATE PushSubscriptions SET Server = 'Rethramis' WHERE Server = 'Shadespire';
UPDATE PushSubscriptions SET Server = 'Tortoyk' WHERE Server = 'Petrania';
UPDATE PushSubscriptions SET Server = 'Moonkeep' WHERE Server = 'Tragon';
UPDATE PushSubscriptions SET Server = 'Punika' WHERE Server = 'Stonehearth';
UPDATE PushSubscriptions SET Server = 'Agaton' WHERE Server = 'Kurzan';
UPDATE PushSubscriptions SET Server = 'Vern' WHERE Server = 'Prideholme';
UPDATE PushSubscriptions SET Server = 'Gienah' WHERE Server = 'Yorn';
UPDATE PushSubscriptions SET Server = 'Arcturus' WHERE Server = 'Feiton';
UPDATE PushSubscriptions SET Server = 'Armen' WHERE Server = 'Sirius';
UPDATE PushSubscriptions SET Server = 'Armen' WHERE Server = 'Sceptrum';
UPDATE PushSubscriptions SET Server = 'Lazenith' WHERE Server = 'Thaemine';
UPDATE PushSubscriptions SET Server = 'Lazenith' WHERE Server = 'Procyon';
UPDATE PushSubscriptions SET Server = 'Evergrace' WHERE Server = 'Nineveh';
UPDATE PushSubscriptions SET Server = 'Evergrace' WHERE Server = 'Beatrice';
UPDATE PushSubscriptions SET Server = 'Ezrebet' WHERE Server = 'Brelshaza';
UPDATE PushSubscriptions SET Server = 'Ezrebet' WHERE Server = 'Inanna';
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {

    }
}
