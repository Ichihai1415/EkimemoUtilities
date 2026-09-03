using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace EkimemoUtilities.Utils
{
    public class WhatPolygonIs//by copilot　を微調整したのみ
    {


        public static bool IsPointInPolygon(List<PointF> polygon, PointF testPoint)
        {
            bool result = false;
            int j = polygon.Count - 1;

            for (int i = 0; i < polygon.Count; i++)
            {
                if ((polygon[i].Y < testPoint.Y && polygon[j].Y >= testPoint.Y) ||
                    (polygon[j].Y < testPoint.Y && polygon[i].Y >= testPoint.Y))
                {
                    if (polygon[i].X +
                        (testPoint.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) *
                        (polygon[j].X - polygon[i].X) < testPoint.X)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }

        public static GeoJSON.Feature? FindContainingPolygon(GeoJSON.FeatureCollection fc, double lat, double lon)
        {
            var pt = new PointF((float)lon, (float)lat);

            foreach (var f in fc.features)
            {
                if (f.geometry == null) continue;

                if (f.geometry.type == "Polygon")
                {

                    var rings = JsonSerializer.Deserialize<List<List<List<double>>>>(
                        f.geometry.coordinates.ToString()
                    );

                    foreach (var ring in rings)
                    {
                        var poly = ring.Select(c => new PointF((float)c[0], (float)c[1])).ToList();
                        if (IsPointInPolygon(poly, pt))
                            return f;
                    }
                }
                else if (f.geometry.type == "MultiPolygon")
                {
                    var polys = JsonSerializer.Deserialize<List<List<List<List<double>>>>>(
                        f.geometry.coordinates.ToString()
                    );

                    foreach (var polygon in polys)
                    {
                        foreach (var ring in polygon)
                        {
                            var poly = ring.Select(c => new PointF((float)c[0], (float)c[1])).ToList();
                            if (IsPointInPolygon(poly, pt))
                                return f;
                        }
                    }
                }
            }

            return null; // どのポリゴンにも属さない
        }


        public class GeoJSON
        {
            public GeoJSON(string json)
            {
                FC = JsonSerializer.Deserialize<FeatureCollection>(json);
            }

            public (string?, string?, double?, double?) FindName(double lat, double lon)
            {
                var prop = FindContainingPolygon(FC, lat, lon)?.properties;
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
}
