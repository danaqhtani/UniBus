using System;
using System.Collections.Generic;

namespace UniBusApp.Models;

public partial class Location
{
    public int location_id { get; set; }

    public decimal? latitude { get; set; }

    public decimal? longitude { get; set; }

    public DateTime? timestamp { get; set; }

    public int? driver_id { get; set; }

    public int? trip_id { get; set; }

    public virtual Driver? Driver { get; set; }

    public virtual ShuttleTrip? Trip { get; set; }
}
