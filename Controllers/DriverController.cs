using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniBusApp.Data;
using UniBusApp.Models;
using UniBusApp.Services;

namespace UniBusApp.Controllers
{
    public class DriverController : Controller
    {
        private readonly UniBusDbContext _db;
        private readonly TripGeneratorService _tripService;
        private readonly TripTrackingService _tripTrackingService;

        public DriverController(
            UniBusDbContext db,
            TripGeneratorService tripService,
            TripTrackingService tripTrackingService)
        {
            _db = db;
            _tripService = tripService;
            _tripTrackingService = tripTrackingService;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(int loginId)
        {
            var driver = await _db.Driver
                .FirstOrDefaultAsync(d => d.login_id == loginId && d.is_active);

            if (driver == null)
            {
                ViewBag.Error = "Serial number is required.";
                return View();
            }

            HttpContext.Session.SetInt32("driver_id", driver.driver_id);
            return RedirectToAction("Home");
        }

        public async Task<IActionResult> Home()
        {
            _tripService.EnsureTodayTripsGenerated();

            var driverId = HttpContext.Session.GetInt32("driver_id");
            if (driverId == null) return RedirectToAction("Login");

            var today = DateOnly.FromDateTime(DateTime.Today);
            var currentTime = TimeOnly.FromDateTime(DateTime.Now);

            var existingTrips = await _db.ShuttleTrip
                .Where(t => t.driver_id == driverId.Value && t.trip_date == today)
                .Include(t => t.Bookings)
                .ToListAsync();

            var displayTrips = existingTrips
                .Where(t =>
                    t.departure_time >= currentTime ||
                    (t.status == "Started" && t.started_at >= DateTime.Now.AddMinutes(-20)) ||
                    (t.status == "Completed" && t.ended_at >= DateTime.Now.AddMinutes(-20)))
                .OrderBy(t => t.departure_time)
                .ToList();

            return View(displayTrips);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var driverId = HttpContext.Session.GetInt32("driver_id");
            if (driverId == null) return RedirectToAction("Login");

            var driver = await _db.Driver.FindAsync(driverId.Value);
            if (driver == null) return NotFound();

            return View(driver);
        }

        [HttpGet]
        public async Task<IActionResult> GetTripRoute(int tripId)
        {
            var driverId = HttpContext.Session.GetInt32("driver_id");
            if (driverId == null) return Unauthorized();

            var trip = await LoadDriverTripAsync(driverId.Value, tripId);
            if (trip == null) return NotFound();

            var snapshot = await _tripTrackingService.BuildTrackingSnapshotAsync(tripId);
            if (snapshot == null) return NotFound();

            return Json(snapshot);
        }

        [HttpPost]
        public async Task<IActionResult> StartTrip(int id)
        {
            var driverId = HttpContext.Session.GetInt32("driver_id");
            if (driverId == null) return Unauthorized();

            var trip = await LoadDriverTripAsync(driverId.Value, id);
            if (trip == null) return Json(new { success = false, message = "Trip not found for this driver" });

            if (string.Equals(trip.status, "Completed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trip.status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, message = "Trip can no longer be started" });
            }

            if (IsTripStarted(trip.status))
            {
                return Json(new { success = true, status = trip.status });
            }

            trip.status = "Started";
            trip.started_at ??= DateTime.Now;
            trip.ended_at = null;

            await _db.SaveChangesAsync();

            return Json(new { success = true, status = trip.status });
        }

        [HttpPost]
        public async Task<IActionResult> EndTrip(int id)
        {
            var driverId = HttpContext.Session.GetInt32("driver_id");
            if (driverId == null) return Unauthorized();

            var trip = await LoadDriverTripAsync(driverId.Value, id);
            if (trip == null) return Json(new { success = false, message = "Trip not found for this driver" });

            if (string.Equals(trip.status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = true, status = trip.status });
            }

            trip.status = "Completed";
            trip.ended_at = DateTime.Now;

            await _db.SaveChangesAsync();

            return Json(new { success = true, status = trip.status });
        }

        public async Task<IActionResult> Tracking(int tripId)
        {
            var driverId = HttpContext.Session.GetInt32("driver_id");
            if (driverId == null) return RedirectToAction("Login");

            var trip = await LoadDriverTripAsync(driverId.Value, tripId);
            if (trip == null) return RedirectToAction("Home");

            ViewBag.TripId = tripId;
            ViewBag.GoogleMapsApiKey = _tripTrackingService.GoogleMapsApiKey;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetTripInfo(int tripId)
        {
            var driverId = HttpContext.Session.GetInt32("driver_id");
            if (driverId == null) return Unauthorized();

            var trip = await _db.ShuttleTrip
                .FirstOrDefaultAsync(t => t.trip_id == tripId && t.driver_id == driverId.Value);

            if (trip == null) return NotFound();

            int bookedCount = trip.total_seats - trip.available_seats;

            return Json(new
            {
                tripId = trip.trip_id,
                time = trip.departure_time.ToString(@"HH\:mm"),
                passengers = bookedCount,
                capacity = trip.total_seats,
                status = trip.status
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateLocation(int tripId, double lat, double lng)
        {
            var sessionDriverId = HttpContext.Session.GetInt32("driver_id");
            if (sessionDriverId == null) return Unauthorized();

            if (!IsValidCoordinate(lat, lng))
                return Json(new { success = false, message = "Invalid location coordinates" });

            var trip = await LoadDriverTripAsync(sessionDriverId.Value, tripId);

            if (trip == null)
                return Json(new { success = false, message = "Trip not found for this driver" });

            if (trip.status == "Completed" || trip.status == "Cancelled")
                return Json(new { success = false, message = "Trip is not active" });

            if (!IsTripStarted(trip.status))
                return Json(new { success = false, message = "Start the trip before sending location updates" });

            var loc = new Location
            {
                trip_id = tripId,
                driver_id = sessionDriverId.Value,
                latitude = (decimal)lat,
                longitude = (decimal)lng,
                timestamp = DateTime.Now
            };

            _db.Location.Add(loc);
            await _db.SaveChangesAsync();

            return Json(new
            {
                success = true,
                liveBusLocation = new
                {
                    lat = loc.latitude,
                    lng = loc.longitude,
                    timestamp = loc.timestamp
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetLatestLocation(int tripId)
        {
            var driverId = HttpContext.Session.GetInt32("driver_id");
            if (driverId == null) return Unauthorized();

            var trip = await LoadDriverTripAsync(driverId.Value, tripId);
            if (trip == null)
                return Json(new { success = false, message = "Trip not found for this driver" });

            var latest = await _tripTrackingService.GetLiveBusLocationAsync(tripId);
            if (latest == null)
                return Json(new { success = false, message = "No location yet" });

            return Json(new
            {
                success = true,
                latitude = latest.Lat,
                longitude = latest.Lng,
                timestamp = latest.Timestamp,
                driverId = driverId.Value,
                tripId
            });
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Driver");
        }

        private async Task<ShuttleTrip?> LoadDriverTripAsync(int driverId, int tripId)
        {
            return await _db.ShuttleTrip
                .FirstOrDefaultAsync(t => t.trip_id == tripId && t.driver_id == driverId);
        }

        private static bool IsTripStarted(string? status)
        {
            return string.Equals(status, "Started", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidCoordinate(double lat, double lng)
        {
            return !double.IsNaN(lat)
                && !double.IsInfinity(lat)
                && !double.IsNaN(lng)
                && !double.IsInfinity(lng)
                && lat >= -90
                && lat <= 90
                && lng >= -180
                && lng <= 180;
        }
    }
}
