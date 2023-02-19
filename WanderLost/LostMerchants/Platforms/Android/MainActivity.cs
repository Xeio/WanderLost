using Android.App;
using Android.Content.PM;
using Android.OS;
using Firebase;

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
        }
    }
}