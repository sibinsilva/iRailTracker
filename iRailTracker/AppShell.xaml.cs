using iRailTracker.View;

namespace iRailTracker
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(AppSettings), typeof(AppSettings));
        }
    }
}
