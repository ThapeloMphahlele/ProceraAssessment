using Microsoft.EntityFrameworkCore;
using ProceraAssessment.Models;

namespace ProceraAssessment.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Person> Persons { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<AccountStatus> AccountStatuses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enforce the unique indexes that exist in the database
        modelBuilder.Entity<Person>()
            .HasIndex(p => p.IdNumber)
            .IsUnique();

        modelBuilder.Entity<Account>()
            .HasIndex(a => a.AccountNumber)
            .IsUnique();

        modelBuilder.Entity<AccountStatus>()
            .HasIndex(s => s.StatusName)
            .IsUnique();
    }
}