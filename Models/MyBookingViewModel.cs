namespace UniBusApp.Models
{
    public class MyBookingViewModel
    {
        public int BookingId { get; set; }
        public int TripId { get; set; }

        public string BookingStatus { get; set; } = string.Empty;
        public string TripStatus { get; set; } = string.Empty;
        public string DirectionName { get; set; } = string.Empty;
        public string MetroStationName { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string BusPlate { get; set; } = string.Empty;

        public DateOnly TripDate { get; set; }
        public TimeOnly DepartureTime { get; set; }
        public TimeOnly ArrivalTime { get; set; }

        public int AvailableSeats { get; set; }
        public int TotalSeats { get; set; }
    }
}