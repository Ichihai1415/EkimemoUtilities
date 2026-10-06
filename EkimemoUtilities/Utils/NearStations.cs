namespace EkimemoUtilities.Utils
{
    internal class NearStations
    {
        static (double minLat, double maxLat, double minLon, double maxLon) BoundingBox(double lat, double lon, double radiusKm) //copilot//高緯度、広範囲でなければ使用可
        {
            const double degLatPerKm = 1.05 / 110.574;//1.05にしているのはマージン
            const double degLonPerKm = 1.05 / 111.320;

            double dLat = radiusKm * degLatPerKm;
            double dLon = radiusKm * degLonPerKm * Math.Cos(lat * Math.PI / 180.0);

            return (lat - dLat, lat + dLat, lon - dLon, lon + dLon);
        }

        public static (string Name, int Distance)[] GetNearStations(GeoJSON.FeatureCollection fc, double lat, double lon, int distKm, int maxCount = int.MaxValue)
        {
            if (distKm == 0 || maxCount == 0)
                return [];

            var (minLat, maxLat, minLon, maxLon) = BoundingBox(lat, lon, distKm);

            var candidates = fc.features
                //.AsParallel() //並列化
                .Where(p =>
                    p.properties.lat >= minLat && p.properties.lat <= maxLat &&
                    p.properties.lng >= minLon && p.properties.lng <= maxLon)
                .ToList();

            var seen = new HashSet<string>();
            var result = new List<(string Name, int Distance)>();
            foreach (var f in candidates)
            {
                var dist = Common.GetDistance(lat, lon, f.properties.lat, f.properties.lng);
                if (dist <= distKm * 1000)
                    if (seen.Add(f.properties.name))
                        result.Add((f.properties.name, (int)dist));
            }
            result.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            return [.. result.Take(Math.Min(result.Count, maxCount))];
        }


    }
}
