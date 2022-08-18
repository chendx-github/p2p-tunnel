namespace client.service.app
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            Launcher.OpenAsync("https://snltty.gitee.io/p2p-tunnel");
        }
    }
}