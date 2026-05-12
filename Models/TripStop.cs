using System;
using System.Collections.Generic;

namespace UniBusApp.Models;

public partial class TripStop
{
    public int stop_id { get; set; }

    public int trip_id { get; set; }

    public int building_id { get; set; }

    public int stop_order { get; set; }

    public virtual Building Building { get; set; } = null!;

    public virtual ShuttleTrip Trip { get; set; } = null!;
}
