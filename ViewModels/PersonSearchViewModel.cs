namespace ProceraAssessment.ViewModels;

public class PersonSearchViewModel
{
    public string? SearchTerm { get; set; }

    // Holds the mapped results to pass to the view
    public List<PersonListItemViewModel> Persons { get; set; } = new List<PersonListItemViewModel>();
}