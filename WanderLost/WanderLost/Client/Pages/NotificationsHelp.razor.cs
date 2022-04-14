using Microsoft.AspNetCore.Components;

namespace WanderLost.Client.Pages
{
    public partial class NotificationsHelp
    {
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;

        [Parameter] public string? Section { get; set; }

        private Dictionary<string, bool> _initialSectionState = new Dictionary<string, bool>();
        protected override void OnInitialized()
        {
            initSections();
            if (!string.IsNullOrEmpty(Section))
            {
                _initialSectionState[Section] = true;
            }
            base.OnInitialized();
        }

        private void initSections()
        {
            _initialSectionState.Add("enableNotif", false);
            _initialSectionState.Add("unresponsiveNotif", false);
            _initialSectionState.Add("noNotif", false);
            _initialSectionState.Add("missingNotif", false);
            _initialSectionState.Add("setupNotif", false);
            _initialSectionState.Add("notifsoundpopup", false);
        }
    }
}
