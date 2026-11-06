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

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Enforce varchar instead of nvarchar for all strings
        configurationBuilder.Properties<string>().AreUnicode(false);
    }

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

        // Enforce NoAction delete behavior to match SQL Server
        modelBuilder.Entity<Account>()
            .HasOne(a => a.Person)
            .WithMany(p => p.Accounts)
            .HasForeignKey(a => a.PersonCode)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Account>()
            .HasOne(a => a.AccountStatus)
            .WithMany(s => s.Accounts)
            .HasForeignKey(a => a.StatusCode)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Account)
            .WithMany(a => a.Transactions)
            .HasForeignKey(t => t.AccountCode)
            .OnDelete(DeleteBehavior.NoAction);
    }
}