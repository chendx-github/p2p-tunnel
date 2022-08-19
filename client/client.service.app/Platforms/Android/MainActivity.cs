using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using Android.Util;
using AndroidX.Core.App;

namespace client.service.app
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            Window.SetFlags(Android.Views.WindowManagerFlags.TranslucentStatus, Android.Views.WindowManagerFlags.TranslucentStatus);
            Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
            Window.SetNavigationBarColor(Android.Graphics.Color.Transparent);

            PowerManager pm = (PowerManager)GetSystemService(PowerService);
            var wakelock = pm.NewWakeLock(WakeLockFlags.Partial | WakeLockFlags.OnAfterRelease, "mywakelock");
            if (wakelock != null)
            {
                wakelock.Acquire();
            }

            intent = new Intent(this, typeof(MyService));
            this.StartService(intent);

            base.OnCreate(savedInstanceState);
        }

        static Intent intent;

    }

    [Service(IsolatedProcess = false, Exported = true, Name = "client.service.app.MyService", Process = "client.service.app.myservice_process")]
    public class MyService : Service
    {
        public override IBinder OnBind(Intent intent)
        {
            return null;
        }
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            return StartCommandResult.Sticky;

        }
        public override void OnCreate()
        {
            Startup.Start();


            // Instantiate the builder and set notification elements:
            //NotificationCompat.Builder builder = new NotificationCompat.Builder(this,"test")
            //    .SetContentTitle("Sample Notification")
            //    .SetContentText("Hello World! This is my first notification!")
            //    .SetSmallIcon(Resource.Drawable.appiconfg);
            //Notification notification = builder.Build();
            //NotificationManager notificationManager =
            //    GetSystemService(Context.NotificationService) as NotificationManager;

            //const int notificationId = 0;
            //notificationManager.Notify(notificationId, notification);
        }

    }
}