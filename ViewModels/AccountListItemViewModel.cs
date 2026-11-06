using System.ComponentModel.DataAnnotations;

namespace ProceraAssessment.ViewModels;

public class AccountListItemViewModel
{
    public int Code { get; set; }

    [Display(Name = "Account Number")]
    public string AccountNumber { get; set; } = string.Empty;

    [Display(Name = "Balance")]
    public decimal OutstandingBalance { get; set; }

    public string StatusName { get; set; } = string.Empty;
}