using System;
using System.Collections.Generic;
using System.Text;

namespace EkimemoUtilities.Services;

public readonly record struct LocationInfo(
    double Latitude,
    double Longitude,
    double? Altitude,
    double? AccuracyMeters,
    double? Speed,
    DateTime Timestamp,
    string? Provider);