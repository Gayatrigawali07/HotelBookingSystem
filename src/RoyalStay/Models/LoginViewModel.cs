using System.ComponentModel.DataAnnotations;

namespace RoyalStay.Models;

public class LoginViewModel
{
    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password), StringLength(128)]
    public string Password { get; set; } = "";
}
