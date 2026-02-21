using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Dispatching;

namespace iRailTracker.Service
{
    public class AutoRefreshService
    {
        private static AutoRefreshService? _instance;
        public static AutoRefreshService Instance =>
            _instance ??= new AutoRefreshService();

        private IDispatcherTimer? _timer;

        private AutoRefreshService() { }

        public void Start(bool enabled, int intervalSeconds)
        {
            Stop();

            if (!enabled)
                return;

            _timer = Application.Current?.Dispatcher.CreateTimer();
            if (_timer == null) return;

            _timer.Interval = TimeSpan.FromSeconds(intervalSeconds);
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        public void Stop()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= OnTimerTick;
                _timer = null;
            }
        }

        public void UpdateSettings(bool enabled, int intervalSeconds)
        {
            Start(enabled, intervalSeconds);
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            WeakReferenceMessenger.Default.Send(new AutoRefreshMessage());
            System.Diagnostics.Debug.WriteLine("Auto refresh triggered");
        }
    }
}
