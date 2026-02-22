using iRailTracker.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
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

                // Fetch station list
                var stationService = new StationService();
                var stationList = await stationService.GetAllStationsAsync(settings, errorCallback);
                if (stationList == null)
                {
                    errorCallback?.Invoke("Failed to load station list.");
                    return;
                }

                _stationListService.Data = stationList;
            }
            catch (Exception ex)
            {
                errorCallback?.Invoke($"An error occurred while loading settings: {ex.Message}");
            }
        }
    }
}
