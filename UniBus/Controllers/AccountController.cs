using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using UniBus.Helpers;
using UniBus.Models;

namespace UniBus.Controllers
{
    public class AccountController : Controller
    {
        // Read connection string from Web.config
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["UniBusDb"].ConnectionString;

        // Real university domain based on your database data
        private const string UniversityDomain = "@sm.imamu.edu.sa";

        /* ===========================================================
           GET: Start Page
        ============================================================ */
        public ActionResult Start()
        {
            return View();
        }

        /* ===========================================================
           GET: Login Page
        ============================================================ */
        public ActionResult Login()
        {
            return View();
        }

        /* ===========================================================
           POST: Real Login Method
        ============================================================ */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = "Please complete all fields correctly.";
                return View(model);
            }

            string fullEmail = model.EmailPrefix.Trim() + UniversityDomain;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT student_id, name, university_email, password_hash, password_salt
                        FROM dbo.Student
                        WHERE university_email = @UniversityEmail";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UniversityEmail", fullEmail);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int studentId = Convert.ToInt32(reader["student_id"]);
                                string studentName = reader["name"].ToString();
                                byte[] savedHash = (byte[])reader["password_hash"];
                                byte[] savedSalt = (byte[])reader["password_salt"];

                                bool isValidPassword = PasswordHelper.VerifyPassword(
                                    model.Password,
                                    savedSalt,
                                    savedHash
                                );

                                if (isValidPassword)
                                {
                                    Session["StudentId"] = studentId;
                                    Session["StudentName"] = studentName;
                                    Session["StudentEmail"] = fullEmail;

                                    return RedirectToAction("Index", "Home");
                                }
                            }
                        }
                    }
                }

                ViewBag.ErrorMessage = "Login failed. Please check your email or password.";
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Unexpected error: " + ex.Message;
                return View(model);
            }
        }

        /* ===========================================================
           GET: Register Page
        ============================================================ */
        public ActionResult Register()
        {
            return View();
        }

        /* ===========================================================
           POST: Real Register Method
        ============================================================ */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = "Please complete all fields correctly.";
                return View(model);
            }

            string fullEmail = model.EmailPrefix.Trim() + UniversityDomain;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Check if email already exists
                    string checkEmailQuery = @"
                        SELECT COUNT(*)
                        FROM dbo.Student
                        WHERE university_email = @UniversityEmail";

                    using (SqlCommand checkCommand = new SqlCommand(checkEmailQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@UniversityEmail", fullEmail);

                        int existingCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                        if (existingCount > 0)
                        {
                            ViewBag.ErrorMessage = "This university email is already registered.";
                            return View(model);
                        }
                    }

                    byte[] salt = PasswordHelper.GenerateSalt();
                    byte[] hash = PasswordHelper.HashPassword(model.Password, salt);

                    string insertQuery = @"
                        INSERT INTO dbo.Student
                        (
                            name,
                            phone_number,
                            university_email,
                            email_verified,
                            building_id,
                            password_hash,
                            password_salt
                        )
                        VALUES
                        (
                            @Name,
                            @PhoneNumber,
                            @UniversityEmail,
                            @EmailVerified,
                            @BuildingId,
                            @PasswordHash,
                            @PasswordSalt
                        )";

                    using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@Name", model.Name.Trim());
                        insertCommand.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber.Trim());
                        insertCommand.Parameters.AddWithValue("@UniversityEmail", fullEmail);
                        insertCommand.Parameters.AddWithValue("@EmailVerified", false);
                        insertCommand.Parameters.AddWithValue("@BuildingId", model.BuildingId);
                        insertCommand.Parameters.AddWithValue("@PasswordHash", hash);
                        insertCommand.Parameters.AddWithValue("@PasswordSalt", salt);

                        insertCommand.ExecuteNonQuery();
                    }
                }

                TempData["SuccessMessage"] = "Account created successfully. You can now log in.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Unexpected error: " + ex.Message;
                return View(model);
            }
        }

        /* ===========================================================
           GET: Driver Login Page
        ============================================================ */
        public ActionResult DriverLogin()
        {
            return View();
        }

        /* ===========================================================
           GET: Logout
           Clears current session and returns user to Start page
        ============================================================ */
        public ActionResult Logout()
        {
            // Clear all session values
            Session.Clear();
            Session.Abandon();

            // Optional: prevent back button showing protected pages from cache
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));

            return RedirectToAction("Start", "Account");
        }
    }
}