using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProceraAssessment.Models;

[Table("Transactions")]
public class Transaction
{
    [Key]
    [Column("code")]
    public int Code { get; set; }

    [Column("account_code")]
    public int AccountCode { get; set; }

    [Column("transaction_date")]
    public DateTime TransactionDate { get; set; }

    [Column("capture_date")]
    public DateTime CaptureDate { get; set; }

    [Column("amount", TypeName = "money")]
    public decimal Amount { get; set; }

    [Required]
    [Column("description")]
    [StringLength(100)]
    public string Description { get; set; } = null!;

    [ForeignKey(nameof(AccountCode))]
    public Account Account { get; set; } = null!;
}