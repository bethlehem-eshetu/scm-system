using Microsoft.AspNetCore.Mvc;

namespace SCM_System.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
