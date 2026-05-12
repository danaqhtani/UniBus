using System.Collections.Generic;

namespace UniBus.Models
{
    public class HomeViewModel
    {
        // Student info loaded from database
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public string PhoneNumber { get; set; }
        public int BuildingId { get; set; }

        // Friendly campus label for UI
        public string CampusLabel { get; set; }

        // Home page sections
        public List<RouteCardViewModel> AvailableRoutes { get; set; }
        public BookingCardViewModel CurrentBooking { get; set; }

        public HomeViewModel()
        {
            AvailableRoutes = new List<RouteCardViewModel>();
        }
    }

    public class RouteCardViewModel
    {
        public string RouteCode { get; set; }
        public string RouteTitle { get; set; }
        public string RouteSubtitle { get; set; }
        public string RouteDirection { get; set; }
        public string AccentColorClass { get; set; }
        public string IconName { get; set; }
        public int RidersCount { get; set; }
        public string ViewTimesUrl { get; set; }
    }

    public class BookingCardViewModel
    {
        public bool HasBooking { get; set; }

        public string TripCode { get; set; }
        public string RouteName { get; set; }
        public string StartPoint { get; set; }
        public string EndPoint { get; set; }
        public string DepartureTime { get; set; }
        public string EstimatedArrival { get; set; }
        public string DriverName { get; set; }
        public string SeatNumber { get; set; }

        public string TrackUrl { get; set; }
        public string CancelUrl { get; set; }
    }
}