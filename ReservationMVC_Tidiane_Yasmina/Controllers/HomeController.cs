using Microsoft.AspNetCore.Mvc;

namespace ReservationMVC.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult Error()
        {
            return View();
        }
    }
}
