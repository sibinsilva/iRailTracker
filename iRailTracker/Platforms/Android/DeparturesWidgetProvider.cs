using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using iRailTracker.Service;

namespace iRailTracker
{
    /// <summary>
    /// Home-screen widget showing the next departures from the user's first favourite station.
    /// Android refreshes it every 30 minutes; the ⟳ button and favourite changes refresh it at once.
    /// </summary>
    [BroadcastReceiver(Label = "iRailTracker departures", Exported = true)]
    [IntentFilter([AppWidgetManager.ActionAppwidgetUpdate])]
    [MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/departures_widget_info")]
    public class DeparturesWidgetProvider : AppWidgetProvider
    {
        // A broadcast receiver must finish within about 10 seconds
        private static readonly TimeSpan UpdateTimeout = TimeSpan.FromSeconds(8);

        /// <summary>Asks every placed widget to refresh, e.g. after the favourites change.</summary>
        public static void RequestUpdate(Context context)
        {
            var manager = AppWidgetManager.GetInstance(context);
            var ids = manager?.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(DeparturesWidgetProvider))));
            if (ids is null || ids.Length == 0)
                return;

            context.SendBroadcast(CreateUpdateIntent(context, ids));
        }

        public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
        {
            if (context is null || appWidgetManager is null || appWidgetIds is null || appWidgetIds.Length == 0)
                return;

            // Show "Updating…" straight away, then fill in the departures off the main thread
            Render(context, appWidgetManager, appWidgetIds,
                new DeparturesSummary.Summary("iRailTracker", ["Updating…"], string.Empty));

            var pending = GoAsync();
            _ = Task.Run(async () =>
            {
                try
                {
                    DeparturesSummary.Summary summary;
                    try
                    {
                        summary = await DeparturesSummary.LoadAsync().WaitAsync(UpdateTimeout);
                    }
                    catch (TimeoutException)
                    {
                        summary = new DeparturesSummary.Summary("iRailTracker", ["Timed out, tap ⟳ to retry"], string.Empty);
                    }
                    catch (Exception ex)
                    {
                        summary = new DeparturesSummary.Summary("iRailTracker", [$"Error: {ex.Message}"], string.Empty);
                    }

                    Render(context, appWidgetManager, appWidgetIds, summary);
                }
                finally
                {
                    pending?.Finish();
                }
            });
        }

        private static void Render(Context context, AppWidgetManager manager, int[] ids, DeparturesSummary.Summary summary)
        {
            var views = new RemoteViews(context.PackageName, Resource.Layout.departures_widget);

            views.SetTextViewText(Resource.Id.widget_title, summary.Title);
            views.SetTextViewText(Resource.Id.widget_row1, summary.Rows.ElementAtOrDefault(0) ?? string.Empty);
            views.SetTextViewText(Resource.Id.widget_row2, summary.Rows.ElementAtOrDefault(1) ?? string.Empty);
            views.SetTextViewText(Resource.Id.widget_row3, summary.Rows.ElementAtOrDefault(2) ?? string.Empty);
            views.SetTextViewText(Resource.Id.widget_updated, summary.Footer);

            var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
            if (launchIntent is not null)
            {
                var openApp = PendingIntent.GetActivity(context, 0, launchIntent,
                    PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
                views.SetOnClickPendingIntent(Resource.Id.widget_root, openApp);
            }

            var refresh = PendingIntent.GetBroadcast(context, 0, CreateUpdateIntent(context, ids),
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
            views.SetOnClickPendingIntent(Resource.Id.widget_refresh, refresh);

            manager.UpdateAppWidget(ids, views);
        }

        private static Intent CreateUpdateIntent(Context context, int[] ids)
        {
            var intent = new Intent(context, typeof(DeparturesWidgetProvider));
            intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
            return intent;
        }
    }
}
