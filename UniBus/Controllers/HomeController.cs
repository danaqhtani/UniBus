using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using UniBus.Models;

namespace UniBus.Controllers
{
    public class HomeController : Controller
    {
        // Read connection string from Web.config
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["UniBusDb"].ConnectionString;

        /* ===========================================================
           GET: Home Page
           Loads current student info from database based on session.
           Routes and booking are mock data for now until trip schema
           is fully confirmed.
        ============================================================ */
        public ActionResult Index()
        {
            // Protect page if user is not logged in
            if (Session["StudentId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int studentId = Convert.ToInt32(Session["StudentId"]);

            // Main page model
            HomeViewModel model = new HomeViewModel();

            try
            {
                // =====================================================
                // REAL DATABASE SECTION
                // Load student data from dbo.Student
                // =====================================================
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT
                            student_id,
                            name,
                            university_email,
                            phone_number,
                            building_id
                        FROM dbo.Student
                        WHERE student_id = @StudentId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@StudentId", studentId);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                model.StudentId = Convert.ToInt32(reader["student_id"]);
                                model.StudentName = reader["name"].ToString();
                                model.StudentEmail = reader["university_email"].ToString();
                                model.PhoneNumber = reader["phone_number"].ToString();
                                model.BuildingId = Convert.ToInt32(reader["building_id"]);

                                // Friendly label shown in UI
                                model.CampusLabel = "Campus " + model.BuildingId;
                            }
                            else
                            {
                                // If session exists but student is not found, force relogin
                                return RedirectToAction("Login", "Account");
                            }
                        }
                    }
                }

                // =====================================================
                // MOCK ROUTES SECTION
                // Replace these with real route/trip records later
                // when ShuttleTrip / TripDirection schema is finalized.
                // =====================================================
                model.AvailableRoutes.Add(new RouteCardViewModel
                {
                    RouteCode = "R-UNI-01",
                    RouteTitle = "From University to Station",
                    RouteSubtitle = "Morning campus departure",
                    RouteDirection = "University → Station",
                    AccentColorClass = "text-violet-600",
                    IconName = "school",
                    RidersCount = 42,
                    ViewTimesUrl = "#"
                });

                model.AvailableRoutes.Add(new RouteCardViewModel
                {
                    RouteCode = "R-STA-02",
                    RouteTitle = "From Station to University",
                    RouteSubtitle = "Return to campus",
                    RouteDirection = "Station → University",
                    AccentColorClass = "text-pink-600",
                    IconName = "train",
                    RidersCount = 18,
                    ViewTimesUrl = "#"
                });

                // =====================================================
                // MOCK BOOKING SECTION
                // Replace with real booking lookup from dbo.Booking
                // + dbo.ShuttleTrip once final trip columns are confirmed.
                // =====================================================
                model.CurrentBooking = new BookingCardViewModel
                {
                    HasBooking = true,
                    TripCode = "UB-204",
                    RouteName = "Route 14A Express",
                    StartPoint = "Central Station",
                    EndPoint = "Main Campus Gate",
                    DepartureTime = "09:15 AM",
                    EstimatedArrival = "09:40 AM",
                    DriverName = "Captain Marcus",
                    SeatNumber = "12F",
                    TrackUrl = "#",
                    CancelUrl = "#"
                };

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Unexpected error: " + ex.Message;
                return View(model);
            }
        }
    }
}