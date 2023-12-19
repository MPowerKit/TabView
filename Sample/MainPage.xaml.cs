using System.Collections.ObjectModel;

namespace Sample;

public class PersonViewModel
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        Title = "Main Page";

        ObservableCollection<PersonViewModel> people = [];
        for (int i = 0; i < 5; i++)
        {
            people.Add(new PersonViewModel() { FirstName = $"FirstName {i}", LastName = $"LastName {i}" });
        }
        People = people;
    }

    public ObservableCollection<PersonViewModel> People { get; set; } = new ObservableCollection<PersonViewModel>();
}