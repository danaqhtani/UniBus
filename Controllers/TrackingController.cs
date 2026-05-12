using Microsoft.AspNetCore.Mvc;

namespace UniBusApp.Controllers
{
    public class TrackingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}