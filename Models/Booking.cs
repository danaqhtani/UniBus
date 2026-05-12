using System;
using System.Collections.Generic;

namespace UniBusApp.Models;

public partial class Booking
{
    public int booking_id { get; set; }

    public DateTime? booking_time { get; set; }

    public string? booking_status { get; set; }

    public int student_id { get; set; }

    public int trip_id { get; set; }

    public virtual Student Student { get; set; } = null!;

    public virtual ShuttleTrip Trip { get; set; } = null!;
}
