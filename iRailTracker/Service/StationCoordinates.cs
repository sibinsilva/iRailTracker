using iRailTracker.Model;

namespace iRailTracker.Service
{
    /// <summary>
    /// Repairs bad station coordinates from the Irish Rail API. Some stations are listed several
    /// times under one name with different codes, and one entry can carry wrong coordinates:
    /// Adamstown is ADAMS, ADAMF and ADMTN, and ADAMS has longitude -7 (about 40 km west, near
    /// Edenderry) where the others have -6.45233. Lookups by name found ADAMS first, so the map
    /// showed an Adamstown pin in the wrong place.
    /// </summary>
    public static class StationCoordinates
    {
        /// <summary>
        /// For each station name listed more than once, gives entries whose coordinates disagree
        /// with the majority the coordinates most entries agree on. Ties are left alone, since
        /// there's no way to tell which entry is right. Returns the same list, changed in place.
        /// </summary>
        public static List<Station> Repair(List<Station> stations)
        {
            foreach (var group in stations.GroupBy(s => s.StationDesc.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                var entries = group.ToList();
                if (entries.Count < 2)
                    continue;

                var byCoordinates = entries
                    .GroupBy(s => (s.StationLatitude, s.StationLongitude))
                    .OrderByDescending(g => g.Count())
                    .ToList();

                if (byCoordinates.Count < 2 || byCoordinates[0].Count() == byCoordinates[1].Count())
                    continue;

                var (lat, lon) = byCoordinates[0].Key;
                foreach (var station in byCoordinates.Skip(1).SelectMany(g => g))
                {
                    station.StationLatitude = lat;
                    station.StationLongitude = lon;
                }
            }

            return stations;
        }
    }
}
