using System.Web.Mvc;

namespace UniBus.Controllers
{
    public class TripsController : Controller
    {
        /* ===========================================================
           GET: Trips Page
           Frontend page for current booking + available trips
        ============================================================ */
        public ActionResult Index()
        {
            return View();
        }

        /* ===========================================================
           GET: Track Trip Page
           Frontend page for trip tracking
        ============================================================ */
        public ActionResult Track()
        {
            return View();
        }
    }
}