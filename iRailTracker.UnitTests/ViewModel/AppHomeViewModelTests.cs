using CommunityToolkit.Mvvm.Messaging;
using iRailTracker.Model;
using iRailTracker.Service;
using iRailTracker.ViewModel;
using System.ComponentModel;

namespace iRailTracker.UnitTests.ViewModel;

public class AppHomeViewModelTests
{
    private static AppHomeViewModel CreateViewModel(List<Station>? stations = null)
    {
        var stationListService = new DataService<List<Station>>
        {
            Data = stations ??
            [
                new() { StationDesc = "Dublin Connolly", StationCode = "CNLLY" },
                new() { StationDesc = "Dublin Heuston", StationCode = "HSTON" },
                new() { StationDesc = "Cork", StationCode = "CORK" }
            ]
        };

        var settingsService = new DataService<Settings>
        {
            Data = new Settings()
        };

        return new AppHomeViewModel(stationListService, settingsService);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesStationOptions_SortedAndDistinct()
    {
        var stations = new List<Station>
        {
            new() { StationDesc = "Cork", StationCode = "CORK" },
            new() { StationDesc = "Dublin Connolly", StationCode = "CNLLY" },
            new() { StationDesc = "Cork", StationCode = "CORK2" },
        };

        var vm = CreateViewModel(stations);

        Assert.Equal(2, vm.StationOptions.Count);
        Assert.Equal("Cork", vm.StationOptions[0]);
        Assert.Equal("Dublin Connolly", vm.StationOptions[1]);
    }

    [Fact]
    public void Constructor_InitializesEmptyTrainJourneys()
    {
        var vm = CreateViewModel();

        Assert.Empty(vm.TrainJourneys);
    }

    [Fact]
    public void Constructor_SelectedStation_IsEmpty()
    {
        var vm = CreateViewModel();

        Assert.Equal(string.Empty, vm.SelectedStation);
    }

    [Fact]
    public void Constructor_IsBusy_IsFalse()
    {
        var vm = CreateViewModel();

        Assert.False(vm.IsBusy);
    }

    [Fact]
    public void Constructor_EnableListView_IsFalse()
    {
        var vm = CreateViewModel();

        Assert.False(vm.EnableListView);
    }

    [Fact]
    public void Constructor_NoJourneyFound_IsFalse()
    {
        var vm = CreateViewModel();

        Assert.False(vm.NoJourneyFound);
    }

    [Fact]
    public void Constructor_WithEmptyStationList_StationOptionsIsEmpty()
    {
        var vm = CreateViewModel([]);

        Assert.Empty(vm.StationOptions);
    }

    #endregion

    #region Property Change Notification Tests

    [Fact]
    public void IsBusy_SetValue_RaisesPropertyChanged()
    {
        var vm = CreateViewModel();
        var raised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.IsBusy)) raised = true;
        };

        vm.IsBusy = true;

        Assert.True(raised);
        Assert.True(vm.IsBusy);
    }

    [Fact]
    public void NoJourneyMsg_SetValue_RaisesPropertyChanged()
    {
        var vm = CreateViewModel();
        var raised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.NoJourneyMsg)) raised = true;
        };

        vm.NoJourneyMsg = "No journeys found";

        Assert.True(raised);
        Assert.Equal("No journeys found", vm.NoJourneyMsg);
    }

    [Fact]
    public void RefreshTime_SetValue_RaisesPropertyChanged()
    {
        var vm = CreateViewModel();
        var raised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.RefreshTime)) raised = true;
        };

        vm.RefreshTime = "12:00:00 pm";

        Assert.True(raised);
        Assert.Equal("12:00:00 pm", vm.RefreshTime);
    }

    [Fact]
    public void ButtonText_SetValue_RaisesPropertyChanged()
    {
        var vm = CreateViewModel();
        var raised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.ButtonText)) raised = true;
        };

        vm.ButtonText = "Search";

        Assert.True(raised);
        Assert.Equal("Search", vm.ButtonText);
    }

    [Fact]
    public void NoJourneyFound_SetValue_RaisesPropertyChanged()
    {
        var vm = CreateViewModel();
        var raised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.NoJourneyFound)) raised = true;
        };

        vm.NoJourneyFound = true;

        Assert.True(raised);
        Assert.True(vm.NoJourneyFound);
    }

    [Fact]
    public void EnableListView_SetValue_RaisesPropertyChanged()
    {
        var vm = CreateViewModel();
        var raised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.EnableListView)) raised = true;
        };

        vm.EnableListView = true;

        Assert.True(raised);
        Assert.True(vm.EnableListView);
    }

    [Fact]
    public void IsRefreshing_SetValue_RaisesPropertyChanged()
    {
        var vm = CreateViewModel();
        var raised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.IsRefreshing)) raised = true;
        };

        vm.IsRefreshing = true;

        Assert.True(raised);
        Assert.True(vm.IsRefreshing);
    }

    [Fact]
    public void RefreshCountdown_SetValue_RaisesPropertyChanged()
    {
        var vm = CreateViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);

        vm.RefreshCountdown = 30;

        Assert.Contains(nameof(vm.RefreshCountdown), changedProperties);
        Assert.Contains(nameof(vm.RefreshStatusText), changedProperties);
        Assert.Equal(30, vm.RefreshCountdown);
    }

    [Fact]
    public void RefreshCountdown_SameValue_DoesNotRaisePropertyChanged()
    {
        var vm = CreateViewModel();
        vm.RefreshCountdown = 30;

        var raised = false;
        vm.PropertyChanged += (s, e) => raised = true;

        vm.RefreshCountdown = 30;

        Assert.False(raised);
    }

    #endregion

    #region Property Logic Tests

    [Fact]
    public void SelectedStation_SetValue_DisablesListViewAndResetsButtonText()
    {
        var vm = CreateViewModel();
        vm.EnableListView = true;

        vm.SelectedStation = "Dublin Connolly";

        Assert.False(vm.EnableListView);
        Assert.Equal("Dublin Connolly", vm.SelectedStation);
    }

    [Fact]
    public void IsFindNearbyStationChecked_SetTrue_SetsHideSearchButton_DisablesListView()
    {
        var vm = CreateViewModel();
        vm.EnableListView = true;

        vm.IsFindNearbyStationChecked = true;

        Assert.True(vm.IsFindNearbyStationChecked);
        Assert.True(vm.HideSearchButton);
        Assert.False(vm.EnableListView);
    }

    [Fact]
    public void IsFindNearbyStationChecked_SetSameValue_DoesNotRaisePropertyChanged()
    {
        var vm = CreateViewModel();
        vm.IsFindNearbyStationChecked = true;

        var raised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.IsFindNearbyStationChecked)) raised = true;
        };

        vm.IsFindNearbyStationChecked = true;

        Assert.False(raised);
    }

    [Fact]
    public void IsLocateStationChecked_SetTrue_SetsStationListLoaded_ResetsStationOptions()
    {
        var stations = new List<Station>
        {
            new() { StationDesc = "Cork", StationCode = "CORK" },
            new() { StationDesc = "Dublin Connolly", StationCode = "CNLLY" }
        };
        var vm = CreateViewModel(stations);

        vm.IsLocateStationChecked = true;

        Assert.True(vm.IsLocateStationChecked);
        Assert.True(vm.IsStationListLoaded);
        Assert.False(vm.EnableListView);
        Assert.Equal(2, vm.StationOptions.Count);
    }

    [Fact]
    public void IsLocateStationChecked_SetSameValue_DoesNotRaisePropertyChanged()
    {
        var vm = CreateViewModel();
        vm.IsLocateStationChecked = true;

        var raised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.IsLocateStationChecked)) raised = true;
        };

        vm.IsLocateStationChecked = true;

        Assert.False(raised);
    }

    [Fact]
    public void HideSearchButton_SetSameValue_DoesNotRaisePropertyChanged()
    {
        var vm = CreateViewModel();
        vm.HideSearchButton = true;

        var raised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.HideSearchButton)) raised = true;
        };

        vm.HideSearchButton = true;

        Assert.False(raised);
    }

    [Fact]
    public void IsStationListLoaded_SetSameValue_DoesNotRaisePropertyChanged()
    {
        var vm = CreateViewModel();
        vm.IsStationListLoaded = true;

        var raised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.IsStationListLoaded)) raised = true;
        };

        vm.IsStationListLoaded = true;

        Assert.False(raised);
    }

    [Fact]
    public void IsRefreshing_SetSameValue_DoesNotRaisePropertyChanged()
    {
        var vm = CreateViewModel();
        vm.IsRefreshing = true;

        var raised = false;
        vm.PropertyChanged += (s, e) => raised = true;

        vm.IsRefreshing = true;

        Assert.False(raised);
    }

    #endregion

    #region RefreshStatusText Tests

    [Fact]
    public void RefreshStatusText_WhenAutoRefreshDisabled_ReturnsNonCountdownText()
    {
        var vm = CreateViewModel();

        Assert.DoesNotContain("Refreshing in", vm.RefreshStatusText);
    }

    #endregion

    #region Messaging Tests

    [Fact]
    public void Receive_AutoRefreshSettingsChanged_Disabled_SetsCountdownToZero()
    {
        var vm = CreateViewModel();
        vm.RefreshCountdown = 30;

        vm.Receive(new AutoRefreshSettingsChangedMessage(false, 30));

        Assert.Equal(0, vm.RefreshCountdown);
    }

    [Fact]
    public void Receive_AutoRefreshSettingsChanged_EnabledButListViewDisabled_DoesNotChangeCountdown()
    {
        var vm = CreateViewModel();
        vm.EnableListView = false;
        vm.RefreshCountdown = 0;

        vm.Receive(new AutoRefreshSettingsChangedMessage(true, 60));

        Assert.Equal(0, vm.RefreshCountdown);
    }

    [Fact]
    public void OnAppearing_RegistersForMessages()
    {
        var vm = CreateViewModel();

        var exception = Record.Exception(() => vm.OnAppearing());

        Assert.Null(exception);
    }

    [Fact]
    public void OnAppearing_CalledTwice_DoesNotThrow()
    {
        var vm = CreateViewModel();

        vm.OnAppearing();
        var exception = Record.Exception(() => vm.OnAppearing());

        Assert.Null(exception);

        vm.OnDisappearing();
    }

    [Fact]
    public void OnDisappearing_UnregistersMessages()
    {
        var vm = CreateViewModel();
        vm.OnAppearing();

        var exception = Record.Exception(() => vm.OnDisappearing());

        Assert.Null(exception);
    }

    [Fact]
    public void OnDisappearing_WithoutOnAppearing_DoesNotThrow()
    {
        var vm = CreateViewModel();

        var exception = Record.Exception(() => vm.OnDisappearing());

        Assert.Null(exception);
    }

    #endregion

    #region ErrorOccurred Tests

    [Fact]
    public void SearchServiceCommand_WithNoSelectedStation_RaisesError()
    {
        var vm = CreateViewModel();
        vm.SelectedStation = string.Empty;

        string? errorMessage = null;
        vm.ErrorOccurred += (s, e) => errorMessage = e;

        vm.SearchServiceCommand.Execute(null);

        Assert.NotNull(errorMessage);
    }

    #endregion
}
