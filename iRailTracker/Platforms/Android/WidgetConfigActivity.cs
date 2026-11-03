using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using iRailTracker.Service;

namespace iRailTracker
{
    /// <summary>
    /// Shown when a widget is placed (or reconfigured with a long-press on Android 12+): pick the
    /// station that widget should show. Favourites are listed first. Plain Android UI, because
    /// it opens without the main app.
    /// </summary>
    [Activity(Name = "com.app.irailtracker.WidgetConfigActivity", Label = "Choose station",
        Exported = true, Theme = "@style/Theme.AppCompat.DayNight.Dialog")]
    [IntentFilter([AppWidgetManager.ActionAppwidgetConfigure])]
    public class WidgetConfigActivity : Activity
    {
        private int _widgetId = AppWidgetManager.InvalidAppwidgetId;
        private List<string> _allStations = new();
        private ArrayAdapter<string>? _adapter;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Backing out without choosing leaves the widget on its default (first favourite)
            _widgetId = Intent?.GetIntExtra(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId)
                        ?? AppWidgetManager.InvalidAppwidgetId;
            SetResult(Result.Canceled, ResultIntent());
            if (_widgetId == AppWidgetManager.InvalidAppwidgetId)
            {
                Finish();
                return;
            }

            var favourites = FavouriteStationsStore.Load();
            var others = StationListCache.Load().Select(s => s.StationDesc.Trim())
                .Where(n => n.Length > 0 && !favourites.Contains(n))
                .Distinct()
                .OrderBy(n => n);
            _allStations = favourites.Select(f => $"★ {f}").Concat(others).ToList();

            var padding = (int)(16 * (Resources?.DisplayMetrics?.Density ?? 2));
            var layout = new LinearLayout(this) { Orientation = Orientation.Vertical };
            layout.SetPadding(padding, padding, padding, 0);

            var search = new EditText(this) { Hint = "Search stations" };
            search.SetSingleLine(true);
            layout.AddView(search, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

            var list = new Android.Widget.ListView(this);
            _adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, _allStations.ToList());
            list.Adapter = _adapter;
            list.ItemClick += (_, e) => Choose(_adapter.GetItem(e.Position));
            layout.AddView(list, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, (int)(420 * (Resources?.DisplayMetrics?.Density ?? 2))));

            search.TextChanged += (_, _) => Filter(search.Text ?? string.Empty);

            if (_allStations.Count == 0)
                search.Hint = "Open the app once to load the station list";

            SetContentView(layout);
        }

        private void Filter(string query)
        {
            if (_adapter is null)
                return;
            _adapter.Clear();
            _adapter.AddAll(_allStations.Where(s => s.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase)).ToList());
            _adapter.NotifyDataSetChanged();
        }

        private void Choose(string? item)
        {
            if (string.IsNullOrEmpty(item))
                return;

            var station = item.StartsWith("★ ") ? item[2..] : item;
            Preferences.Set(DeparturesWidgetProviderBase.StationKey(_widgetId), station);

            // Refresh just this widget, whichever size it is
            var info = AppWidgetManager.GetInstance(this)?.GetAppWidgetInfo(_widgetId);
            var providerType = info?.Provider?.ClassName?.EndsWith(nameof(CompactDeparturesWidgetProvider)) == true
                ? typeof(CompactDeparturesWidgetProvider)
                : typeof(DeparturesWidgetProvider);
            DeparturesWidgetProviderBase.RequestUpdate(this, providerType, [_widgetId]);

            SetResult(Result.Ok, ResultIntent());
            Finish();
        }

        private Intent ResultIntent() =>
            new Intent().PutExtra(AppWidgetManager.ExtraAppwidgetId, _widgetId);
    }
}
