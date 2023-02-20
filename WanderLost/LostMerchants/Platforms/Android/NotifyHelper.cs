using Android;
using Android.Gms.Extensions;
using Android.Widget;
using LostMerchants.Services;
using static Microsoft.Maui.ApplicationModel.Permissions;

namespace LostMerchants;

internal class NotifyHelper : INotifyHelper
{
    public async Task<PermissionStatus> CheckNotifyPermissions()
    {
        return await RequestAsync<NotificationPermission>();
    }

    public async Task<string> GetToken()
    {
        return (string)await Firebase.Messaging.FirebaseMessaging.Instance.GetToken();
    }

    public void ShowToast(string message)
    {
        var toast = Toast.MakeText(Android.App.Application.Context, message, ToastLength.Short);
        toast.Show();
    }
}

public class NotificationPermission : BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions => new[]
    {
        (Manifest.Permission.PostNotifications, true)
    };
}
