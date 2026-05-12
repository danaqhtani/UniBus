using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UniBusApp.Data;
using UniBusApp.Models;
using UniBusApp.Services;

namespace UniBusApp.Controllers
{
    public class StudentController : Controller
    {
        private readonly UniBusDbContext _context;
        private readonly TripGeneratorService _tripService;
        private readonly TripTrackingService _tripTrackingService;


        public StudentController(
            UniBusDbContext context,
            TripGeneratorService tripService,
            TripTrackingService tripTrackingService)
        {
            _context = context;
            _tripService = tripService;
            _tripTrackingService = tripTrackingService;
        }

        private int? GetCurrentStudentId()
        {
            return HttpContext.Session.GetInt32("StudentId");
        }

        private void LoadBuildings(int? selectedBuildingId = null)
        {
            ViewBag.Buildings = _context.Building
                .Select(b => new SelectListItem
                {
                    Value = b.building_id.ToString(),
                    Text = b.building_name,
                    Selected = selectedBuildingId.HasValue && b.building_id == selectedBuildingId.Value
                })
                .ToList();
        }

        public IActionResult Dashboard()
        {
            return View();
        }

  

        public IActionResult TodayTrips(int direction = 1)
        {
            var studentId = GetCurrentStudentId();

            if (studentId == null)
                return RedirectToAction("Login", "Account");

            _tripService.EnsureTodayTripsGenerated();

            ViewBag.Direction = direction;

            var today = DateOnly.FromDateTime(DateTime.Today);
            var nowTime = TimeOnly.FromDateTime(DateTime.Now);

            var trips = _context.ShuttleTrip
                .Where(t =>
                    t.trip_date == today &&
                    t.direction_id == direction &&
                    t.available_seats > 0 &&
                    t.status == "Scheduled" &&
                    t.departure_time > nowTime)
                .OrderBy(t => t.departure_time)
                .Select(t => new ShuttleTrip
                {
                    trip_id = t.trip_id,
                    departure_time = t.departure_time,
                    arrival_time = t.arrival_time,
                    trip_date = t.trip_date,
                    total_seats = t.total_seats,
                    available_seats = t.available_seats,
                    driver_id = t.driver_id,
                    metro_id = t.metro_id,
                    status = t.status,
                    started_at = t.started_at,
                    ended_at = t.ended_at,
                    direction_id = t.direction_id,

                    Metro = t.Metro == null ? null! : new MetroStation
                    {
                        metro_id = t.Metro.metro_id,
                        station_name = t.Metro.station_name,
                        latitude = t.Metro.latitude,
                        longitude = t.Metro.longitude
                    },

                    Driver = t.Driver == null ? null : new Driver
                    {
                        driver_id = t.Driver.driver_id,
                        driver_name = t.Driver.driver_name,
                        phone_number = t.Driver.phone_number,
                        bus_plate = t.Driver.bus_plate,
                        bus_color = t.Driver.bus_color,
                        login_id = t.Driver.login_id,
                        is_active = t.Driver.is_active
                    },

                    Direction = t.Direction == null ? null : new TripDirection
                    {
                        direction_id = t.Direction.direction_id,
                        direction_name = t.Direction.direction_name
                    }
                })
                .ToList();

            return View(trips);
        }

