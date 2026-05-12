using System.Web.Mvc;

namespace UniBus.Controllers
{
    public class DriverController : Controller
    {
        /* ===========================================================
           GET: Driver Login Page
        ============================================================ */
        public ActionResult Login()
        {
            return View();
        }

        /* ===========================================================
           POST: Driver Login
           MOCK ONLY FOR NOW

           REAL BACKEND LATER:
           - validate serial number from database
           - load driver data from Driver table
           - store DriverId / DriverName / SerialNumber in Session
        ============================================================ */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string serialNumber)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
            {
                ViewBag.ErrorMessage = "Serial number is required.";
                return View();
            }

            // MOCK LOGIN
            Session["DriverSerialNumber"] = serialNumber;
            Session["DriverName"] = "Captain Saad";
            Session["DriverPlateNumber"] = "RUA-4821";

            /*
            REAL DATABASE LATER:

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = @"
                    SELECT driver_id, driver_name, serial_number, plate_number
                    FROM dbo.Driver
                    WHERE serial_number = @SerialNumber";

                // if found:
                // Session["DriverId"] = ...
                // Session["DriverName"] = ...
                // Session["DriverSerialNumber"] = ...
                // Session["DriverPlateNumber"] = ...
            }
            */

            return RedirectToAction("Home");
        }

        /* ===========================================================
           GET: Driver Home
        ============================================================ */
        public ActionResult Home()
        {
            if (Session["DriverSerialNumber"] == null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        /* ===========================================================
           GET: Driver Track Page
        ============================================================ */
        public ActionResult Track(string tripId)
        {
            if (Session["DriverSerialNumber"] == null)
            {
                return RedirectToAction("Login");
            }

            ViewBag.TripId = string.IsNullOrWhiteSpace(tripId) ? "TR-1001" : tripId;
            return View();
        }

        /* ===========================================================
           GET: Driver Profile
        ============================================================ */
        public ActionResult Profile()
        {
            if (Session["DriverSerialNumber"] == null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        /* ===========================================================
           GET: Driver Logout
        ============================================================ */
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login");
        }
    }
}