using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GpaCalculatorApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        // View Visibility States
        private bool _isGpaPageVisible = true;
        public bool IsGpaPageVisible { get => _isGpaPageVisible; set => SetProperty(ref _isGpaPageVisible, value); }

        private bool _isCgpaPageVisible;
        public bool IsCgpaPageVisible { get => _isCgpaPageVisible; set => SetProperty(ref _isCgpaPageVisible, value); }

        private bool _isLoginPageVisible;
        public bool IsLoginPageVisible { get => _isLoginPageVisible; set => SetProperty(ref _isLoginPageVisible, value); }

        public ICommand ShowGpaCommand { get; }
        public ICommand ShowCgpaCommand { get; }
        public ICommand ShowLoginCommand { get; }

        // GPA Calculator
        public ObservableCollection<SubjectItemViewModel> Subjects { get; } = new ObservableCollection<SubjectItemViewModel>();
        public ICommand AddSubjectCommand { get; }
        public ICommand CalculateGpaCommand { get; }
        
        private string _gpaResult = "0.00";
        public string GpaResult { get => _gpaResult; set => SetProperty(ref _gpaResult, value); }
        
        private string _gpaClass = "";
        public string GpaClass { get => _gpaClass; set => SetProperty(ref _gpaClass, value); }
        
        private bool _isGpaResultVisible;
        public bool IsGpaResultVisible { get => _isGpaResultVisible; set => SetProperty(ref _isGpaResultVisible, value); }

        // CGPA Calculator
        public ObservableCollection<SemesterItemViewModel> Semesters { get; } = new ObservableCollection<SemesterItemViewModel>();
        public ICommand AddSemesterCommand { get; }
        public ICommand CalculateCgpaCommand { get; }
        public ICommand SaveToCloudCommand { get; }

        private string _cgpaResult = "0.00";
        public string CgpaResult { get => _cgpaResult; set => SetProperty(ref _cgpaResult, value); }
        
        private string _cgpaClass = "";
        public string CgpaClass { get => _cgpaClass; set => SetProperty(ref _cgpaClass, value); }
        
        private bool _isCgpaResultVisible;
        public bool IsCgpaResultVisible { get => _isCgpaResultVisible; set => SetProperty(ref _isCgpaResultVisible, value); }

        // Login / Register
        private string _loginName = "";
        public string LoginName { get => _loginName; set => SetProperty(ref _loginName, value); }

        private string _loginEmail = "";
        public string LoginEmail { get => _loginEmail; set => SetProperty(ref _loginEmail, value); }

        private string _loginPassword = "";
        public string LoginPassword { get => _loginPassword; set => SetProperty(ref _loginPassword, value); }

        private string _loginMessage = "";
        public string LoginMessage { get => _loginMessage; set => SetProperty(ref _loginMessage, value); }

        private string _userStatusText = "⚫ Offline / Local Mode";
        public string UserStatusText { get => _userStatusText; set => SetProperty(ref _userStatusText, value); }

        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }

        public bool IsLoggedIn => ApiClient.IsLoggedIn;

        public MainViewModel()
        {
            ShowGpaCommand = new RelayCommand(_ => { HideAllPages(); IsGpaPageVisible = true; });
            ShowCgpaCommand = new RelayCommand(_ => { HideAllPages(); IsCgpaPageVisible = true; });
            ShowLoginCommand = new RelayCommand(_ => { HideAllPages(); IsLoginPageVisible = true; });

            AddSubjectCommand = new RelayCommand(_ => AddSubject());
            CalculateGpaCommand = new RelayCommand(async _ => await CalculateGpaAsync());

            AddSemesterCommand = new RelayCommand(_ => AddSemester());
            CalculateCgpaCommand = new RelayCommand(_ => CalculateCgpa());
            SaveToCloudCommand = new RelayCommand(async _ => await SaveToCloudAsync(), _ => IsLoggedIn);

            LoginCommand = new RelayCommand(async _ => await DoLoginAsync());
            RegisterCommand = new RelayCommand(async _ => await DoRegisterAsync());

            AddSubject();
            AddSubject();
            AddSemester();
        }

        private void HideAllPages()
        {
            IsGpaPageVisible = false;
            IsCgpaPageVisible = false;
            IsLoginPageVisible = false;
        }

        private void AddSubject()
        {
            var subj = new SubjectItemViewModel { SubjectName = $"Subject {Subjects.Count + 1}" };
            subj.RemoveCommand = new RelayCommand(_ => Subjects.Remove(subj));
            Subjects.Add(subj);
        }

        private void AddSemester()
        {
            var sem = new SemesterItemViewModel { SemesterName = $"Semester {Semesters.Count + 1}" };
            sem.RemoveCommand = new RelayCommand(_ => Semesters.Remove(sem));
            Semesters.Add(sem);
        }

        private async Task CalculateGpaAsync()
        {
            if (Subjects.Count == 0) return;

            if (IsLoggedIn)
            {
                var dtos = Subjects.Select(s => new SubjectDto { SubjectName = s.SubjectName, Credits = s.Credits, Grade = s.Grade, Semester = "Current" }).ToList();
                var result = await ApiClient.CalculateGpaOnlineAsync(dtos);
                if (result.HasValue)
                {
                    GpaResult = result.Value.ToString("F2");
                    GpaClass = GetClassification(result.Value);
                    IsGpaResultVisible = true;
                    return;
                }
            }

            // Local fallback
            double totalQP = 0, totalCredits = 0;
            var gradePoints = new System.Collections.Generic.Dictionary<string, double>
            {
                {"A+", 4.0}, {"A", 3.7}, {"A-", 3.3},
                {"B+", 3.0}, {"B", 2.7}, {"B-", 2.3},
                {"C+", 2.0}, {"C", 1.7}, {"C-", 1.3},
                {"D", 1.0}, {"F", 0.0}
            };

            foreach (var sub in Subjects)
            {
                if (gradePoints.TryGetValue(sub.Grade, out double pts))
                {
                    totalQP += sub.Credits * pts;
                    totalCredits += sub.Credits;
                }
            }

            if (totalCredits > 0)
            {
                double gpa = totalQP / totalCredits;
                GpaResult = gpa.ToString("F2");
                GpaClass = GetClassification(gpa);
                IsGpaResultVisible = true;
            }
        }

        private void CalculateCgpa()
        {
            if (Semesters.Count == 0) return;

            double totalQP = 0, totalCredits = 0;
            foreach (var sem in Semesters)
            {
                totalQP += sem.Gpa * sem.Credits;
                totalCredits += sem.Credits;
            }

            if (totalCredits > 0)
            {
                double cgpa = totalQP / totalCredits;
                CgpaResult = cgpa.ToString("F2");
                CgpaClass = GetClassification(cgpa);
                IsCgpaResultVisible = true;
            }
        }

        private async Task SaveToCloudAsync()
        {
            if (!IsLoggedIn) return;

            await ApiClient.DeleteAllRecordsAsync();
            foreach (var sem in Semesters)
            {
                await ApiClient.SaveGpaRecordAsync(new GpaRecordDto { Semester = sem.SemesterName, GPA = sem.Gpa, CGPA = sem.Gpa });
            }
        }

        private async Task LoadSemestersFromCloudAsync()
        {
            var records = await ApiClient.GetGpaRecordsAsync();
            if (records != null && records.Any())
            {
                Semesters.Clear();
                foreach (var rec in records)
                {
                    var sem = new SemesterItemViewModel { SemesterName = rec.Semester, Gpa = rec.GPA, Credits = 15 }; // Assuming 15 default credits
                    sem.RemoveCommand = new RelayCommand(_ => Semesters.Remove(sem));
                    Semesters.Add(sem);
                }
            }
        }

        private async Task DoLoginAsync()
        {
            LoginMessage = "Logging in...";
            var res = await ApiClient.LoginAsync(LoginEmail, LoginPassword);
            if (res.Success)
            {
                ApiClient.SetToken(res.Token!, res.Name!, res.Email!);
                LoginMessage = $"✅ Welcome back, {res.Name}!";
                UserStatusText = "🟢 Logged In";
                OnPropertyChanged(nameof(IsLoggedIn));
                
                await LoadSemestersFromCloudAsync();
                ShowCgpaCommand.Execute(null);
            }
            else
            {
                LoginMessage = "❌ " + res.Message;
            }
        }

        private async Task DoRegisterAsync()
        {
            LoginMessage = "Registering...";
            var res = await ApiClient.RegisterAsync(LoginName, LoginEmail, LoginPassword);
            LoginMessage = res.Success ? "✅ " + res.Message : "❌ " + res.Message;
        }

        private static string GetClassification(double gpa) => gpa switch
        {
            >= 3.70 => "First Class",
            >= 3.30 => "Second Class Upper",
            >= 3.00 => "Second Class Lower",
            >= 2.00 => "Pass",
            _ => "Below Pass"
        };
    }
}
