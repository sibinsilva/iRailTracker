using iRailTracker.Model;
using iRailTracker.Service;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Tiling;
using MapsuiBrush = Mapsui.Styles.Brush;
using MapsuiColor = Mapsui.Styles.Color;
using MapsuiMap = Mapsui.Map;
using MapsuiSymbolStyle = Mapsui.Styles.SymbolStyle;

namespace iRailTracker.View;

public partial class TrainLiveStatusPage : ContentPage
{
    private readonly IReadOnlyList<Station> _stations;
    private IDispatcherTimer? _blinkTimer;
    private MemoryLayer? _currentStationLayer;
    private MyLocationLayer? _myLocationLayer;
    private IDispatcherTimer? _liveTimer;
    private bool _mapBuilt;
    private bool _refreshing;

    // OpenStreetMap's tile usage policy requires a User-Agent that identifies
    // the app; Mapsui's generic default is blocked with HTTP 403.
    private static readonly string OsmUserAgent =
        $"iRailTracker/{AppInfo.Current.VersionString} ({AppInfo.Current.PackageName}; contact: sibinsilva@gmail.com)";

    public TrainLiveStatusPage(TrainJourney journey, IReadOnlyList<Station> stations)
    {
        InitializeComponent();
        BindingContext = journey;
        _stations = stations;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is not TrainJourney journey)
            return;

        TrainMapControl.Info += OnMapInfo;

        // OnAppearing runs again every time the page comes back into view; build the layers only
        // once so tiles and pins aren't stacked on top of each other.
        if (_mapBuilt)
        {
            StartBlinking();
            StartLiveUpdates(journey);
            await RefreshLivePositionAsync(journey);
            return;
        }

        _mapBuilt = true;

        var map = TrainMapControl.Map;
        map.Layers.Add(OpenStreetMap.CreateTileLayer(OsmUserAgent));

        var allPoints = new List<MPoint>();
        var movements = await FetchMovementsAsync(journey.TrainCode, journey.TrainDate);

        if (movements.Count > 0)
        {
            var currentStop = TrainMovementAnalysis.DetermineCurrentStop(movements, IrishTime.Now, journey.TrainDate);
            ShowLiveStatus(currentStop);

            foreach (var movement in movements.OrderBy(m => m.LocationOrder))
            {
                var station = FindStation(movement.LocationCode);
                if (station is null)
                    continue;

                var (x, y) = SphericalMercator.FromLonLat(
                    station.StationLongitude, station.StationLatitude);
                var point = new MPoint(x, y);
                allPoints.Add(point);

                bool isOrigin = movement.LocationType == "O";
                bool isDestination = movement.LocationType == "D";
                bool isCurrent = currentStop is not null
                    ? ReferenceEquals(movement, currentStop)
                    : !string.IsNullOrWhiteSpace(journey.LastLocation) &&
                      IsCurrentStation(movement.LocationFullName, journey.LastLocation);

                if (isCurrent)
                    AddCurrentStationPin(point, map);

                var color = isOrigin ? new MapsuiColor(34, 139, 34)
                          : isDestination ? new MapsuiColor(220, 20, 60)
                          : new MapsuiColor(65, 105, 225);
                var scale = (isOrigin || isDestination) ? 0.45 : 0.25;
                AddStationPin(point, movement.LocationFullName, color, scale, map, movement);
            }
        }
        else
        {
            AddFallbackPin(journey.Origin, new MapsuiColor(34, 139, 34), 0.45, map, allPoints);
            AddFallbackPin(journey.Destination, new MapsuiColor(220, 20, 60), 0.45, map, allPoints);

            if (!string.IsNullOrWhiteSpace(journey.LastLocation))
            {
                var station = FindStation(journey.LastLocation);
                if (station is not null)
                {
                    var (x, y) = SphericalMercator.FromLonLat(
                        station.StationLongitude, station.StationLatitude);
                    var point = new MPoint(x, y);
                    allPoints.Add(point);
                    AddCurrentStationPin(point, map);
                }
            }
        }

        if (allPoints.Count == 0)
            return;

        var extent = new MRect(
            allPoints.Min(p => p.X), allPoints.Min(p => p.Y),
            allPoints.Max(p => p.X), allPoints.Max(p => p.Y));
        map.Navigator.ZoomToBox(extent, MBoxFit.Fit);

