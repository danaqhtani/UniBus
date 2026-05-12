using UniBusApp.Models;

namespace UniBusApp.ViewModels
{
    public class TripDetailsVM
    {
        public ShuttleTrip Trip { get; set; }
        public List<TripStop> Stops { get; set; }
    }
}
