using System;
using System.Collections.Generic;

namespace UniBusApp.Models;

public class TripTrackingSnapshot
{
    public int TripId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string DirectionName { get; set; } = string.Empty;

    public string DepartureTime { get; set; } = string.Empty;

    public string DriverName { get; set; } = string.Empty;

    public string BusPlate { get; set; } = string.Empty;

    public int Passengers { get; set; }

    public int Capacity { get; set; }

    public TrackingPoint? StartLocation { get; set; }

    public TrackingPoint? EndLocation { get; set; }

    public List<TrackingPoint> Stops { get; set; } = new();

    public List<TrackingPoint> OptimizedRoute { get; set; } = new();

    public string? EstimatedArrivalTime { get; set; }

    public int? StudentStopBuildingId { get; set; }

    public string? StudentStopLabel { get; set; }

    public string? StudentStopName { get; set; }

    public string? StudentEstimatedArrivalTime { get; set; }

    public int? StudentEtaMinutes { get; set; }

    public bool StudentArrivingSoon { get; set; }

    public GoogleRouteMetric? Distance { get; set; }

    public GoogleRouteMetric? Duration { get; set; }

    public bool HasRoadPolyline { get; set; }

    public string? PolylineFailureReason { get; set; }

    public string? Polyline { get; set; }

    public LiveBusLocation? LiveBusLocation { get; set; }
}

public class TrackingPoint
{
    public int? Id { get; set; }

    public int? BuildingId { get; set; }

    public string Name { get; set; } = string.Empty;

    public double Lat { get; set; }

    public double Lng { get; set; }

    public int? StopOrder { get; set; }

    public string? MarkerLabel { get; set; }

    public string Type { get; set; } = string.Empty;

    public string? EstimatedArrivalTime { get; set; }

    public int? EtaMinutes { get; set; }

    public bool IsArrivingSoon { get; set; }
}

public class GoogleRouteMetric
{
    public string Text { get; set; } = string.Empty;

    public int Value { get; set; }
}

public class LiveBusLocation
{
    public double Lat { get; set; }

    public double Lng { get; set; }

    public string? Timestamp { get; set; }
}

public class StudentStopEta
{
    public int BuildingId { get; set; }

    public string? StopLabel { get; set; }

    public string StopName { get; set; } = string.Empty;

    public string? EstimatedArrivalTime { get; set; }

    public int RemainingMinutes { get; set; }

    public bool IsArrivingSoon { get; set; }
}
