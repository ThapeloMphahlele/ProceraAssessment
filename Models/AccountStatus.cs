using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProceraAssessment.Models;

[Table("AccountStatuses")]
public class AccountStatus
{
    [Key]
    [Column("code")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)] // Disables identity generation
    public int Code { get; set; }

    [Required]
    [Column("status_name")]
    [StringLength(20)]
    public string StatusName { get; set; } = null!;

    public ICollection<Account> Accounts { get; set; } = new List<Account>();
}