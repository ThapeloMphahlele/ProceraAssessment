using System.ComponentModel.DataAnnotations;

namespace ProceraAssessment.ViewModels;

public class AccountEditViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Invalid account.")]
    public int Code { get; set; }

    [Required(ErrorMessage = "Account Number is required.")]
    [StringLength(50, ErrorMessage = "Account Number cannot exceed 50 characters.")]
    [Display(Name = "Account Number")]
    public string AccountNumber { get; set; } = string.Empty;

    // Read-only display properties
    public string? PersonFullName { get; set; }

    [Display(Name = "Outstanding Balance")]
    public decimal OutstandingBalance { get; set; }

    [Display(Name = "Status")]
    public string? StatusName { get; set; }

    public List<TransactionListItemViewModel> Transactions { get; set; } = new List<TransactionListItemViewModel>();
}