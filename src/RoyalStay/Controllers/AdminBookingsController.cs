using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RoyalStay.Data;
using RoyalStay.Models;

namespace RoyalStay.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Bookings/{action=Index}/{id?}")]
public class AdminBookingsController : Controller
{
    private readonly BookingRepository _bookings;

    public AdminBookingsController(BookingRepository bookings) => _bookings = bookings;

    [HttpGet]
    public IActionResult Index(string? status)
    {
        if (!string.IsNullOrEmpty(status) && !BookingStatus.IsValid(status)) status = null;
        ViewBag.Status = status;
        return View(_bookings.GetAll(string.IsNullOrEmpty(status) ? null : status));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateStatus(int id, string status, string? filter)
    {
        BookingResult result;
        try
        {
            result = _bookings.UpdateStatus(id, status);
        }
        catch (SqlException ex) when (ex.Number == 1205)
        {
            result = new BookingResult(false, "The database was busy. Please try again.");
        }
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index), new { status = filter });
    }
}
