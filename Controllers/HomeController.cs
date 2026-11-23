using System.Diagnostics;
using ImproveYourCv.Models;
using Microsoft.AspNetCore.Mvc;

namespace ImproveYourCv.Controllers;

public class HomeController : Controller
{
    // Ziyaretçiye site tanıtımı; giriş yapmış kullanıcı doğrudan profiline gider
    public IActionResult Index() =>
        User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Profile") : View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
