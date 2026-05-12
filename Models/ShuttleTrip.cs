using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniBusApp.Models;

public partial class ShuttleTrip
{
    public int trip_id { get; set; }

    public TimeOnly departure_time { get; set; }

    public TimeOnly arrival_time { get; set; }

    public DateOnly trip_date { get; set; }

    public int total_seats { get; set; }

    public int available_seats { get; set; }

    public int? driver_id { get; set; }

    public string status { get; set; } = null!;

    public int metro_id { get; set; }

    public DateTime? started_at { get; set; }

    public DateTime? ended_at { get; set; }

    public int direction_id { get; set; }

    [NotMapped]
    public virtual TripDirection? Direction { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

   

    public virtual Driver? Driver { get; set; }

    public virtual ICollection<Location> Locations { get; set; } = new List<Location>();

    public virtual MetroStation Metro { get; set; } = null!;

    public virtual OptimizedRoute? OptimizedRoute { get; set; }

    public virtual ICollection<TripStop> TripStops { get; set; } = new List<TripStop>();
}