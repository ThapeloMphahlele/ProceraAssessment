using System.ComponentModel.DataAnnotations;

namespace ProceraAssessment.ViewModels;

public class TransactionCreateViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Invalid account.")]
    public int AccountCode { get; set; }

    // Read-only display property for UI context
    public string? AccountDisplayInfo { get; set; }

    [Required(ErrorMessage = "Transaction Date is required.")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "Transaction Date")]
    public DateTime? TransactionDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Transaction Type is required.")]
    [Display(Name = "Transaction Type")]
    public TransactionType Type { get; set; }

    [Required(ErrorMessage = "Amount is required.")]
    [Range(0.01, 922337203685477.58, ErrorMessage = "Amount must be a positive value greater than zero.")]
    [Display(Name = "Amount")]
    public decimal AbsoluteAmount { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(100, ErrorMessage = "Description cannot exceed 100 characters.")]
    public string? Description { get; set; }
}