using Microsoft.AspNetCore.Mvc;
using RoyalStay.Data;

namespace RoyalStay.Controllers;

public class RoomsController : Controller
{
    private readonly RoomRepository _rooms;

    public RoomsController(RoomRepository rooms) => _rooms = rooms;

    // Public list. Home search form sends checkIn / checkOut / guests as query string.
    public IActionResult Index(DateTime? checkIn, DateTime? checkOut, int? guests)
    {
        if (checkIn.HasValue && checkOut.HasValue && checkOut.Value.Date <= checkIn.Value.Date)
        {
            TempData["Error"] = "Check-out must be after check-in.";
            checkIn = checkOut = null;
        }

        if (checkIn.HasValue && checkIn.Value.Date < DateTime.Today)
        {
            TempData["Error"] = "Check-in date cannot be in the past.";
            checkIn = checkOut = null;
        }

        ViewBag.CheckIn = checkIn;
        ViewBag.CheckOut = checkOut;
        ViewBag.Guests = guests;
        return View(_rooms.GetAvailable(checkIn, checkOut, guests));
    }

    public IActionResult Details(int id, DateTime? checkIn, DateTime? checkOut)
    {
        var room = _rooms.GetById(id);
        if (room == null || !room.IsAvailable) return NotFound();

        ViewBag.CheckIn = checkIn;
        ViewBag.CheckOut = checkOut;
        return View(room);
    }
}
