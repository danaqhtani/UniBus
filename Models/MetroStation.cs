using System;
using System.Collections.Generic;

namespace UniBusApp.Models;

public partial class MetroStation
{
    public int metro_id { get; set; }

    public string station_name { get; set; } = null!;

    public decimal latitude { get; set; }

    public decimal longitude { get; set; }

    public virtual ICollection<ShuttleTrip> ShuttleTrips { get; set; } = new List<ShuttleTrip>();
}
