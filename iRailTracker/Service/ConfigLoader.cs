using iRailTracker.Model;
using Microsoft.Extensions.Configuration;

namespace iRailTracker.Service
{
    public class ConfigLoader
    {
        private readonly DataService<Settings> _settingsService;
        public readonly DataService<List<Station>> _stationListService;

        public ConfigLoader(DataService<Settings> settingsService, DataService<List<Station>> stationListService)
        {
            _settingsService = settingsService;
            _stationListService = stationListService;
        }

        public async Task LoadSettingsAsync(IConfiguration configuration)
        {
            var settings = configuration.GetRequiredSection("Settings").Get<Settings>();
            _settingsService.Data = settings;

            var stationService = new StationService();
            var stationList = await stationService.GetAllStationsAsync(settings);
            _stationListService.Data = stationList;
        }
    }

}
