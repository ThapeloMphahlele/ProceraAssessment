using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProceraAssessment.Data;
using ProceraAssessment.Models;
using ProceraAssessment.ViewModels;
using System;
using System.Threading.Tasks;

namespace ProceraAssessment.Controllers;

[Authorize]
public class TransactionsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(ApplicationDbContext context, ILogger<TransactionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int accountCode)
    {
        // Retrieve the account and its current status
        var account = await _context.Accounts.Include(a => a.AccountStatus).Include(a => a.Person).FirstOrDefaultAsync(a => a.Code == accountCode);

        if (account == null)
        {
            return NotFound();
        }

        // Verify account is Open
        if (account.AccountStatus.StatusName != "Open")
        {
            TempData["ErrorMessage"] = "Transactions cannot be created for a closed account.";
            return RedirectToAction("Edit", "Accounts", new { id = accountCode });
        }

        // Populate ViewModel
        var model = new TransactionCreateViewModel
        {
            AccountCode = account.Code,
            AccountDisplayInfo = $"Account: {account.AccountNumber} ({account.Person.Name} {account.Person.Surname})".Trim(),
            TransactionDate = DateTime.Today
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TransactionCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await ReloadAccountDisplayInfo(model);
            return View(model);
        }

        // Explicit Server-Side Validations
        if (!Enum.IsDefined(typeof(TransactionType), model.Type))
        {
            ModelState.AddModelError("Type", "Invalid transaction type selected.");
        }

        // Ensure missing date is rejected and future dates are blocked
        if (!model.TransactionDate.HasValue)
        {
            ModelState.AddModelError("TransactionDate", "Transaction date is required.");
        }
        else if (model.TransactionDate.Value > DateTime.Today)
        {
            ModelState.AddModelError("TransactionDate", "Transaction date cannot be in the future.");
        }

        if (model.AbsoluteAmount <= 0)
        {
            ModelState.AddModelError("AbsoluteAmount", "Amount must be greater than zero.");
        }

        // Ensure description is truly provided
        if (string.IsNullOrWhiteSpace(model.Description))
        {
            ModelState.AddModelError("Description", "Description is required.");
        }

        if (!ModelState.IsValid)
        {
            await ReloadAccountDisplayInfo(model);
            return View(model);
        }

        // Retrieve Open status & Verify Account
        var openStatus = await _context.AccountStatuses.FirstOrDefaultAsync(s => s.StatusName == "Open");
        if (openStatus == null)
        {
            _logger.LogError("The 'Open' account status is missing from reference table.");
            ModelState.AddModelError(string.Empty, "System configuration error. Please contact support.");
            await ReloadAccountDisplayInfo(model);
            return View(model);
        }

        var account = await _context.Accounts.Include(a => a.AccountStatus).FirstOrDefaultAsync(a => a.Code == model.AccountCode);

        if (account == null)
        {
            ModelState.AddModelError(string.Empty, "The associated account could not be found.");
            await ReloadAccountDisplayInfo(model);
            return View(model);
        }

        if (account.AccountStatus.StatusName != "Open")
        {
            ModelState.AddModelError(string.Empty, "Transactions cannot be created for a closed account.");
            await ReloadAccountDisplayInfo(model);
            return View(model);
        }

        // Convert Amount based on Type (Debit = Positive, Credit = Negative)
        decimal signedAmount = model.Type == TransactionType.Debit ? model.AbsoluteAmount : -model.AbsoluteAmount;

        // Concurrency & Data Integrity (EF Core Transaction)
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? dbTransaction = null;
        try
        {
            dbTransaction = await _context.Database.BeginTransactionAsync();

            // Save the transaction record
            var newTransaction = new Transaction
            {
                AccountCode = model.AccountCode,
                TransactionDate = model.TransactionDate!.Value,
                CaptureDate = DateTime.Now, // Set exclusively on the server
                Amount = signedAmount,
                Description = model.Description!.Trim()
            };

            _context.Transactions.Add(newTransaction);
            await _context.SaveChangesAsync();

            // Atomically update the account balance conditionally
            var rowsAffected = await _context.Accounts.Where(a => a.Code == model.AccountCode && a.StatusCode == openStatus.Code).ExecuteUpdateAsync(s => s.SetProperty(
                a => a.OutstandingBalance,
                a => a.OutstandingBalance + signedAmount
            ));

            // Evaluate Outcome
            if (rowsAffected == 0)
            {
                // The account was likely closed by another user mere milliseconds ago
                await dbTransaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Transaction failed: The account was closed or modified concurrently.");
                await ReloadAccountDisplayInfo(model);
                return View(model);
            }

            await dbTransaction.CommitAsync();

            TempData["SuccessMessage"] = "Transaction created successfully.";
            return RedirectToAction("Edit", "Accounts", new { id = model.AccountCode });
        }
        catch (Exception ex)
        {
            // Error handling does not obscure the original database exception (ex is logged securely)
            if (dbTransaction != null)
            {
                await dbTransaction.RollbackAsync();
            }
            _logger.LogError(ex, "An error occurred while creating a transaction for Account {AccountCode}.", model.AccountCode);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred while saving the transaction. Please try again.");
            await ReloadAccountDisplayInfo(model);
            return View(model);
        }
        finally
        {
            // Appropriate asynchronous disposal
            if (dbTransaction != null)
            {
                await dbTransaction.DisposeAsync();
            }
        }
    }

    // Helper method to securely reload display data on validation failure
    private async Task ReloadAccountDisplayInfo(TransactionCreateViewModel model)
    {
        var account = await _context.Accounts
            .Include(a => a.Person)
            .FirstOrDefaultAsync(a => a.Code == model.AccountCode);

        if (account != null)
        {
            model.AccountDisplayInfo = $"Account: {account.AccountNumber} ({account.Person.Name} {account.Person.Surname})".Trim();
        }
    }

    // EDIT ACTIONS

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        // Fetch the authoritative transaction and its account context
        var transaction = await _context.Transactions.Include(t => t.Account).ThenInclude(a => a.AccountStatus).Include(t => t.Account).ThenInclude(a => a.Person).FirstOrDefaultAsync(t => t.Code == id);

        if (transaction == null)
        {
            return NotFound();
        }

        // Reject editing if the account is closed
        if (transaction.Account.AccountStatus.StatusName != "Open")
        {
            TempData["ErrorMessage"] = "Cannot edit transactions for a closed account.";
            return RedirectToAction("Edit", "Accounts", new { id = transaction.AccountCode });
        }

        // Map to ViewModel, reverse-engineering the signed amount for the UI
        var model = new TransactionEditViewModel
        {
            Code = transaction.Code,
            AccountCode = transaction.AccountCode, // For UI routing only
            AccountDisplayInfo = $"Account: {transaction.Account.AccountNumber} ({transaction.Account.Person.Name} {transaction.Account.Person.Surname})".Trim(),
            TransactionDate = transaction.TransactionDate,
            Type = transaction.Amount >= 0 ? TransactionType.Debit : TransactionType.Credit,
            AbsoluteAmount = Math.Abs(transaction.Amount),
            Description = transaction.Description
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TransactionEditViewModel model)
    {
        // Security Check: Prevent route/ID tampering
        if (id != model.Code)
        {
            return BadRequest("Invalid request data.");
        }

        // Explicit Server-Side Validations
        if (!Enum.IsDefined(typeof(TransactionType), model.Type))
        {
            ModelState.AddModelError("Type", "Invalid transaction type selected.");
        }

        if (!model.TransactionDate.HasValue)
        {
            ModelState.AddModelError("TransactionDate", "Transaction date is required.");
        }
        else if (model.TransactionDate.Value > DateTime.Today)
        {
            ModelState.AddModelError("TransactionDate", "Transaction date cannot be in the future.");
        }

        if (model.AbsoluteAmount <= 0)
        {
            ModelState.AddModelError("AbsoluteAmount", "Amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(model.Description))
        {
            ModelState.AddModelError("Description", "Description is required.");
        }

        if (!ModelState.IsValid)
        {
            await ReloadAccountDisplayInfoForEdit(id, model);
            return View(model);
        }

        // Retrieve the tracked authoritative transaction and verify account status
        var transactionToUpdate = await _context.Transactions.Include(t => t.Account).ThenInclude(a => a.AccountStatus).FirstOrDefaultAsync(t => t.Code == id);

        if (transactionToUpdate == null)
        {
            return NotFound();
        }

        if (transactionToUpdate.Account.AccountStatus.StatusName != "Open")
        {
            ModelState.AddModelError(string.Empty, "Cannot edit transactions for a closed account.");
            await ReloadAccountDisplayInfoForEdit(id, model);
            return View(model);
        }

        var openStatus = await _context.AccountStatuses.FirstOrDefaultAsync(s => s.StatusName == "Open");
        if (openStatus == null)
        {
            _logger.LogError("The 'Open' account status is missing from reference table.");
            ModelState.AddModelError(string.Empty, "System configuration error. Please contact support.");
            await ReloadAccountDisplayInfoForEdit(id, model);
            return View(model);
        }

        // Calculate the Balance Adjustment
        decimal oldSignedAmount = transactionToUpdate.Amount;
        decimal newSignedAmount = model.Type == TransactionType.Debit ? model.AbsoluteAmount : -model.AbsoluteAmount;
        decimal balanceAdjustment = newSignedAmount - oldSignedAmount;

        // Concurrency & Data Integrity
        try
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            // Update the transaction entity (AccountCode is deliberately left untouched)
            transactionToUpdate.TransactionDate = model.TransactionDate!.Value;
            transactionToUpdate.CaptureDate = DateTime.Now; // Automatically refreshed on server
            transactionToUpdate.Amount = newSignedAmount;
            transactionToUpdate.Description = model.Description!.Trim();

            await _context.SaveChangesAsync();

            // Apply the balance adjustment conditionally
            var rowsAffected = await _context.Accounts.Where(a => a.Code == transactionToUpdate.AccountCode && a.StatusCode == openStatus.Code).ExecuteUpdateAsync(s => s.SetProperty(
                a => a.OutstandingBalance,
                a => a.OutstandingBalance + balanceAdjustment
            ));

            // Evaluate Outcome
            if (rowsAffected == 0)
            {
                await dbTransaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Transaction failed: The account was closed or modified concurrently.");
                await ReloadAccountDisplayInfoForEdit(id, model);
                return View(model);
            }

            await dbTransaction.CommitAsync();

            TempData["SuccessMessage"] = "Transaction updated successfully.";
            return RedirectToAction("Edit", "Accounts", new { id = transactionToUpdate.AccountCode });
        }
        catch (Exception ex)
        {
            // Due to await using var dbTransaction, uncommitted transactions are automatically rolled back when disposed, preserving the original exception here for logging.
            _logger.LogError(ex, "An error occurred while updating transaction {TransactionCode}.", id);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred while saving the transaction. Please try again.");
            await ReloadAccountDisplayInfoForEdit(id, model);
            return View(model);
        }
    }

    // Helper method to securely reload display data using the authoritative route ID
    private async Task ReloadAccountDisplayInfoForEdit(int id, TransactionEditViewModel model)
    {
        var transaction = await _context.Transactions.Include(t => t.Account).ThenInclude(a => a.Person).FirstOrDefaultAsync(t => t.Code == id);

        if (transaction != null)
        {
            model.AccountDisplayInfo = $"Account: {transaction.Account.AccountNumber} ({transaction.Account.Person.Name} {transaction.Account.Person.Surname})".Trim();
        }
    }
}