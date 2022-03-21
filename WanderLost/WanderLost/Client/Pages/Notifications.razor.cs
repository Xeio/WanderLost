using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace WanderLost.Client.Pages
{
    public partial class Notifications
    {
        [Inject] public ILocalStorageService LocalStorage { get; set; } = default!; //default! to suppress NULL warning

        //todo
    }
}
