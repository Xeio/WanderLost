namespace LostMerchants.Services;

public interface INotifyHelper
{
    public Task<string> GetToken();
    public void ShowToast(string message);
    public Task<PermissionStatus> CheckNotifyPermissions();
}
