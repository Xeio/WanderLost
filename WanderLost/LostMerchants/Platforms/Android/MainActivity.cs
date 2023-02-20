using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using Firebase;
using Java.Net;

namespace LostMerchants
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var app = FirebaseApp.InitializeApp(ApplicationContext);

            System.Diagnostics.Debug.Assert(app is not null, "Failed to initialize firebase.");

            var notificationManager = (NotificationManager)GetSystemService(NotificationService);

            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                var weiChannel = new NotificationChannel("wei", "Wei", NotificationImportance.High)
                {
                    Description = "Notification for Wei cards"
                };
                notificationManager.CreateNotificationChannel(weiChannel);

                var rapportChannel = new NotificationChannel("rapport", "Rapport", NotificationImportance.Default)
                {
                    Description = "Notifications for rapport items"
                };
                notificationManager.CreateNotificationChannel(rapportChannel);
            }
        }
    }
}