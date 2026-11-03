using iRailTracker.Resources.Strings;
using iRailTracker.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace iRailTracker.Service
{
    public class ConfigLoader
    {
        private readonly DataService<Settings> _settingsService;
        private readonly DataService<List<Station>> _stationListService;

        public ConfigLoader(DataService<Settings> settingsService, DataService<List<Station>> stationListService)
        {
            _settingsService = settingsService;
            _stationListService = stationListService;
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Settings))]
        public async Task LoadSettingsAsync(IConfiguration configuration, Action<string>? errorCallback = null)
        {
            try
            {
                // Load settings from configuration
                var settings = configuration.GetRequiredSection("Settings").Get<Settings>();
                if (settings == null)
                {
                    errorCallback?.Invoke("Failed to load settings from configuration.");
                    return;
                }

                _settingsService.Data = settings;

                var stationService = new StationService();

                // Start from the copy saved on the device: instant, and works offline. Refresh it
                // in the background so the next start picks up any changes.
                var cached = StationListCache.Load();
                if (cached.Count > 0)
                {
                    _stationListService.Data = cached;
                    _ = RefreshStationCacheAsync(stationService, settings);
                    return;
                }

                // First start (or cache lost): the list has to come from the network
                string? fetchError = null;
                var stationList = await stationService.GetAllStationsAsync(settings, e => fetchError = e);
                if (stationList.Count == 0)
                {
                    errorCallback?.Invoke(fetchError is null
                        ? "Failed to load station list."
                        : AppMessages.StationListDownloadFailed);
                    return;
                }

                StationListCache.Save(stationList);
                _stationListService.Data = stationList;
            }
            catch (Exception ex)
            {
                errorCallback?.Invoke($"An error occurred while loading settings: {ex.Message}");
            }
        }

        private static async Task RefreshStationCacheAsync(StationService stationService, Settings settings)
        {
            var fresh = await stationService.GetAllStationsAsync(settings, null);
            StationListCache.Save(fresh);
        }
    }
}
