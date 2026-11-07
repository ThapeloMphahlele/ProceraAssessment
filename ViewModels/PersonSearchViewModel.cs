namespace ProceraAssessment.ViewModels;

public class PersonSearchViewModel
{
    public string? SearchTerm { get; set; }

    // Holds the mapped results to pass to the view
    public List<PersonListItemViewModel> Persons { get; set; } = new List<PersonListItemViewModel>();

    // Pagination properties
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalItems { get; set; } = 0;
    public int PageSize { get; } = 10; // Fixed constraint

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    // Helpers for "Showing X-Y of Z persons"
    public int FirstItemOnPage => TotalItems == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
    public int LastItemOnPage => System.Math.Min(CurrentPage * PageSize, TotalItems);
}