        StartBlinking();
        StartLiveUpdates(journey);
        await UpdateMyLocationAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopBlinking();
        StopLiveUpdates();
        TrainMapControl.Info -= OnMapInfo;
    }

    #region Live updates

    /// <summary>Re-polls the train's movements on the user's refresh interval while the page is visible.</summary>
    private void StartLiveUpdates(TrainJourney journey)
    {
        if (_liveTimer is not null)
            return;

        var seconds = Math.Max(Preferences.Get(AppPreferences.RefreshIntervalSeconds, 30), 15);
        _liveTimer = Dispatcher.CreateTimer();
        _liveTimer.Interval = TimeSpan.FromSeconds(seconds);
        _liveTimer.Tick += async (_, _) => await RefreshLivePositionAsync(journey);
        _liveTimer.Start();
    }

    private void StopLiveUpdates()
    {
        _liveTimer?.Stop();
        _liveTimer = null;
    }

    private async Task RefreshLivePositionAsync(TrainJourney journey)
    {
        if (_refreshing)
            return;

        _refreshing = true;
        try
        {
            var movements = await FetchMovementsAsync(journey.TrainCode, journey.TrainDate);
            if (movements.Count > 0)
            {
                var currentStop = TrainMovementAnalysis.DetermineCurrentStop(movements, IrishTime.Now, journey.TrainDate);
                ShowLiveStatus(currentStop);

                var station = currentStop is null ? null : FindStation(currentStop.LocationCode);
                if (station is not null)
                    MoveCurrentPin(ToMapPoint(station));
            }

            await UpdateMyLocationAsync();
        }
        finally
        {
            _refreshing = false;
        }
    }

    private void ShowLiveStatus(TrainMovement? currentStop)
    {
        LiveStatusLabel.Text = currentStop is null
            ? $"Updated {IrishTime.Now:HH:mm:ss}"
            : $"Near {currentStop.LocationFullName} · updated {IrishTime.Now:HH:mm:ss}";
        LiveStatusLabel.IsVisible = true;
    }

    private void MoveCurrentPin(MPoint point)
    {
        var map = TrainMapControl.Map;

        if (_currentStationLayer is null)
        {
            AddCurrentStationPin(point, map);
            StartBlinking();
        }
        else
        {
            var feature = new PointFeature(point);
            feature["Label"] = "Current";
            _currentStationLayer.Features = [feature];
            _currentStationLayer.DataHasChanged();
        }

        map.RefreshGraphics();
    }

    /// <summary>
    /// Shows the user's own position if location permission was already granted elsewhere in the
    /// app (Nearby Station). Never prompts from this page.
    /// </summary>
    private async Task UpdateMyLocationAsync()
    {
        try
        {
            if (await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>() != PermissionStatus.Granted)
                return;

            var location = await Geolocation.GetLastKnownLocationAsync();
            if (location is null)
                return;

            var (x, y) = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
            var point = new MPoint(x, y);

            if (_myLocationLayer is null)
            {
                _myLocationLayer = new MyLocationLayer(TrainMapControl.Map, point) { Name = "MyLocation" };
                TrainMapControl.Map.Layers.Add(_myLocationLayer);
            }
            else
            {
                _myLocationLayer.UpdateMyLocation(point, true);
            }
        }
        catch (Exception ex) when (ex is FeatureNotSupportedException or FeatureNotEnabledException or PermissionException)
        {
            // Location is optional on this page.
        }
    }

    private static MPoint ToMapPoint(Station station)
    {
        var (x, y) = SphericalMercator.FromLonLat(station.StationLongitude, station.StationLatitude);
        return new MPoint(x, y);
    }

    #endregion

    private void OnMapInfo(object? sender, MapInfoEventArgs e)
    {
        var mapInfo = e.GetMapInfo?.Invoke(TrainMapControl.Map.Layers);
        var feature = mapInfo?.Feature;

        if (feature is null)
        {
            PinInfoOverlay.IsVisible = false;
            return;
        }

        var label = feature["Label"]?.ToString();
        if (string.IsNullOrWhiteSpace(label) || label == "Current")
        {
            PinInfoOverlay.IsVisible = false;
            return;
        }

        PinStationLabel.Text = label;

        var locationType = feature["LocationType"]?.ToString();
        var arrival = feature["ExpectedArrival"]?.ToString();
        var departure = feature["ExpectedDeparture"]?.ToString();

        var details = locationType switch
        {
            "O" => !string.IsNullOrWhiteSpace(departure) ? $"Dep: {departure}" : "Origin",
            "D" => !string.IsNullOrWhiteSpace(arrival) ? $"Arr: {arrival}" : "Destination",
            _ => FormatArrivalDeparture(arrival, departure)
        };

        PinArrivalLabel.Text = details;
        PinInfoOverlay.IsVisible = true;
    }

    private static string FormatArrivalDeparture(string? arrival, string? departure)
    {
        bool hasArr = !string.IsNullOrWhiteSpace(arrival);
        bool hasDep = !string.IsNullOrWhiteSpace(departure);

        return (hasArr, hasDep) switch
        {
            (true, true) => $"Arr: {arrival}  Dep: {departure}",
            (true, false) => $"Arr: {arrival}",
            (false, true) => $"Dep: {departure}",
            _ => string.Empty
        };
    }

    private async Task<List<TrainMovement>> FetchMovementsAsync(string trainCode, DateTime trainDate)
    {
        try
        {
            var settingsService = MauiProgram.Current?.GetService<DataService<Settings>>();
            if (settingsService?.Data is null)
                return [];

            var stationService = new StationService();
            return await stationService.GetTrainMovementsAsync(
                settingsService.Data, trainCode, trainDate, _ => { });
        }
        catch
        {
            return [];
        }
    }

    private static void AddStationPin(MPoint point, string label, MapsuiColor color, double scale, MapsuiMap map, TrainMovement? movement = null)
    {
        var feature = new PointFeature(point);
        feature["Label"] = label;

        if (movement is not null)
        {
            feature["ExpectedArrival"] = movement.ExpectedArrival;
            feature["ExpectedDeparture"] = movement.ExpectedDeparture;
            feature["LocationType"] = movement.LocationType;
        }

        var layer = new MemoryLayer
        {
            Name = $"Stop_{label}",
            Features = [feature],
            Style = new MapsuiSymbolStyle
            {
                SymbolScale = scale,
                Fill = new MapsuiBrush(color),
            }
        };
        map.Layers.Add(layer);
    }

    private void AddCurrentStationPin(MPoint point, MapsuiMap map)
    {
        var feature = new PointFeature(point);
        feature["Label"] = "Current";

        _currentStationLayer = new MemoryLayer
        {
            Name = "CurrentStation",
            Features = [feature],
            Style = new MapsuiSymbolStyle
            {
                SymbolScale = 0.6,
                Fill = new MapsuiBrush(new MapsuiColor(255, 165, 0)),
            }
        };
        map.Layers.Add(_currentStationLayer);
    }

    private void AddFallbackPin(string stationName, MapsuiColor color, double scale, MapsuiMap map, List<MPoint> points)
    {
        var station = FindStation(stationName);
        if (station is null)
            return;

        var (x, y) = SphericalMercator.FromLonLat(
            station.StationLongitude, station.StationLatitude);
        var point = new MPoint(x, y);
        points.Add(point);
        AddStationPin(point, stationName, color, scale, map);
    }

    private void StartBlinking()
    {
        if (_currentStationLayer is null || _blinkTimer is not null)
            return;

        _blinkTimer = Dispatcher.CreateTimer();
        _blinkTimer.Interval = TimeSpan.FromMilliseconds(600);
        _blinkTimer.Tick += (_, _) =>
        {
            _currentStationLayer.Enabled = !_currentStationLayer.Enabled;
            TrainMapControl.Map.RefreshGraphics();
        };
        _blinkTimer.Start();
    }

    private void StopBlinking()
    {
        _blinkTimer?.Stop();
        _blinkTimer = null;
    }

    private static bool IsCurrentStation(string movementStationName, string lastLocation)
    {
        return lastLocation.Contains(movementStationName, StringComparison.OrdinalIgnoreCase);
    }

    private Station? FindStation(string name)
    {
        var normalized = name.Trim();

        return _stations.FirstOrDefault(s =>
            s.StationCode.Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(s.StationAlias) && s.StationAlias.Equals(normalized, StringComparison.OrdinalIgnoreCase)) ||
            normalized.Contains(s.StationCode, StringComparison.OrdinalIgnoreCase));
    }
}
