namespace ProceraAssessment.ViewModels;

public class AccountSearchViewModel
{
    public string? SearchTerm { get; set; }

    public List<AccountListItemViewModel> Accounts { get; set; } = new List<AccountListItemViewModel>();
}