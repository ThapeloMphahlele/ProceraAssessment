using System.ComponentModel.DataAnnotations;

namespace ProceraAssessment.ViewModels;

public class TransactionListItemViewModel
{
    public int Code { get; set; }

    [Display(Name = "Transaction Date")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
    public DateTime TransactionDate { get; set; }

    [Display(Name = "Capture Date")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
    public DateTime CaptureDate { get; set; }

    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;
}