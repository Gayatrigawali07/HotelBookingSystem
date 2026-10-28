using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoyalStay.Data;

namespace RoyalStay.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly BookingRepository _bookings;

    public AdminController(BookingRepository bookings) => _bookings = bookings;

    [HttpGet("Admin")]
    [HttpGet("Admin/Dashboard")]
    public IActionResult Index() => View(_bookings.GetStats());
}
