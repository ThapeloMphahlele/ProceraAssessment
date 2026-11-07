using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProceraAssessment.Data;
using ProceraAssessment.Models;
using ProceraAssessment.ViewModels;

namespace ProceraAssessment.Controllers;

[Authorize]
public class PersonsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PersonsController> _logger;

    public PersonsController(ApplicationDbContext context, ILogger<PersonsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, int page = 1)
    {
        // Start with a base query on the Persons table
        IQueryable<Models.Person> query = _context.Persons;

        // Apply filters only if a search term is provided
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();

            // Search by Exact ID Number OR Partial Surname OR Exact Account Number
            query = query.Where(p =>
                p.IdNumber == term ||
                (p.Surname != null && p.Surname.Contains(term)) ||
                p.Accounts.Any(a => a.AccountNumber == term)
            );
        }

        // Pagination setup and calculations
        int pageSize = 10;
        int totalItems = await query.CountAsync();
        int totalPages = totalItems > 0 ? (int)Math.Ceiling(totalItems / (double)pageSize) : 1;

        // Handle invalid page numbers gracefully
        if (page < 1)
        {
            page = 1;
        }
        else if (page > totalPages)
        {
            page = totalPages;
        }

        // Apply deterministic ordering to ensure consistent pagination
        query = query.OrderBy(p => p.Code);

        // Apply pagination limits
        query = query.Skip((page - 1) * pageSize).Take(pageSize);

        var personsList = await query.Select(p => new PersonListItemViewModel
        {
            Code = p.Code,
            Name = p.Name,
            Surname = p.Surname,
            IdNumber = p.IdNumber,
            AccountCount = p.Accounts.Count
        }).ToListAsync();

        var viewModel = new PersonSearchViewModel
        {
            SearchTerm = searchTerm,
            Persons = personsList,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems
        };

        return View(viewModel);
    }

    // CREATE ACTIONS

    [HttpGet]
    public IActionResult Create()
    {
        return View(new PersonCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PersonCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Trim the ID number before checking and saving
        model.IdNumber = model.IdNumber.Trim();

        // Proactive uniqueness check for better UX
        bool idExists = await _context.Persons.AnyAsync(p => p.IdNumber == model.IdNumber);
        if (idExists)
        {
            ModelState.AddModelError("IdNumber", "A person with this ID Number already exists.");
            return View(model);
        }

        var person = new Person
        {
            Name = model.Name,
            Surname = model.Surname,
            IdNumber = model.IdNumber
        };

        try
        {
            _context.Persons.Add(person);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            // Specifically handle SQL Server unique constraint violations
            if (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                ModelState.AddModelError("IdNumber", "Unable to save changes. The ID Number is already in use.");
            }
            else
            {
                // For all other DB errors, log securely and show a generic message
                _logger.LogError(ex, "An error occurred while creating a new person.");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while saving your changes. Please try again.");
            }

            return View(model);
        }
    }

    // EDIT ACTIONS

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        // Include accounts and their statuses for the details view
        var person = await _context.Persons.Include(p => p.Accounts).ThenInclude(a => a.AccountStatus).FirstOrDefaultAsync(p => p.Code == id);

        if (person == null)
        {
            return NotFound();
        }

        var model = new PersonEditViewModel
        {
            Code = person.Code,
            Name = person.Name,
            Surname = person.Surname,
            IdNumber = person.IdNumber,
            // Map the accounts to the view model
            Accounts = person.Accounts.Select(a => new AccountListItemViewModel
            {
                Code = a.Code,
                AccountNumber = a.AccountNumber,
                OutstandingBalance = a.OutstandingBalance,
                StatusName = a.AccountStatus.StatusName
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PersonEditViewModel model)
    {
        // Anti-tamper check: Ensure the route ID matches the hidden Code field. This prevents a malicious user from manipulating the hidden input to overwrite another record.
        if (id != model.Code)
        {
            return BadRequest("Invalid request data.");
        }

        if (!ModelState.IsValid)
        {
            await ReloadAccountsForViewModel(model);
            return View(model);
        }

        // Trim the ID number before checking and saving
        model.IdNumber = model.IdNumber.Trim();

        // Proactive uniqueness check, explicitly ignoring the current person's Code
        bool idExists = await _context.Persons.AnyAsync(p => p.IdNumber == model.IdNumber && p.Code != model.Code);
        if (idExists)
        {
            ModelState.AddModelError("IdNumber", "Another person with this ID Number already exists.");
            await ReloadAccountsForViewModel(model);
            return View(model);
        }

        var personToUpdate = await _context.Persons.FindAsync(model.Code);
        if (personToUpdate == null)
        {
            return NotFound();
        }

        // Map updated fields from ViewModel to the tracked entity
        personToUpdate.Name = model.Name;
        personToUpdate.Surname = model.Surname;
        personToUpdate.IdNumber = model.IdNumber;

        try
        {
            await _context.SaveChangesAsync();

            // Provide feedback and redirect back to this details page
            TempData["SuccessMessage"] = "Person details updated successfully.";
            return RedirectToAction(nameof(Edit), new { id = model.Code });
        }
        catch (DbUpdateException ex)
        {
            // Specifically handle SQL Server unique constraint violations
            if (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                ModelState.AddModelError("IdNumber", "Unable to save changes. The ID Number may already be in use by another record.");
            }
            else
            {
                // For all other DB errors, log securely and show a generic message
                _logger.LogError(ex, "An error occurred while updating person with ID {PersonCode}.", model.Code);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while saving your changes. Please try again.");
            }

            await ReloadAccountsForViewModel(model);
            return View(model);
        }
    }

    // Helper method to reload accounts if the POST fails, preventing the UI table from disappearing
    private async Task ReloadAccountsForViewModel(PersonEditViewModel model)
    {
        model.Accounts = await _context.Accounts.Where(a => a.PersonCode == model.Code).Include(a => a.AccountStatus).Select(a => new AccountListItemViewModel{
            Code = a.Code,
            AccountNumber = a.AccountNumber,
            OutstandingBalance = a.OutstandingBalance,
            StatusName = a.AccountStatus.StatusName
        }).ToListAsync();
    }

    // DELETE ACTIONS

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        // Retrieve the person along with their accounts and the account statuses
        var person = await _context.Persons.Include(p => p.Accounts).ThenInclude(a => a.AccountStatus).FirstOrDefaultAsync(p => p.Code == id);

        if (person == null)
        {
            return NotFound();
        }

        return View(person);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        // Retrieve the person, their accounts, statuses, AND their transactions for safe deletion
        var person = await _context.Persons.Include(p => p.Accounts).ThenInclude(a => a.AccountStatus).Include(p => p.Accounts).ThenInclude(a => a.Transactions).FirstOrDefaultAsync(p => p.Code == id);

        if (person == null)
        {
            return NotFound();
        }

        // Recheck the business rule on the server: Cannot delete if ANY account is "Open"
        bool hasOpenAccounts = person.Accounts.Any(a => a.AccountStatus.StatusName == "Open");
        if (hasOpenAccounts)
        {
            ModelState.AddModelError(string.Empty, "Deletion failed: This person has one or more Open accounts.");
            return View("Delete", person);
        }

        // Begin a database transaction
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Delete associated transactions first
            var transactionsToDelete = person.Accounts.SelectMany(a => a.Transactions).ToList();
            if (transactionsToDelete.Any())
            {
                _context.Transactions.RemoveRange(transactionsToDelete);
                await _context.SaveChangesAsync();
            }

            // Delete associated accounts second
            if (person.Accounts.Any())
            {
                _context.Accounts.RemoveRange(person.Accounts);
                await _context.SaveChangesAsync();
            }

            // Delete the person last
            _context.Persons.Remove(person);
            await _context.SaveChangesAsync();

            // Commit the transaction only after all three operations succeed
            await transaction.CommitAsync();

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            // Roll back all operations if any deletion fails
            await transaction.RollbackAsync();

            _logger.LogError(ex, "An error occurred while attempting to delete person with ID {PersonCode}.", person.Code);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred while deleting the person. Please try again.");

            return View("Delete", person);
        }
    }
}