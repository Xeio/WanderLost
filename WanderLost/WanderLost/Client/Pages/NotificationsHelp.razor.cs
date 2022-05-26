using Microsoft.AspNetCore.Components;

namespace WanderLost.Client.Pages;

public partial class NotificationsHelp
{
    [Parameter] public string? Section { get; set; }

    private readonly Dictionary<string, bool> _initialSectionState = new();

    protected override void OnInitialized()
    {
        InitSections();
        if (!string.IsNullOrEmpty(Section))
        {
            _initialSectionState[Section] = true;
        }
        base.OnInitialized();
    }

    private void InitSections()
    {
        _initialSectionState.Add("enableNotif", false);
        _initialSectionState.Add("unresponsiveNotif", false);
        _initialSectionState.Add("noNotif", false);
        _initialSectionState.Add("missingNotif", false);
        _initialSectionState.Add("setupNotif", false);
        _initialSectionState.Add("notifsoundpopup", false);
    }
}
