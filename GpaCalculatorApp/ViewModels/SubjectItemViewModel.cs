using System.Collections.ObjectModel;
using System.Linq;

namespace GpaCalculatorApp.ViewModels
{
    public class SubjectItemViewModel : ViewModelBase
    {
        private string _subjectName = "New Subject";
        public string SubjectName
        {
            get => _subjectName;
            set => SetProperty(ref _subjectName, value);
        }

        private int _credits = 3;
        public int Credits
        {
            get => _credits;
            set => SetProperty(ref _credits, value);
        }

        private string _grade = "A";
        public string Grade
        {
            get => _grade;
            set => SetProperty(ref _grade, value);
        }

        public ObservableCollection<int> AvailableCredits { get; } = new ObservableCollection<int> { 1, 2, 3, 4, 5 };
        public ObservableCollection<string> AvailableGrades { get; } = new ObservableCollection<string> { "A+", "A", "A-", "B+", "B", "B-", "C+", "C", "C-", "D", "F" };
        
        public RelayCommand RemoveCommand { get; set; } = null!; // Set by MainViewModel
    }
}
