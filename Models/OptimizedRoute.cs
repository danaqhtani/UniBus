using System;
using System.Collections.Generic;

namespace UniBusApp.Models;

public partial class OptimizedRoute
{
    public int route_id { get; set; }

    public int trip_id { get; set; }

    public string? route_order { get; set; }

    public double? total_distance { get; set; }

    public DateTime? created_at { get; set; }

    public virtual ShuttleTrip Trip { get; set; } = null!;
}
