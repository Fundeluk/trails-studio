namespace TrailsStudio.MAUI
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("studiopage", typeof(StudioPage));
        }
    }
}