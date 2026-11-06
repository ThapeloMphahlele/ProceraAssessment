using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProceraAssessment.Models;

[Table("Persons")]
public class Person
{
    [Key]
    [Column("code")]
    public int Code { get; set; }

    [Column("name")]
    [StringLength(50)]
    public string? Name { get; set; }

    [Column("surname")]
    [StringLength(50)]
    public string? Surname { get; set; }

    [Required]
    [Column("id_number")]
    [StringLength(50)]
    public string IdNumber { get; set; } = null!;

    public ICollection<Account> Accounts { get; set; } = new List<Account>();
}