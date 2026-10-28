using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using RoyalStay.Data;

namespace RoyalStay.Controllers;

public class HomeController : Controller
{
    private readonly RoomRepository _rooms;

    public HomeController(RoomRepository rooms) => _rooms = rooms;

    public IActionResult Index()
    {
        // Home page shows the first 3 bookable rooms from SQL Server.
        var rooms = _rooms.GetAvailable(null, null, null).Take(3).ToList();
        return View(rooms);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        ViewData["RequestId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        // If the problem is a database problem, say so in simple words (no stack trace, no connection string).
        var ex = HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
        ViewData["IsDatabaseError"] = ex is SqlException;
        return View();
    }
}
