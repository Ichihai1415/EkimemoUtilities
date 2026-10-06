using System.Text.Json;

namespace EkimemoUtilities.Utils
{
    public class Common
    {
        public static double GetDistance(double lat1, double lon1, double lat2, double lon2)// 単位: メートル
        {
            //global::Android.Util.Log.Debug("Ichihai1415.EkimemoUtilities", $"[Common.GetDistance]{lat1}, {lon1} / {lat2}, {lon2}");

            const double earthRadius = 6371000;

            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadius * c;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }

    public class GeoJSON
    {
        public GeoJSON(string json)
        {
            FC = JsonSerializer.Deserialize<FeatureCollection>(json)!;
            //global::Android.Util.Log.Debug("Ichihai1415.EkimemoUtilities", $"[GeoJSON]FC.feature.count={FC.features.Count}");
        }

        public (string?, string?, double?, double?) FindName(double lat, double lon)
        {
            var prop = WhatPolygonIs.FindContainingPolygon(FC, lat, lon)?.properties;
            return (prop?.name, prop?.attr, prop?.lat, prop?.lng);
        }

        public FeatureCollection FC { get; set; }

        public class FeatureCollection
        {
            public string type { get; set; }
            public List<Feature> features { get; set; }
        }

        public class Feature
        {
            public string type { get; set; }
            public Geometry geometry { get; set; }
            public Property properties { get; set; }
        }

        public class Geometry
        {
            public string type { get; set; }
            public object coordinates { get; set; }
        }


        public class Property
        {
            public string name { get; set; }
            public string attr { get; set; }
            public double lat { get; set; }
            public double lng { get; set; }

        }

    }

}
