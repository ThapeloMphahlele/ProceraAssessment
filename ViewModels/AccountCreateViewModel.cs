using System.ComponentModel.DataAnnotations;

namespace ProceraAssessment.ViewModels;

public class AccountCreateViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Invalid person.")]
    public int PersonCode { get; set; }

    // Read-only property to provide visual context on the creation form
    public string? PersonFullName { get; set; }

    [Required(ErrorMessage = "Account Number is required.")]
    [StringLength(50, ErrorMessage = "Account Number cannot exceed 50 characters.")]
    [Display(Name = "Account Number")]
    public string AccountNumber { get; set; } = string.Empty;
}