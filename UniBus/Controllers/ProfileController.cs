using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using UniBus.Models;

namespace UniBus.Controllers
{
    public class ProfileController : Controller
    {
        // Read connection string from Web.config
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["UniBusDb"].ConnectionString;

        /* ===========================================================
           GET: Profile Page
           Loads current student data from database using session
        ============================================================ */
        public ActionResult Index()
        {
            // Protect profile page if user is not logged in
            if (Session["StudentId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int studentId = Convert.ToInt32(Session["StudentId"]);

            ProfileViewModel model = new ProfileViewModel();

            try
            {
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
                                model.FullName = reader["name"].ToString();
                                model.UniversityEmail = reader["university_email"].ToString();
                                model.PhoneNumber = reader["phone_number"].ToString();
                                model.BuildingId = Convert.ToInt32(reader["building_id"]);

                                // Friendly UI labels
                                model.Username = model.UniversityEmail.Split('@')[0];
                                model.CampusLabel = "Campus " + model.BuildingId;
                            }
                            else
                            {
                                return RedirectToAction("Login", "Account");
                            }
                        }
                    }
                }

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