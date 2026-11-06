using System.ComponentModel.DataAnnotations;

namespace ProceraAssessment.ViewModels;

public class PersonListItemViewModel
{
    public int Code { get; set; }

    public string? Name { get; set; }

    public string? Surname { get; set; }

    [Display(Name = "ID Number")]
    public string IdNumber { get; set; } = string.Empty;

    [Display(Name = "Accounts")]
    public int AccountCount { get; set; }
}