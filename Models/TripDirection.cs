using System;
using System.Collections.Generic;

namespace UniBusApp.Models;

public partial class TripDirection
{
    public int direction_id { get; set; }

    public string? direction_name { get; set; }

    public virtual ICollection<ShuttleTrip> ShuttleTrips { get; set; } = new List<ShuttleTrip>();
}
