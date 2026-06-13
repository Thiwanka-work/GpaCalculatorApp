namespace GpaCalculatorApp.ViewModels
{
    public class SemesterItemViewModel : ViewModelBase
    {
        private string _semesterName = "New Semester";
        public string SemesterName
        {
            get => _semesterName;
            set => SetProperty(ref _semesterName, value);
        }

        private double _gpa = 0.0;
        public double Gpa
        {
            get => _gpa;
            set => SetProperty(ref _gpa, value);
        }

        private double _credits = 0;
        public double Credits
        {
            get => _credits;
            set => SetProperty(ref _credits, value);
        }

        public RelayCommand RemoveCommand { get; set; } = null!; // Set by MainViewModel
    }
}
