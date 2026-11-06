using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProceraAssessment.Models;

[Table("Accounts")]
public class Account
{
    [Key]
    [Column("code")]
    public int Code { get; set; }

    [Column("person_code")]
    public int PersonCode { get; set; }

    [Required]
    [Column("account_number")]
    [StringLength(50)]
    public string AccountNumber { get; set; } = null!;

    [Column("outstanding_balance", TypeName = "money")]
    public decimal OutstandingBalance { get; set; }

    [Column("status_code")]
    public int StatusCode { get; set; }

    [ForeignKey(nameof(PersonCode))]
    public Person Person { get; set; } = null!;

    [ForeignKey(nameof(StatusCode))]
    public AccountStatus AccountStatus { get; set; } = null!;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}