        public IActionResult TripDetails(int id)
        {
            var studentId = GetCurrentStudentId();

            if (studentId == null)
                return RedirectToAction("Login", "Account");

            var trip = _context.ShuttleTrip
                .Where(t => t.trip_id == id)
                .Select(t => new ShuttleTrip
                {
                    trip_id = t.trip_id,
                    departure_time = t.departure_time,
                    arrival_time = t.arrival_time,
                    trip_date = t.trip_date,
                    total_seats = t.total_seats,
                    available_seats = t.available_seats,
                    driver_id = t.driver_id,
                    metro_id = t.metro_id,
                    status = t.status,
                    started_at = t.started_at,
                    ended_at = t.ended_at,
                    direction_id = t.direction_id,

                    Metro = t.Metro == null ? null! : new MetroStation
                    {
                        metro_id = t.Metro.metro_id,
                        station_name = t.Metro.station_name,
                        latitude = t.Metro.latitude,
                        longitude = t.Metro.longitude
                    },

                    Driver = t.Driver == null ? null : new Driver
                    {
                        driver_id = t.Driver.driver_id,
                        driver_name = t.Driver.driver_name,
                        phone_number = t.Driver.phone_number,
                        bus_plate = t.Driver.bus_plate,
                        bus_color = t.Driver.bus_color,
                        login_id = t.Driver.login_id,
                        is_active = t.Driver.is_active
                    },

                    Direction = t.Direction == null ? null : new TripDirection
                    {
                        direction_id = t.Direction.direction_id,
                        direction_name = t.Direction.direction_name
                    },

                    TripStops = t.TripStops
                        .OrderBy(ts => ts.stop_order)
                        .Select(ts => new TripStop
                        {
                            stop_id = ts.stop_id,
                            trip_id = ts.trip_id,
                            building_id = ts.building_id,
                            stop_order = ts.stop_order,
                            Building = ts.Building == null ? null : new Building
                            {
                                building_id = ts.Building.building_id,
                                building_name = ts.Building.building_name,
                                latitude = ts.Building.latitude,
                                longitude = ts.Building.longitude
                            }
                        })
                        .ToList()
                })
                .FirstOrDefault();

            if (trip == null)
            {
                TempData["Error"] = "Trip not found.";
                return RedirectToAction("TodayTrips");
            }

            return View(trip);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult JoinTrip(int tripId)
        {
            var studentId = GetCurrentStudentId();

            if (studentId == null)
                return RedirectToAction("Login", "Account");

            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var nowTime = TimeOnly.FromDateTime(now);

            var trip = _context.ShuttleTrip
                .FirstOrDefault(t => t.trip_id == tripId);

            if (trip == null)
            {
                TempData["Error"] = "Trip not found.";
                return RedirectToAction("TodayTrips");
            }

            if (trip.trip_date != today)
            {
                TempData["Error"] = "You can only book today's trips.";
                return RedirectToAction("TodayTrips", new { direction = trip.direction_id });
            }

            if (trip.status != "Scheduled" || trip.departure_time <= nowTime)
            {
                TempData["Error"] = "This trip is no longer available for booking.";
                return RedirectToAction("TodayTrips", new { direction = trip.direction_id });
            }

            if (trip.available_seats <= 0)
            {
                TempData["Error"] = "No seats available on this trip.";
                return RedirectToAction("TodayTrips", new { direction = trip.direction_id });
            }

            bool alreadyBooked = _context.Booking
                .Any(b => b.student_id == studentId.Value && b.trip_id == tripId);

            if (alreadyBooked)
            {
                TempData["Error"] = "You have already booked this trip.";
                return RedirectToAction("TripDetails", new { id = tripId });
            }

            try
            {
                _context.Database.ExecuteSqlRaw(@"
            INSERT INTO Booking (booking_time, booking_status, student_id, trip_id)
            VALUES ({0}, {1}, {2}, {3})",
                    now, "Booked", studentId.Value, tripId);

                TempData["Success"] = "Trip booked successfully.";
                return RedirectToAction("MyBookings");
            }
            catch
            {
                TempData["Error"] = "Something went wrong while booking the trip.";
                return RedirectToAction("TripDetails", new { id = tripId });
            }
        }

        public IActionResult MyBookings()
        {
            var studentId = GetCurrentStudentId();

            if (studentId == null)
                return RedirectToAction("Login", "Account");

            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var nowTime = TimeOnly.FromDateTime(now);

            var bookings = _context.Booking
                .Include(b => b.Trip)
                    .ThenInclude(t => t.Driver)
                .Include(b => b.Trip)
                    .ThenInclude(t => t.Metro)
                .Include(b => b.Trip)
                    .ThenInclude(t => t.Direction)
                .Where(b =>
                    b.student_id == studentId.Value &&
                    (
                        b.Trip.trip_date > today ||
                        (b.Trip.trip_date == today && b.Trip.arrival_time > nowTime)
                    )
                )
                .OrderBy(b => b.Trip.trip_date)
                .ThenBy(b => b.Trip.departure_time)
                .Select(b => new MyBookingViewModel
                {
                    BookingId = b.booking_id,
                    TripId = b.trip_id,
                    BookingStatus = b.booking_status,
                    TripStatus = b.Trip.status,
                    DirectionName = b.Trip.Direction != null ? b.Trip.Direction.direction_name : "No Direction",
                    MetroStationName = b.Trip.Metro != null ? b.Trip.Metro.station_name : "No Station",
                    DriverName = b.Trip.Driver != null ? b.Trip.Driver.driver_name : "No Driver",
                    BusPlate = b.Trip.Driver != null ? b.Trip.Driver.bus_plate : "No Bus",
                    TripDate = b.Trip.trip_date,
                    DepartureTime = b.Trip.departure_time,
                    ArrivalTime = b.Trip.arrival_time,
                    AvailableSeats = b.Trip.available_seats,
                    TotalSeats = b.Trip.total_seats
                })
                .ToList();

            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelBooking(int bookingId)
        {
            var studentId = GetCurrentStudentId();

            if (studentId == null)
                return RedirectToAction("Login", "Account");

            var booking = _context.Booking
                .FirstOrDefault(b => b.booking_id == bookingId && b.student_id == studentId.Value);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction("MyBookings");
            }

            using var transaction = _context.Database.BeginTransaction();

            try
            {
                // نرجع المقعد للرحلة
                _context.Database.ExecuteSqlRaw(@"
            UPDATE ShuttleTrip
            SET available_seats = available_seats + 1
            WHERE trip_id = {0}",
                    booking.trip_id);

                // نحذف الحجز
                _context.Database.ExecuteSqlRaw(@"
            DELETE FROM Booking
            WHERE booking_id = {0} AND student_id = {1}",
                    bookingId, studentId.Value);

                transaction.Commit();

                TempData["Success"] = "Booking cancelled successfully.";
                return RedirectToAction("MyBookings");
            }
            catch
            {
                transaction.Rollback();
                TempData["Error"] = "Something went wrong while cancelling the booking.";
                return RedirectToAction("MyBookings");
            }
        }

        public IActionResult Profile()
        {
            var studentId = GetCurrentStudentId();

            if (studentId == null)
                return RedirectToAction("Login", "Account");

            var student = _context.Student
                .Include(s => s.Building)
                .FirstOrDefault(s => s.student_id == studentId.Value);

            if (student == null)
                return RedirectToAction("Login", "Account");

            var model = new ProfileViewModel
            {
                StudentId = student.student_id,
                FullName = student.name,
              
                UniversityEmail = student.university_email,
                PhoneNumber = student.phone_number,
                BuildingId = student.building_id,
                CampusLabel = student.Building != null ? student.Building.building_name : "No Building"
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult EditProfile()
        {
            var studentId = GetCurrentStudentId();

            if (studentId == null)
                return RedirectToAction("Login", "Account");

            var student = _context.Student
                .Include(s => s.Building)
                .FirstOrDefault(s => s.student_id == studentId.Value);

            if (student == null)
                return RedirectToAction("Login", "Account");

            var model = new EditProfileViewModel
            {
                StudentId = student.student_id,
                FullName = student.name,
                PhoneNumber = student.phone_number,
                UniversityEmail = student.university_email,
                BuildingId = student.building_id,
                CurrentBuildingName = student.Building != null ? student.Building.building_name : null
            };

            LoadBuildings(student.building_id);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(EditProfileViewModel model)
        {
            var studentId = GetCurrentStudentId();

            if (studentId == null)
                return RedirectToAction("Login", "Account");

            if (model.StudentId != studentId.Value)
                return RedirectToAction("Profile");

            if (!ModelState.IsValid)
            {
                LoadBuildings(model.BuildingId);
                return View(model);
            }

            var student = _context.Student.FirstOrDefault(s => s.student_id == studentId.Value);

            if (student == null)
                return RedirectToAction("Login", "Account");

            student.name = model.FullName;
            student.phone_number = model.PhoneNumber;
            student.building_id = model.BuildingId;

            _context.SaveChanges();

            TempData["Success"] = "تم تحديث البيانات بنجاح";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAccount()
        {
            var studentId = GetCurrentStudentId();

            if (studentId == null)
                return RedirectToAction("Login", "Account");

            var student = _context.Student
                .FirstOrDefault(s => s.student_id == studentId.Value);

            if (student == null)
                return RedirectToAction("Login", "Account");

            _context.Student.Remove(student);
            _context.SaveChanges();

            HttpContext.Session.Clear();

            return RedirectToAction("Login", "Account");
        }

        public async Task<IActionResult> TrackTrip(int tripId)
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null)
                return RedirectToAction("Login", "Account");

            var hasBooking = await _context.Booking
                .AnyAsync(b => b.trip_id == tripId && b.student_id == studentId.Value);

            if (!hasBooking)
            {
                TempData["Error"] = "Booking not found for this trip.";
                return RedirectToAction("MyBookings");
            }

            ViewBag.TripId = tripId;
            ViewBag.GoogleMapsApiKey = _tripTrackingService.GoogleMapsApiKey;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetTrackingSnapshot(int tripId)
        {
            var studentId = GetCurrentStudentId();

            if (studentId == null)
                return Json(new { success = false, message = "Student not logged in" });

            var booking = await _context.Booking
                .Include(b => b.Student)
                .Include(b => b.Trip)
                .FirstOrDefaultAsync(b => b.trip_id == tripId && b.student_id == studentId.Value);

            if (booking == null)
                return Json(new { success = false, message = "Booking not found" });

            var snapshot = await _tripTrackingService.BuildTrackingSnapshotAsync(tripId);

            if (snapshot == null)
                return Json(new { success = false, message = "Trip not found" });

            var studentStopEta = await _tripTrackingService.BuildStudentStopEtaAsync(tripId, booking.Student.building_id);
            ApplyStudentStopEta(snapshot, booking.Student.building_id, studentStopEta);

            return Json(snapshot);
        }

        [HttpGet]
        public IActionResult GetTrackData(int tripId)
        {
            var trip = _context.ShuttleTrip
                .Include(t => t.Driver)
                .FirstOrDefault(t => t.trip_id == tripId);

            if (trip == null)
            {
                return Json(new { success = false });
            }

            var now = TimeOnly.FromDateTime(DateTime.Now);
            var remainingMinutes = (int)(trip.arrival_time.ToTimeSpan() - now.ToTimeSpan()).TotalMinutes;

            if (remainingMinutes < 0)
                remainingMinutes = 0;

            return Json(new
            {
                success = true,
                driverName = trip.Driver != null ? trip.Driver.driver_name : "No driver assigned",
                busPlate = trip.Driver != null ? trip.Driver.bus_plate : "N/A",
                estimatedMinutes = remainingMinutes
            });
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        // for tracking the driver
        [HttpGet]
        public async Task<IActionResult> GetLiveLocation(int tripId)
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null)
                return Json(new { success = false, message = "Student not logged in" });

            var booking = await _context.Booking
                .Include(b => b.Student)
                .Include(b => b.Trip)
                .ThenInclude(t => t.Driver)
                .FirstOrDefaultAsync(b => b.trip_id == tripId && b.student_id == studentId.Value);

            if (booking == null)
                return Json(new { success = false, message = "Booking not found" });

            var studentStopEta = await _tripTrackingService.BuildStudentStopEtaAsync(tripId, booking.Student.building_id);
            var liveBusLocation = await _tripTrackingService.GetLiveBusLocationAsync(tripId);
            var tripStarted = IsTripStarted(booking.Trip.status);

            if (liveBusLocation == null)
            {
                return Json(new
                {
                    success = true,
                    tripStarted,
                    status = booking.Trip.status,
                    studentStopBuildingId = studentStopEta?.BuildingId ?? booking.Student.building_id,
                    studentStopLabel = studentStopEta?.StopLabel,
                    studentStopName = studentStopEta?.StopName,
                    studentEstimatedArrivalTime = studentStopEta?.EstimatedArrivalTime,
                    studentEtaMinutes = studentStopEta?.RemainingMinutes,
                    studentArrivingSoon = studentStopEta?.IsArrivingSoon ?? false,
                    driverName = booking.Trip.Driver?.driver_name ?? "No driver",
                    busPlate = booking.Trip.Driver?.bus_plate ?? "N/A",
                    message = tripStarted ? "Live bus location unavailable" : "Trip not started yet"
                });
            }

            return Json(new
            {
                success = true,
                tripStarted,
                status = booking.Trip.status,
                liveBusLocation,
                studentStopBuildingId = studentStopEta?.BuildingId ?? booking.Student.building_id,
                studentStopLabel = studentStopEta?.StopLabel,
                studentStopName = studentStopEta?.StopName,
                studentEstimatedArrivalTime = studentStopEta?.EstimatedArrivalTime,
                studentEtaMinutes = studentStopEta?.RemainingMinutes,
                studentArrivingSoon = studentStopEta?.IsArrivingSoon ?? false,
                driverName = booking.Trip.Driver?.driver_name ?? "No driver",
                busPlate = booking.Trip.Driver?.bus_plate ?? "N/A"
            });
        }

        private static void ApplyStudentStopEta(
            TripTrackingSnapshot snapshot,
            int studentBuildingId,
            StudentStopEta? studentStopEta)
        {
            snapshot.StudentStopBuildingId = studentStopEta?.BuildingId ?? studentBuildingId;
            snapshot.StudentStopLabel = studentStopEta?.StopLabel;
            snapshot.StudentStopName = studentStopEta?.StopName;
            snapshot.StudentEstimatedArrivalTime = studentStopEta?.EstimatedArrivalTime;
            snapshot.StudentEtaMinutes = studentStopEta?.RemainingMinutes;
            snapshot.StudentArrivingSoon = studentStopEta?.IsArrivingSoon ?? false;
        }

        private static bool IsTripStarted(string? status)
        {
            return string.Equals(status, "Started", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase);
        }

    }
}
