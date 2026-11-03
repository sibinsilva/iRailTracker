using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using iRailTracker.Service;

namespace iRailTracker
{
    /// <summary>
    /// Shared logic for the home-screen widgets. Each placed widget shows its own station (chosen in
    /// <see cref="WidgetConfigActivity"/>), falling back to the first favourite. Android refreshes
    /// them every 30 minutes; the ⟳ button and favourite changes refresh them at once.
    /// </summary>
    public abstract class DeparturesWidgetProviderBase : AppWidgetProvider
    {
        // A broadcast receiver must finish within about 10 seconds
        private static readonly TimeSpan UpdateTimeout = TimeSpan.FromSeconds(8);

        protected abstract int LayoutId { get; }
        protected abstract int MaxRows { get; }

        /// <summary>Asks every placed widget (both sizes) to refresh, e.g. after the favourites change.</summary>
        public static void RequestUpdate(Context context)
        {
            RequestUpdate(context, typeof(DeparturesWidgetProvider));
            RequestUpdate(context, typeof(CompactDeparturesWidgetProvider));
        }

        public static void RequestUpdate(Context context, Type providerType, int[]? onlyIds = null)
        {
            var manager = AppWidgetManager.GetInstance(context);
            var ids = onlyIds ?? manager?.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(providerType)));
            if (ids is null || ids.Length == 0)
                return;

            context.SendBroadcast(CreateUpdateIntent(context, providerType, ids));
        }

        public static string StationKey(int widgetId) => $"widget_station_{widgetId}";

        public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
        {
            if (context is null || appWidgetManager is null || appWidgetIds is null || appWidgetIds.Length == 0)
                return;

            var pending = GoAsync();
            _ = Task.Run(async () =>
            {
                try
                {
                    // Each widget can show a different station
                    var updates = appWidgetIds.Select(id => UpdateOneAsync(context, appWidgetManager, id));
                    await Task.WhenAll(updates);
                }
                finally
                {
                    pending?.Finish();
                }
            });
        }

        public override void OnDeleted(Context? context, int[]? appWidgetIds)
        {
            foreach (var id in appWidgetIds ?? [])
                Preferences.Remove(StationKey(id));
        }

        private async Task UpdateOneAsync(Context context, AppWidgetManager manager, int widgetId)
        {
            var station = Preferences.Get(StationKey(widgetId), string.Empty);
            Render(context, manager, widgetId, new DeparturesSummary.Summary(
                string.IsNullOrEmpty(station) ? "iRailTracker" : station, ["Updating…"], string.Empty));

            DeparturesSummary.Summary summary;
            try
            {
                summary = await DeparturesSummary.LoadAsync(station, MaxRows).WaitAsync(UpdateTimeout);
            }
            catch (TimeoutException)
            {
                summary = new DeparturesSummary.Summary(station, ["Timed out, tap ⟳ to retry"], string.Empty);
            }
            catch (Exception ex)
            {
                summary = new DeparturesSummary.Summary(station, [$"Error: {ex.Message}"], string.Empty);
            }

            Render(context, manager, widgetId, summary);
        }

        private void Render(Context context, AppWidgetManager manager, int widgetId, DeparturesSummary.Summary summary)
        {
            var views = new RemoteViews(context.PackageName, LayoutId);

            views.SetTextViewText(Resource.Id.widget_title, summary.Title);
            views.SetTextViewText(Resource.Id.widget_row1, summary.Rows.ElementAtOrDefault(0) ?? string.Empty);
            if (MaxRows > 1)
            {
                views.SetTextViewText(Resource.Id.widget_row2, summary.Rows.ElementAtOrDefault(1) ?? string.Empty);
                views.SetTextViewText(Resource.Id.widget_row3, summary.Rows.ElementAtOrDefault(2) ?? string.Empty);
                views.SetTextViewText(Resource.Id.widget_updated, summary.Footer);
            }

            var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
            if (launchIntent is not null)
            {
                var openApp = PendingIntent.GetActivity(context, widgetId, launchIntent,
                    PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
                views.SetOnClickPendingIntent(Resource.Id.widget_root, openApp);
            }

            // Request code = widget id, so each widget's ⟳ refreshes that widget
            var refresh = PendingIntent.GetBroadcast(context, widgetId, CreateUpdateIntent(context, GetType(), [widgetId]),
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
            views.SetOnClickPendingIntent(Resource.Id.widget_refresh, refresh);

            manager.UpdateAppWidget(widgetId, views);
        }

        private static Intent CreateUpdateIntent(Context context, Type providerType, int[] ids)
        {
            var intent = new Intent(context, providerType);
            intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
            return intent;
        }
    }

    /// <summary>4×2 widget: the next three departures.</summary>
    [BroadcastReceiver(Label = "iRailTracker departures", Exported = true)]
    [IntentFilter([AppWidgetManager.ActionAppwidgetUpdate])]
    [MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/departures_widget_info")]
    public class DeparturesWidgetProvider : DeparturesWidgetProviderBase
    {
        protected override int LayoutId => Resource.Layout.departures_widget;
        protected override int MaxRows => 3;
    }

    /// <summary>One-row widget: just the next departure.</summary>
    [BroadcastReceiver(Label = "iRailTracker next train", Exported = true)]
    [IntentFilter([AppWidgetManager.ActionAppwidgetUpdate])]
    [MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/departures_widget_compact_info")]
    public class CompactDeparturesWidgetProvider : DeparturesWidgetProviderBase
    {
        protected override int LayoutId => Resource.Layout.departures_widget_compact;
        protected override int MaxRows => 1;
    }
}
