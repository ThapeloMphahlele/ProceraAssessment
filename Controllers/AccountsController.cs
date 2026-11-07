using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProceraAssessment.Data;
using ProceraAssessment.ViewModels;

namespace ProceraAssessment.Controllers;

[Authorize]
public class AccountsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(ApplicationDbContext context, ILogger<AccountsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm)
    {
        IQueryable<Models.Account> query = _context.Accounts;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();

            // Search by Exact Account Number OR Partial Person Surname
            query = query.Where(a =>
                a.AccountNumber == term || (a.Person.Surname != null && a.Person.Surname.Contains(term))
            );
        }

        // Project directly into the ViewModel to minimize memory allocation. EF Core translates this into a highly efficient SELECT statement with JOINs.
        var accountsList = await query.Select(a => new AccountListItemViewModel{
            Code = a.Code,
            AccountNumber = a.AccountNumber,
            PersonName = a.Person.Name,
            PersonSurname = a.Person.Surname,
            OutstandingBalance = a.OutstandingBalance,
            StatusName = a.AccountStatus.StatusName
        }).ToListAsync();

        var viewModel = new AccountSearchViewModel
        {
            SearchTerm = searchTerm,
            Accounts = accountsList
        };

        return View(viewModel);
    }

    // CREATE ACTIONS

    [HttpGet]
    public async Task<IActionResult> Create(int personCode)
    {
        var person = await _context.Persons.FindAsync(personCode);
        if (person == null)
        {
            return NotFound();
        }

        var model = new AccountCreateViewModel
        {
            PersonCode = person.Code,
            PersonFullName = $"{person.Name} {person.Surname}".Trim()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccountCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Verify the person still exists in the database
        var person = await _context.Persons.FindAsync(model.PersonCode);
        if (person == null)
        {
            ModelState.AddModelError(string.Empty, "The associated person could not be found. They may have been deleted.");
            return View(model);
        }

        // Refresh the PersonFullName from the database to avoid trusting the hidden input
        model.PersonFullName = $"{person.Name} {person.Surname}".Trim();

        // Trim the account number before checking and saving
        model.AccountNumber = model.AccountNumber.Trim();

        // Proactive uniqueness check
        var accountExists = await _context.Accounts.AnyAsync(a => a.AccountNumber == model.AccountNumber);
        if (accountExists)
        {
            ModelState.AddModelError("AccountNumber", "An account with this number already exists.");
            return View(model);
        }

        // Dynamically retrieve the "Open" status reference
        var openStatus = await _context.AccountStatuses.FirstOrDefaultAsync(s => s.StatusName == "Open");
        if (openStatus == null)
        {
            _logger.LogError("The 'Open' account status is missing from the AccountStatuses reference table.");
            ModelState.AddModelError(string.Empty, "Unable to create the account due to a missing system configuration (Account Status). Please contact support.");
            return View(model);
        }

        // Construct the entity safely, enforcing business rules (0 balance, open status)
        var account = new Models.Account
        {
            PersonCode = person.Code, // Use the retrieved person's code
            AccountNumber = model.AccountNumber,
            OutstandingBalance = 0m,
            StatusCode = openStatus.Code
        };

        try
        {
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            // Redirect to the new Account Details page
            TempData["SuccessMessage"] = "Account created successfully.";
            return RedirectToAction(nameof(Edit), new { id = account.Code });
        }
        catch (DbUpdateException ex)
        {
            // Specifically handle SQL Server unique constraint violations
            if (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                ModelState.AddModelError("AccountNumber", "Unable to save changes. The Account Number is already in use.");
            }
            else
            {
                _logger.LogError(ex, "An error occurred while creating a new account for person {PersonCode}.", person.Code);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while saving the account. Please try again.");
            }

            return View(model);
        }
    }

    // EDIT / DETAILS ACTIONS

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var account = await _context.Accounts
            .Include(a => a.Person)
            .Include(a => a.AccountStatus)
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.Code == id);

        if (account == null)
        {
            return NotFound();
        }

        var model = new AccountEditViewModel
        {
            Code = account.Code,
            AccountNumber = account.AccountNumber,
            PersonFullName = $"{account.Person.Name} {account.Person.Surname}".Trim(),
            OutstandingBalance = account.OutstandingBalance,
            StatusName = account.AccountStatus.StatusName,
            Transactions = account.Transactions.OrderByDescending(t => t.TransactionDate).Select(t => new TransactionListItemViewModel{
                Code = t.Code,
                TransactionDate = t.TransactionDate,
                CaptureDate = t.CaptureDate,
                Amount = t.Amount,
                Description = t.Description
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AccountEditViewModel model)
    {
        // Anti-tamper check: Ensure the route ID matches the hidden Code field.
        if (id != model.Code)
        {
            return BadRequest("Invalid request data.");
        }

        if (!ModelState.IsValid)
        {
            await ReloadAccountDetailsForViewModel(id, model);
            return View(model);
        }

        // Trim the account number before checking and saving
        model.AccountNumber = model.AccountNumber.Trim();

        // Proactive uniqueness check, explicitly ignoring the current account's Code
        bool accountExists = await _context.Accounts.AnyAsync(a => a.AccountNumber == model.AccountNumber && a.Code != id);
        if (accountExists)
        {
            ModelState.AddModelError("AccountNumber", "Another account with this number already exists.");
            await ReloadAccountDetailsForViewModel(id, model);
            return View(model);
        }

        // Use the authoritative route ID to fetch the tracked entity
        var accountToUpdate = await _context.Accounts.FindAsync(id);
        if (accountToUpdate == null)
        {
            return NotFound();
        }

        // Map ONLY the AccountNumber. OutstandingBalance, StatusCode, and PersonCode are deliberately ignored.
        accountToUpdate.AccountNumber = model.AccountNumber;

        try
        {
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Account details updated successfully.";
            return RedirectToAction(nameof(Edit), new { id = id });
        }
        catch (DbUpdateException ex)
        {
            // Specifically handle SQL Server unique constraint violations
            if (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                ModelState.AddModelError("AccountNumber", "Unable to save changes. The Account Number may already be in use by another record.");
            }
            else
            {
                _logger.LogError(ex, "An error occurred while updating account with ID {AccountCode}.", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while saving your changes. Please try again.");
            }

            await ReloadAccountDetailsForViewModel(id, model);
            return View(model);
        }
    }

    // Helper method to reload read-only UI state if the POST fails validation
    private async Task ReloadAccountDetailsForViewModel(int id, AccountEditViewModel model)
    {
        var account = await _context.Accounts
            .Include(a => a.Person)
            .Include(a => a.AccountStatus)
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.Code == id);

        if (account != null)
        {
            model.PersonFullName = $"{account.Person.Name} {account.Person.Surname}".Trim();
            model.OutstandingBalance = account.OutstandingBalance;
            model.StatusName = account.AccountStatus.StatusName;
            model.Transactions = account.Transactions
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => new TransactionListItemViewModel
                {
                    Code = t.Code,
                    TransactionDate = t.TransactionDate,
                    CaptureDate = t.CaptureDate,
                    Amount = t.Amount,
                    Description = t.Description
                }).ToList();
        }
    }

    // STATUS MANAGEMENT ACTIONS

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseAccount(int id)
    {
        // Dynamically retrieve reference statuses
        var openStatus = await _context.AccountStatuses.FirstOrDefaultAsync(s => s.StatusName == "Open");
        var closedStatus = await _context.AccountStatuses.FirstOrDefaultAsync(s => s.StatusName == "Closed");

        if (openStatus == null || closedStatus == null)
        {
            _logger.LogError("Required account statuses (Open/Closed) are missing from the AccountStatuses table.");
            TempData["ErrorMessage"] = "System configuration error. Please contact support.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        // Atomic conditional update to prevent race conditions
        var rowsAffected = await _context.Accounts.Where(a => a.Code == id && a.StatusCode == openStatus.Code && a.OutstandingBalance == 0m).ExecuteUpdateAsync(s => s.SetProperty(a => a.StatusCode, closedStatus.Code));

        // Evaluate the outcome
        if (rowsAffected == 1)
        {
            TempData["SuccessMessage"] = "Account successfully closed.";
        }
        else
        {
            // The atomic update failed. Fetch the account to determine exactly why and inform the user.
            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
            {
                return NotFound();
            }

            if (account.StatusCode == closedStatus.Code)
            {
                TempData["ErrorMessage"] = "This account is already closed.";
            }
            else if (account.OutstandingBalance != 0m)
            {
                TempData["ErrorMessage"] = "Account closure rejected: The outstanding balance must be exactly R0.00.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to close the account due to an unexpected state conflict.";
            }
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReopenAccount(int id)
    {
        // Dynamically retrieve reference statuses
        var openStatus = await _context.AccountStatuses.FirstOrDefaultAsync(s => s.StatusName == "Open");
        var closedStatus = await _context.AccountStatuses.FirstOrDefaultAsync(s => s.StatusName == "Closed");

        if (openStatus == null || closedStatus == null)
        {
            _logger.LogError("Required account statuses (Open/Closed) are missing from the AccountStatuses table.");
            TempData["ErrorMessage"] = "System configuration error. Please contact support.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        // Atomic conditional update
        var rowsAffected = await _context.Accounts.Where(a => a.Code == id && a.StatusCode == closedStatus.Code).ExecuteUpdateAsync(s => s.SetProperty(a => a.StatusCode, openStatus.Code));

        // Evaluate the outcome
        if (rowsAffected == 1)
        {
            TempData["SuccessMessage"] = "Account successfully reopened.";
        }
        else
        {
            // The atomic update failed. Fetch to determine why.
            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
            {
                return NotFound();
            }

            if (account.StatusCode == openStatus.Code)
            {
                TempData["ErrorMessage"] = "This account is already open.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to reopen the account due to an unexpected state conflict.";
            }
        }

        return RedirectToAction(nameof(Edit), new { id });
    }
}