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

        var map = TrainMapControl.Map;
        map.Layers.Add(OpenStreetMap.CreateTileLayer());

        var allPoints = new List<MPoint>();
        var movements = await FetchMovementsAsync(journey.TrainCode);

        if (movements.Count > 0)
        {
            foreach (var movement in movements.OrderBy(m => m.LocationOrder))
            {
                var station = FindStation(movement.LocationFullName);
                if (station is null)
                    continue;

                var (x, y) = SphericalMercator.FromLonLat(
                    station.StationLongitude, station.StationLatitude);
                var point = new MPoint(x, y);
                allPoints.Add(point);

                bool isOrigin = movement.LocationType == "O";
                bool isDestination = movement.LocationType == "D";
                bool isCurrent = !string.IsNullOrWhiteSpace(journey.LastLocation) &&
                    IsCurrentStation(movement.LocationFullName, journey.LastLocation);

                if (isCurrent)
                    AddCurrentStationPin(point, map);

                var color = isOrigin ? new MapsuiColor(34, 139, 34)
                          : isDestination ? new MapsuiColor(220, 20, 60)
                          : new MapsuiColor(65, 105, 225);
                var scale = (isOrigin || isDestination) ? 0.45 : 0.25;
                AddStationPin(point, movement.LocationFullName, color, scale, map);
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
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopBlinking();
    }

    private async Task<List<TrainMovement>> FetchMovementsAsync(string trainCode)
    {
        try
        {
            var settingsService = MauiProgram.Current?.GetService<DataService<Settings>>();
            if (settingsService?.Data is null)
                return [];

            var stationService = new StationService();
            return await stationService.GetTrainMovementsAsync(
                settingsService.Data, trainCode, _ => { });
        }
        catch
        {
            return [];
        }
    }

    private static void AddStationPin(MPoint point, string label, MapsuiColor color, double scale, MapsuiMap map)
    {
        var feature = new PointFeature(point);
        feature["Label"] = label;

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
        if (_currentStationLayer is null)
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
            s.StationDesc.Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(s.StationAlias) && s.StationAlias.Equals(normalized, StringComparison.OrdinalIgnoreCase)) ||
            normalized.Contains(s.StationDesc, StringComparison.OrdinalIgnoreCase));
    }
}
