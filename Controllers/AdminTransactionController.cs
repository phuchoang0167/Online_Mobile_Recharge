using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Mobile_Recharge.Models.Entities;
using Online_Mobile_Recharge.Models.ViewModels;
using Online_Mobile_Recharge.Services;

[Route("Admin/Transaction")]
[AdminAuthorize]
public class AdminTransactionController : Controller
{
    private readonly TransactionService _service;
    private readonly MobileRechargeDbContext _context;

    public AdminTransactionController(TransactionService service, MobileRechargeDbContext context)
    {
        _service = service;
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? userId, string? keyword, string? status, string? range)
    {
        var users = await _context.Users
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Role == "User")
            .OrderBy(x => x.Id)
            .ToListAsync();

        User? selectedUser = null;
        if (userId is int resolvedUserId)
        {
            selectedUser = users.FirstOrDefault(x => x.Id == resolvedUserId);
        }

        var postpaidTransactions = new List<Transaction>();
        var prepaidTransactions = new List<Transaction>();

        if (selectedUser != null)
        {
            postpaidTransactions = await _service.GetTransactionsAsync(selectedUser.Id, keyword, status, "Postpaid", range);
            prepaidTransactions = await _service.GetTransactionsAsync(selectedUser.Id, keyword, status, "Prepaid", range);
        }

        return View(new AdminTransactionHistoryViewModel
        {
            Keyword = keyword?.Trim() ?? string.Empty,
            Status = string.IsNullOrWhiteSpace(status) ? "all" : status,
            Range = string.IsNullOrWhiteSpace(range) ? "all" : range,
            SelectedUserId = selectedUser?.Id,
            SelectedUser = selectedUser,
            Users = users,
            PostpaidTransactions = postpaidTransactions,
            PrepaidTransactions = prepaidTransactions
        });
    }
}
