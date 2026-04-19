using Microsoft.AspNetCore.Mvc;
using SCM_System.Services;

namespace SCM_System.Controllers
{
    public class HomeController : Controller
    {
        public HomeController()
        {
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}