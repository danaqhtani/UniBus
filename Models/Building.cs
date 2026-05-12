using System;
using System.Collections.Generic;

namespace UniBusApp.Models;

public partial class Building
{
    public int building_id { get; set; }

    public string building_name { get; set; } = null!;

    public decimal latitude { get; set; }

    public decimal longitude { get; set; }

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<TripStop> TripStops { get; set; } = new List<TripStop>();
}
