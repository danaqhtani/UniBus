using System;
using System.Collections.Generic;

namespace UniBusApp.Models;

public partial class VwTripSummary
{
    public int trip_id { get; set; }

    public DateOnly trip_date { get; set; }

    public TimeOnly departure_time { get; set; }

    public int direction_id { get; set; }

    public int total_seats { get; set; }

    public int available_seats { get; set; }

    public int? booked_seats { get; set; }

    public string status { get; set; } = null!;

    public int? driver_id { get; set; }

    public int metro_id { get; set; }
}
