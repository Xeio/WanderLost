using LostMerchants.Services;
using System.Diagnostics;
using System.Net.Http.Json;
using WanderLost.Shared.Data;

namespace LostMerchants
{
    public partial class MainPage : ContentPage
    {
        private readonly INotifyHelper _notifyHelper;

        public MainPage(INotifyHelper notifyHelper)
        {
            InitializeComponent();
            _notifyHelper = notifyHelper;
        }

        private async void SendTestNotification(object sender, EventArgs e)
        {
            var token = await _notifyHelper.GetToken();

            CounterBtn.Text = token;

            SemanticScreenReader.Announce(CounterBtn.Text);

            var access = await _notifyHelper.CheckNotifyPermissions();
            if(access != PermissionStatus.Granted)
            {
                _notifyHelper.ShowToast("Notification permission required");
                return;
            }

            var result = await new HttpClient().PostAsJsonAsync(
                "https://lostmerchants.com/api/PushNotifications/UpdatePushSubscription",
                new PushSubscription()
                {
                    Token = token,
                    SendTestNotification = true,
                });

            Debug.Assert(result.IsSuccessStatusCode, "Failed update of subscription");

            _notifyHelper.ShowToast("Updated subscription");
        }
    }
}