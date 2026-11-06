using System.ComponentModel.DataAnnotations;

namespace ProceraAssessment.ViewModels;

public class PersonCreateViewModel
{
    [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters.")]
    public string? Name { get; set; }

    [StringLength(50, ErrorMessage = "Surname cannot exceed 50 characters.")]
    public string? Surname { get; set; }

    [Required(ErrorMessage = "ID Number is required.")]
    [StringLength(50, ErrorMessage = "ID Number cannot exceed 50 characters.")]
    [Display(Name = "ID Number")]
    public string IdNumber { get; set; } = string.Empty;
}