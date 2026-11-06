using System.ComponentModel.DataAnnotations;

namespace ProceraAssessment.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Username is required.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    // Used to redirect the user back to the page they originally requested
    public string? ReturnUrl { get; set; }
}