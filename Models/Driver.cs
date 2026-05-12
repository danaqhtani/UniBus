using System;
using System.Collections.Generic;

namespace UniBusApp.Models;

public partial class Driver
{
    public int driver_id { get; set; }

    public string driver_name { get; set; } = null!;

    public string? phone_number { get; set; }

    public string? bus_plate { get; set; }

    public string? bus_color { get; set; }

    public int login_id { get; set; }

    public bool is_active { get; set; }

    public virtual ICollection<Location> Locations { get; set; } = new List<Location>();

    public virtual ICollection<ShuttleTrip> ShuttleTrips { get; set; } = new List<ShuttleTrip>();
}
