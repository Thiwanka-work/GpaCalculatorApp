using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GpaCalculatorApp
{
    public partial class MainWindow : Window
    {
        // ═══════════ GRADE LOOKUP ═══════════
        private static readonly Dictionary<string, double> GradePoints = new()
        {
            {"A+", 4.0}, {"A", 3.7}, {"A-", 3.3},
            {"B+", 3.0}, {"B", 2.7}, {"B-", 2.3},
            {"C+", 2.0}, {"C", 1.7}, {"C-", 1.3},
            {"D", 1.0}, {"F", 0.0}
        };

        private static readonly string[] Grades = ["A+", "A", "A-", "B+", "B", "B-", "C+", "C", "C-", "D", "F"];
        private static readonly double[] TargetCgpaValues = [3.70, 3.30, 3.00];

        // Tier data for upgrade paths
        private static readonly (string Name, double MinGpa, string Color)[] Tiers =
        [
            ("First Class",        3.70, "#3fb950"),
            ("Second Class Upper", 3.30, "#2563eb"),
            ("Second Class Lower", 3.00, "#d29922"),
            ("Pass",               2.00, "#8b949e"),
        ];

        private int _subjectCount = 0;
        private int _semesterCount = 0;

        public MainWindow()
        {
            InitializeComponent();
            ShowPage(GpaPage, NavGpaBtn);
            AddSubjectRow();
            AddSubjectRow();
            AddSemesterRow();

            // Greet in the chat on load
            Loaded += (_, _) => AddChatMessage("agent",
                "👋 Hi! I'm your AI Academic Advisor.\n\n" +
                "I can help you:\n" +
                "• 📖 Learn how to use this app\n" +
                "• 📅 Generate a personalised study timetable\n" +
                "• 📈 Plan your CGPA upgrade roadmap\n" +
                "• 📚 Get proven study strategies\n\n" +
                "Click a quick-action chip below or type your question!");
        }

        // ═══════════ NAVIGATION ═══════════

        private void ShowPage(Grid page, Button? navBtn)
        {
            GpaPage.Visibility    = Visibility.Collapsed;
            CgpaPage.Visibility   = Visibility.Collapsed;
            TargetPage.Visibility = Visibility.Collapsed;
            AdvisorPage.Visibility= Visibility.Collapsed;
            LoginPage.Visibility  = Visibility.Collapsed;

            page.Visibility = Visibility.Visible;
            ResetNavButtons();
            if (navBtn != null) SetNavActive(navBtn);
        }

        private void ResetNavButtons()
        {
            var dim = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b949e"));
            NavGpaBtn.Foreground  = dim; NavCgpaBtn.Foreground  = dim;
            NavTargetBtn.Foreground= dim; NavAdvisorBtn.Foreground= dim;
            NavGpaBtn.Background  = Brushes.Transparent; NavCgpaBtn.Background  = Brushes.Transparent;
            NavTargetBtn.Background= Brushes.Transparent; NavAdvisorBtn.Background= Brushes.Transparent;
        }

        private void SetNavActive(Button btn)
        {
            btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f0f6fc"));
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#21262d"));
        }

        private void NavGpa_Click(object s, RoutedEventArgs e)     => ShowPage(GpaPage,     NavGpaBtn);
        private void NavCgpa_Click(object s, RoutedEventArgs e)    => ShowPage(CgpaPage,    NavCgpaBtn);
        private void NavTarget_Click(object s, RoutedEventArgs e)  => ShowPage(TargetPage,  NavTargetBtn);
        private void NavAdvisor_Click(object s, RoutedEventArgs e) => ShowPage(AdvisorPage, NavAdvisorBtn);
        private void ShowLogin_Click(object s, RoutedEventArgs e)  => ShowPage(LoginPage,   null);

        // ═══════════ UPDATE STATUS ═══════════

        private void UpdateStatusText()
        {
            if (ApiClient.IsLoggedIn)
            {
                UserStatusText.Text       = "🟢 Logged In";
                UserStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3fb950"));
                LoggedInUserText.Text     = ApiClient.CurrentUserName ?? ApiClient.CurrentUserEmail ?? "";
                SaveSemestersBtn.Visibility = Visibility.Visible;
                LoginBtn.Visibility  = Visibility.Collapsed;
                LogoutBtn.Visibility = Visibility.Visible;
            }
            else
            {
                UserStatusText.Text       = "⚫ Offline / Local Mode";
                UserStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b949e"));
                LoggedInUserText.Text     = "";
                SaveSemestersBtn.Visibility = Visibility.Collapsed;
                LoginBtn.Visibility  = Visibility.Visible;
                LogoutBtn.Visibility = Visibility.Collapsed;
            }
        }

        // ═══════════ AUTH TAB SWITCHING ═══════════

        private void SwitchToLogin_Click(object s, RoutedEventArgs e)
        {
            LoginPanel.Visibility    = Visibility.Visible;
            RegisterPanel.Visibility = Visibility.Collapsed;
            TabLoginBtn.Background   = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7c3aed"));
            TabLoginBtn.Foreground   = Brushes.White;
            TabRegisterBtn.Background= new SolidColorBrush((Color)ColorConverter.ConvertFromString("#21262d"));
            TabRegisterBtn.Foreground= new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b949e"));
            LoginMessage.Text        = "";
        }

        private void SwitchToRegister_Click(object s, RoutedEventArgs e)
        {
            LoginPanel.Visibility    = Visibility.Collapsed;
            RegisterPanel.Visibility = Visibility.Visible;
            TabRegisterBtn.Background= new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7c3aed"));
            TabRegisterBtn.Foreground= Brushes.White;
            TabLoginBtn.Background   = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#21262d"));
            TabLoginBtn.Foreground   = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b949e"));
            LoginMessage.Text        = "";
        }

        // ═══════════ LOGIN ═══════════

        private async void DoLogin_Click(object s, RoutedEventArgs e)
        {
            LoginMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b949e"));
            LoginMessage.Text = "Logging in…";

            if (string.IsNullOrWhiteSpace(LoginEmail.Text) || string.IsNullOrWhiteSpace(LoginPassword.Password))
            {
                LoginMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f85149"));
                LoginMessage.Text = "⚠️ Please enter your email and password.";
                return;
            }

            ApiClient.IsOnlineMode = true;
            var res = await ApiClient.LoginAsync(LoginEmail.Text.Trim(), LoginPassword.Password);

            if (res.Success)
            {
                ApiClient.SetToken(res.Token!, res.Name!, res.Email!);
                LoginMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3fb950"));
                LoginMessage.Text = $"✅ Welcome back, {res.Name}!";
                LoginPassword.Password = "";
                UpdateStatusText();

                // Auto-load saved semesters from the database
                await LoadSemestersFromCloud();
                await Task.Delay(800);
                ShowPage(CgpaPage, NavCgpaBtn);
            }
            else
            {
                ApiClient.IsOnlineMode = false;
                LoginMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f85149"));
                LoginMessage.Text = res.Message;
            }
        }

        private async void DoRegister_Click(object s, RoutedEventArgs e)
        {
            LoginMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b949e"));
            LoginMessage.Text = "Creating account…";

            if (string.IsNullOrWhiteSpace(RegisterName.Text) ||
                string.IsNullOrWhiteSpace(RegisterEmail.Text) ||
                string.IsNullOrWhiteSpace(RegisterPassword.Password))
            {
                LoginMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f85149"));
                LoginMessage.Text = "⚠️ Please fill in all fields.";
                return;
            }

            ApiClient.IsOnlineMode = true;
            var res = await ApiClient.RegisterAsync(RegisterName.Text.Trim(), RegisterEmail.Text.Trim(), RegisterPassword.Password);

            ApiClient.IsOnlineMode = false;
            LoginMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(res.Success ? "#3fb950" : "#f85149"));
            LoginMessage.Text = res.Message;

            if (res.Success)
            {
                // Auto-switch to Login tab so user can sign in
                RegisterPassword.Password = "";
                await Task.Delay(1200);
                SwitchToLogin_Click(s, e);
                LoginEmail.Text = RegisterEmail.Text;
                LoginMessage.Text = "✅ Account created! Enter your password to sign in.";
                LoginMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3fb950"));
            }
        }

        private void DoLogout_Click(object s, RoutedEventArgs e)
        {
            ApiClient.Logout();
            UpdateStatusText();
            CgpaSemestersPanel.Children.Clear();
            _semesterCount = 0;
            AddSemesterRow();
            ShowPage(GpaPage, NavGpaBtn);
            AddChatMessage("agent", "👋 You have been logged out. Your local data has been cleared.\nLog in again to restore your saved semesters from the cloud.");
        }

        // ═══════════ GPA CALCULATOR ═══════════

        private void AddSubject_Click(object s, RoutedEventArgs e) => AddSubjectRow();

        private void AddSubjectRow()
        {
            _subjectCount++;
            var row = MakeRowBorder();
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

            var nameBox     = new TextBox { Text = $"Subject {_subjectCount}", Margin = new Thickness(0,0,8,0), Tag = "SubjectName" };
            var creditsCombo= new ComboBox { Margin = new Thickness(0,0,8,0), Tag = "Credits" };
            for (int i = 1; i <= 5; i++) creditsCombo.Items.Add(i);
            creditsCombo.SelectedIndex = 2;

            var gradeCombo  = new ComboBox { Margin = new Thickness(0,0,8,0), Tag = "Grade" };
            foreach (var g in Grades) gradeCombo.Items.Add(g);
            gradeCombo.SelectedIndex = 0;

            var removeBtn = new Button { Style = (Style)FindResource("DangerButton") };
            removeBtn.Click += (_, _) => GpaSubjectsPanel.Children.Remove(row);

            Grid.SetColumn(nameBox,     0); grid.Children.Add(nameBox);
            Grid.SetColumn(creditsCombo,1); grid.Children.Add(creditsCombo);
            Grid.SetColumn(gradeCombo,  2); grid.Children.Add(gradeCombo);
            Grid.SetColumn(removeBtn,   3); grid.Children.Add(removeBtn);

            row.Child = grid;
            GpaSubjectsPanel.Children.Add(row);
        }

        private void CalculateGpa_Click(object s, RoutedEventArgs e)
        {
            double totalQP = 0, totalCredits = 0;

            foreach (Border row in GpaSubjectsPanel.Children)
            {
                var grid = (Grid)row.Child;
                ComboBox? creditsCombo = null, gradeCombo = null;
                foreach (UIElement child in grid.Children)
                {
                    if (child is ComboBox cb)
                    {
                        if (cb.Tag?.ToString() == "Credits") creditsCombo = cb;
                        else if (cb.Tag?.ToString() == "Grade")   gradeCombo  = cb;
                    }
                }
                if (creditsCombo?.SelectedItem == null || gradeCombo?.SelectedItem == null) continue;
                int    credits = (int)creditsCombo.SelectedItem;
                string grade   = gradeCombo.SelectedItem.ToString()!;
                if (GradePoints.TryGetValue(grade, out double pts)) { totalQP += credits * pts; totalCredits += credits; }
            }

            if (totalCredits == 0) { MessageBox.Show("Please add at least one subject.", "No Data"); return; }

            double gpa = totalQP / totalCredits;
            GpaResultText.Text     = gpa.ToString("F2");
            GpaClassText.Text      = GetClassification(gpa);
            GpaClassText.Foreground= GetClassBrush(gpa);
            GpaResultBorder.Visibility = Visibility.Visible;
        }

        // ═══════════ CGPA CALCULATOR ═══════════

        private void AddSemester_Click(object s, RoutedEventArgs e) => AddSemesterRow();

        private void AddSemesterRow(string name = "", string gpa = "0.00", string credits = "15")
        {
            _semesterCount++;
            var row  = MakeRowBorder();
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40)  });

            var nameBox    = new TextBox { Text = string.IsNullOrEmpty(name) ? $"Semester {_semesterCount}" : name, Margin = new Thickness(0,0,8,0), Tag = "SemName"    };
            var gpaBox     = new TextBox { Text = gpa,     Margin = new Thickness(0,0,8,0), Tag = "SemGpa"     };
            var creditsBox = new TextBox { Text = credits, Margin = new Thickness(0,0,8,0), Tag = "SemCredits" };
            var removeBtn  = new Button { Style = (Style)FindResource("DangerButton") };
            removeBtn.Click += (_, _) => CgpaSemestersPanel.Children.Remove(row);

            Grid.SetColumn(nameBox,    0); grid.Children.Add(nameBox);
            Grid.SetColumn(gpaBox,     1); grid.Children.Add(gpaBox);
            Grid.SetColumn(creditsBox, 2); grid.Children.Add(creditsBox);
            Grid.SetColumn(removeBtn,  3); grid.Children.Add(removeBtn);

            row.Child = grid;
            CgpaSemestersPanel.Children.Add(row);
        }

        private void CalculateCgpa_Click(object s, RoutedEventArgs e)
        {
            double totalQP = 0, totalCredits = 0;
            foreach (Border row in CgpaSemestersPanel.Children)
            {
                var grid = (Grid)row.Child;
                TextBox? gpaBox = null, creditsBox = null;
                foreach (UIElement child in grid.Children)
                    if (child is TextBox tb)
                    {
                        if (tb.Tag?.ToString() == "SemGpa")     gpaBox     = tb;
                        else if (tb.Tag?.ToString() == "SemCredits") creditsBox = tb;
                    }
                if (gpaBox == null || creditsBox == null) continue;
                if (!double.TryParse(gpaBox.Text, out double g) || !double.TryParse(creditsBox.Text, out double c)) continue;
                totalQP += g * c; totalCredits += c;
            }
            if (totalCredits == 0) { MessageBox.Show("Add at least one semester.", "No Data"); return; }
            double cgpa = totalQP / totalCredits;
            CgpaResultText.Text     = cgpa.ToString("F2");
            CgpaClassText.Text      = GetClassification(cgpa);
            CgpaClassText.Foreground= GetClassBrush(cgpa);
            CgpaResultBorder.Visibility = Visibility.Visible;
        }

        private async void SaveSemesters_Click(object s, RoutedEventArgs e)
        {
            if (!ApiClient.IsLoggedIn) { MessageBox.Show("Please log in first.", "Not Logged In"); return; }
            var dtos = CollectSemesterDtos();
            SaveSemestersBtn.IsEnabled = false;
            SaveSemestersBtn.Content   = "💾 Saving…";
            
            await ApiClient.DeleteAllRecordsAsync();
            bool ok = true;
            foreach (var dto in dtos)
            {
                ok &= await ApiClient.SaveGpaRecordAsync(new GpaRecordDto { Semester = dto.Name, GPA = dto.Gpa, CGPA = dto.Gpa }); // For CGPA we just use GPA since we calculate it on the fly here
            }

            SaveSemestersBtn.IsEnabled = true;
            SaveSemestersBtn.Content   = ok ? "✅ Saved!" : "❌ Save Failed";
            await Task.Delay(2000);
            SaveSemestersBtn.Content = "💾  Save to Cloud";
        }

        private async Task LoadSemestersFromCloud()
        {
            var sems = await ApiClient.GetGpaRecordsAsync();
            if (sems == null || sems.Count == 0) return;

            CgpaSemestersPanel.Children.Clear();
            _semesterCount = 0;
            foreach (var sem in sems)
                AddSemesterRow(sem.Semester, sem.GPA.ToString("F2"), "15"); // default credits

            // Notify in the chat
            AddChatMessage("agent",
                $"✅ Loaded {sems.Count} saved semester(s) from your account into the CGPA Calculator.\n" +
                "Head to 📈 CGPA Calculator to see your history!");
        }

        private List<(string Name, double Gpa, double Credits)> CollectSemesterDtos()
        {
            var list = new List<(string Name, double Gpa, double Credits)>();
            foreach (Border row in CgpaSemestersPanel.Children)
            {
                var grid = (Grid)row.Child;
                string name = ""; double gpa = 0, credits = 0;
                foreach (UIElement child in grid.Children)
                    if (child is TextBox tb)
                    {
                        if      (tb.Tag?.ToString() == "SemName")    name    = tb.Text;
                        else if (tb.Tag?.ToString() == "SemGpa")     double.TryParse(tb.Text, out gpa);
                        else if (tb.Tag?.ToString() == "SemCredits") double.TryParse(tb.Text, out credits);
                    }
                if (credits > 0) list.Add((name, gpa, credits));
            }
            return list;
        }

        // ═══════════ TARGET GPA PLANNER ═══════════

        private void LoadTargetFromCgpa_Click(object s, RoutedEventArgs e)
        {
            double totalQP = 0, totalCredits = 0;
            foreach (Border row in CgpaSemestersPanel.Children)
            {
                var grid = (Grid)row.Child;
                TextBox? gpaBox = null, creditsBox = null;
                foreach (UIElement child in grid.Children)
                    if (child is TextBox tb)
                    {
                        if (tb.Tag?.ToString() == "SemGpa")     gpaBox     = tb;
                        else if (tb.Tag?.ToString() == "SemCredits") creditsBox = tb;
                    }
                if (gpaBox == null || creditsBox == null) continue;
                if (double.TryParse(gpaBox.Text, out double g) && double.TryParse(creditsBox.Text, out double c))
                { totalQP += g * c; totalCredits += c; }
            }
            TargetCreditsCompleted.Text = totalCredits.ToString();
            TargetCurrentCgpa.Text      = totalCredits > 0 ? (totalQP / totalCredits).ToString("F2") : "0.00";
        }

        private void CalculateTarget_Click(object s, RoutedEventArgs e)
        {
            if (!double.TryParse(TargetCurrentCgpa.Text,       out double currentCgpa)      ||
                !double.TryParse(TargetCreditsCompleted.Text,  out double creditsCompleted)  ||
                !double.TryParse(TargetRemainingCredits.Text,  out double remainingCredits)  ||
                !int.TryParse   (TargetRemainingSemesters.Text, out int    remainingSemesters)||
                remainingCredits <= 0 || remainingSemesters <= 0) 
            {
                MessageBox.Show("Please fill in all fields with valid values.", "Invalid Input");
                return;
            }

            double targetCgpa = TargetCgpaValues[TargetClassCombo.SelectedIndex];
            double currentQP  = currentCgpa * creditsCompleted;
            double totalCredits= creditsCompleted + remainingCredits;
            double requiredQP  = targetCgpa * totalCredits;
            double requiredGpa = (requiredQP - currentQP) / remainingCredits;
            double creditsPerSem = remainingCredits / remainingSemesters;

            // Primary result title & text
            if (requiredGpa > 4.0)
            {
                TargetResultTitle.Text      = "⚠️ Not Achievable";
                TargetResultTitle.Foreground= C("#f85149");
                TargetResultText.Text       = $"You would need a GPA of {requiredGpa:F2}, which exceeds the 4.0 maximum.\n" +
                                              "Consider choosing a lower target class or enrolling in more credits.";
            }
            else if (requiredGpa <= 0)
            {
                TargetResultTitle.Text      = "🎉 Already Achieved!";
                TargetResultTitle.Foreground= C("#3fb950");
                TargetResultText.Text       = $"You have already met the {GetClassLabel(targetCgpa)} requirement with your current CGPA of {currentCgpa:F2}!";
            }
            else
            {
                TargetResultTitle.Text      = $"📌 Required GPA: {requiredGpa:F2}";
                TargetResultTitle.Foreground= GetClassBrush(requiredGpa);
                TargetResultText.Text       = $"You need an average GPA of {requiredGpa:F2} across your remaining {remainingSemesters} semester(s).\n" +
                                              $"That is approximately {creditsPerSem:F0} credits per semester at {requiredGpa:F2} GPA each.";
            }

            // Build upgrade paths table for ALL tiers
            TargetUpgradePathsPanel.Children.Clear();
            foreach (var (tierName, minGpa, color) in Tiers)
            {
                double rqp = minGpa * totalCredits;
                double rgpa= (rqp - currentQP) / remainingCredits;
                string statusIcon;
                string statusText;
                string borderColor;

                if (currentCgpa >= minGpa) { statusIcon="✅"; statusText = "Already Achieved!"; borderColor="#3fb950"; }
                else if (rgpa > 4.0)        { statusIcon="❌"; statusText = $"Not Achievable (need {rgpa:F2})"; borderColor="#f85149"; }
                else                        { statusIcon="🎯"; statusText = $"Need {rgpa:F2} / sem for {remainingSemesters} semesters"; borderColor=color; }

                var card = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0d1117")),
                    CornerRadius= new CornerRadius(8),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borderColor)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(14, 10, 14, 10),
                    Margin  = new Thickness(0, 0, 0, 8)
                };
                var sp = new StackPanel { Orientation = Orientation.Horizontal };
                sp.Children.Add(new TextBlock { Text = $"{statusIcon}  {tierName}", FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)), FontSize = 13, MinWidth = 180, VerticalAlignment = VerticalAlignment.Center });
                sp.Children.Add(new TextBlock { Text = $"(≥{minGpa:F2})", Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b949e")), FontSize = 12, Margin = new Thickness(0,0,12,0), VerticalAlignment = VerticalAlignment.Center });
                sp.Children.Add(new TextBlock { Text = statusText, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c9d1d9")), FontSize = 13, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center });
                card.Child = sp;
                TargetUpgradePathsPanel.Children.Add(card);
            }

            TargetResultBorder.Visibility     = Visibility.Visible;
            TargetPlaceholderText.Visibility  = Visibility.Collapsed;
        }

        private static string GetClassLabel(double gpa) => gpa switch
        {
            >= 3.70 => "First Class",
            >= 3.30 => "Second Class Upper",
            >= 3.00 => "Second Class Lower",
            _       => "Pass"
        };

        // ═══════════ ACADEMIC ADVISOR PERFORMANCE PANEL ═══════════

        private void SyncGpaToAdvisor_Click(object s, RoutedEventArgs e)
        {
            // Pull CGPA from the CGPA calculator
            double totalQP = 0, totalCredits = 0;
            foreach (Border row in CgpaSemestersPanel.Children)
            {
                var grid = (Grid)row.Child;
                TextBox? gpaBox = null, creditsBox = null;
                foreach (UIElement child in grid.Children)
                    if (child is TextBox tb)
                    {
                        if (tb.Tag?.ToString() == "SemGpa")         gpaBox     = tb;
                        else if (tb.Tag?.ToString() == "SemCredits") creditsBox = tb;
                    }
                if (gpaBox == null || creditsBox == null) continue;
                if (double.TryParse(gpaBox.Text, out double g) && double.TryParse(creditsBox.Text, out double c))
                { totalQP += g * c; totalCredits += c; }
            }
            double cgpa = totalCredits > 0 ? totalQP / totalCredits : 0;
            AdvisorGpaInput.Text = cgpa.ToString("F2");
        }

        private void GetAdvice_Click(object s, RoutedEventArgs e)
        {
            if (!double.TryParse(AdvisorGpaInput.Text, out double gpa) || gpa < 0 || gpa > 4.0)
            {
                MessageBox.Show("Please enter a valid GPA (0.00 – 4.00).", "Invalid GPA");
                return;
            }

            string classification = GetClassification(gpa);
            CurrentTierLabel.Text = $"Current: {classification}";
            NextTierProgress.Value= gpa;

            // Find next tier
            var nextTier = Tiers.FirstOrDefault(t => t.MinGpa > gpa);
            if (nextTier == default) { NextTierLabel.Text = "Next: Max 🏆"; NextTierProgress.Maximum = 4.0; }
            else { NextTierLabel.Text = $"Next: {nextTier.Name} ({nextTier.MinGpa:F2})"; NextTierProgress.Maximum = nextTier.MinGpa; }

            string advice = gpa switch
            {
                >= 3.70 => "🌟 Outstanding! You are in First Class standing. Sustain your A+/A grades, explore honours research, and aim for academic awards.",
                >= 3.30 => "📈 You are in Second Class Upper. Target A- or above in every subject. Form a study group and engage lecturers early for difficult modules.",
                >= 3.00 => "📊 You are in Second Class Lower. Prioritise B+ grades. Eliminate passive reading — use active recall and past papers daily.",
                >= 2.00 => "⚡ You are in Pass territory. Seek tutors for weak subjects, visit your academic counsellor, and map out a concrete improvement plan.",
                _       => "🚨 Critical standing — you risk failing. Contact your academic advisor immediately and create a recovery plan this week."
            };
            AdvisorTargetInfo.Text = advice;

            // Post analysis summary to the chat
            string nextMsg = nextTier == default
                ? "You are already at the top tier — maintain First Class!"
                : $"To reach {nextTier.Name} you need to raise your CGPA by {nextTier.MinGpa - gpa:F2} points.";

            AddChatMessage("agent",
                $"📊 Performance Analysis\n\n" +
                $"Your GPA: {gpa:F2}  |  {classification}\n\n" +
                $"{advice}\n\n" +
                $"{nextMsg}");
        }

        // ═══════════ CHAT ADVISOR ENGINE ═══════════

        private void Chip_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string text = btn.Content?.ToString() ?? "";
                ChatInputTextBox.Text = text;
                SendChat();
            }
        }

        private void ChatInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { e.Handled = true; SendChat(); }
        }

        private void SendChat_Click(object s, RoutedEventArgs e) => SendChat();

        private void SendChat()
        {
            string input = ChatInputTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(input)) return;

            AddChatMessage("user", input);
            ChatInputTextBox.Text = "";

            string response = GenerateAdvisorResponse(input.ToLowerInvariant());
            AddChatMessage("agent", response);

            // Auto-scroll
            ChatScrollViewer.ScrollToBottom();
        }

        private string GenerateAdvisorResponse(string q)
        {
            // ── HELP & INSTRUCTIONS ──
            if (Contains(q, "help", "instruction", "guide", "how to", "how do", "use app", "what can", "feature"))
                return "📖 Here is a quick guide to the app:\n\n" +
                       "1️⃣  📊 GPA Calculator\n" +
                       "   • Add subjects, pick credits (1–5) and your grade, then tap 'Calculate GPA'.\n\n" +
                       "2️⃣  📈 CGPA Calculator\n" +
                       "   • Add each past semester's GPA and credit hours.\n" +
                       "   • If logged in, your semesters are loaded automatically from the cloud.\n" +
                       "   • Use 'Save to Cloud' to back them up.\n\n" +
                       "3️⃣  🎯 Target Analytics\n" +
                       "   • Click 'Load from CGPA Calculator' to auto-fill your data.\n" +
                       "   • Enter remaining credits and semesters, choose a target class, then hit Calculate.\n" +
                       "   • You will see the GPA needed per semester AND a full upgrade-path table.\n\n" +
                       "4️⃣  💡 AI Advisor (here!)\n" +
                       "   • Click the chips or type a question. I can build timetables, explain strategies, and plan your CGPA upgrades.\n\n" +
                       "5️⃣  🔑 Login (optional)\n" +
                       "   • Click 'Login / Register' in the sidebar. Once logged in, your semester history syncs automatically!";

            // ── STUDY TIMETABLE ──
            if (Contains(q, "timetable", "schedule", "study plan", "weekly plan", "study time", "study schedule", "📅"))
            {
                var subjectNames = GetCurrentSubjectNames();
                return BuildTimetable(subjectNames);
            }

            // ── CGPA UPGRADE PLAN ──
            if (Contains(q, "upgrade", "improve cgpa", "cgpa plan", "raise cgpa", "boost cgpa", "class upgrade", "📈"))
                return BuildCgpaUpgradePlan();

            // ── EXAM TIPS / STUDY STRATEGY ──
            if (Contains(q, "study strategy", "study tip", "exam prep", "exam tip", "active recall", "how to study", "learn better", "memory", "📚"))
                return "📚 Proven Study Strategies:\n\n" +
                       "🔁 Active Recall\n" +
                       "   Close your notes and test yourself. Try to answer questions from memory first, then check. This beats re-reading by 3×.\n\n" +
                       "🗂️ Spaced Repetition\n" +
                       "   Review material after 1 day → 3 days → 7 days → 2 weeks. Use free tools like Anki.\n\n" +
                       "🍅 Pomodoro Technique\n" +
                       "   Study in 25-minute focused blocks, then take a 5-minute break. After 4 blocks, take a 20-minute break.\n\n" +
                       "📝 Past Papers First\n" +
                       "   Attempt past-year papers under timed conditions before your exam. This shows exam patterns.\n\n" +
                       "🤝 Study Groups\n" +
                       "   Teach concepts to group members. Teaching forces deeper understanding.\n\n" +
                       "🛌 Sleep & Nutrition\n" +
                       "   7–8 hours of sleep consolidates memory. Avoid all-nighters before exams.\n\n" +
                       "📊 Prioritise by Grade Weight\n" +
                       "   Focus revision time on high-weighted assessments first.";

            // ── GPA / CGPA QUESTIONS ──
            if (Contains(q, "what is gpa", "what is cgpa", "gpa mean", "cgpa mean", "difference", "gpa vs cgpa"))
                return "🎓 GPA vs CGPA explained:\n\n" +
                       "📊 GPA (Grade Point Average)\n" +
                       "   The weighted average of your grades for a SINGLE semester.\n" +
                       "   Formula: Σ(Credits × Grade Points) ÷ Total Credits\n\n" +
                       "📈 CGPA (Cumulative GPA)\n" +
                       "   The weighted average across ALL semesters combined.\n" +
                       "   It determines your final degree classification.\n\n" +
                       "🏆 Degree Classes (typical scale):\n" +
                       "   ≥ 3.70 → First Class\n" +
                       "   ≥ 3.30 → Second Class Upper\n" +
                       "   ≥ 3.00 → Second Class Lower\n" +
                       "   ≥ 2.00 → Pass\n" +
                       "   < 2.00 → Below Pass";

            // ── MOTIVATION ──
            if (Contains(q, "motivat", "stress", "depressed", "give up", "hard", "difficult", "tired"))
                return "💪 Keep going — every high achiever has been where you are!\n\n" +
                       "\"Success is not final, failure is not fatal: it is the courage to continue that counts.\" — Winston Churchill\n\n" +
                       "Here is a 3-step recovery plan:\n" +
                       "1. Take a 20-minute walk to reset your mind.\n" +
                       "2. List your 3 weakest topics and tackle just 1 today.\n" +
                       "3. Celebrate every small win — crossing off a topic feels great!\n\n" +
                       "You have this! 🌟";

            // ── LOGIN / CLOUD ──
            if (Contains(q, "login", "log in", "account", "cloud", "save", "sync"))
                return "🔑 Cloud Sync Guide:\n\n" +
                       "1. Click '🔑 Login / Register' in the sidebar.\n" +
                       "2. Create a new account or log in to your existing one.\n" +
                       "3. Once logged in, your saved semesters are loaded automatically!\n" +
                       "4. On the CGPA Calculator page, use '💾 Save to Cloud' to back up any changes.\n\n" +
                       "💡 Note: All calculations work offline even without logging in.";

            // ── GREETING ──
            if (Contains(q, "hi", "hello", "hey", "good morning", "good evening", "good afternoon"))
                return "👋 Hello! I'm your Academic Advisor. How can I help you today?\n\n" +
                       "Try asking me:\n" +
                       "• 'Generate a study timetable for my subjects'\n" +
                       "• 'How do I upgrade my CGPA to First Class?'\n" +
                       "• 'Give me study tips for exams'\n" +
                       "• 'How does this app work?'";

            // ── DEFAULT FALLBACK ──
            return "🤔 I'm not sure I understood that. Try one of these:\n\n" +
                   "• 📖 'Help & Instructions'\n" +
                   "• 📅 'Study Timetable'\n" +
                   "• 📈 'Upgrade CGPA Plan'\n" +
                   "• 📚 'Study Strategy'\n" +
                   "• ❓ 'What is GPA?'\n\n" +
                   "Or click one of the quick-action chips above!";
        }

        private string BuildTimetable(List<string> subjects)
        {
            string[] days = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];
            string[] timeSlots = ["8:00–10:00 AM", "10:30 AM–12:30 PM", "2:00–4:00 PM", "4:30–6:30 PM"];
            string[] activities = ["📖 Review Notes", "✏️ Practice Problems", "🔁 Active Recall", "📝 Past Papers"];

            if (subjects.Count == 0)
                subjects = ["Subject A", "Subject B", "Subject C"];

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("📅 Your Personalised Weekly Study Timetable\n");

            // Assign subjects to days with variety
            for (int d = 0; d < 7; d++)
            {
                string day = days[d];
                if (d == 6) { sb.AppendLine($"🏖️ {day}\n   Rest & light review — your brain needs recovery!"); continue; }

                sb.AppendLine($"📆 {day}");
                string subject1 = subjects[d % subjects.Count];
                string subject2 = subjects[(d + 1) % subjects.Count];
                string activity1 = activities[d % activities.Length];
                string activity2 = activities[(d + 2) % activities.Length];

                sb.AppendLine($"   {timeSlots[0]}  →  {subject1}  |  {activity1}");
                sb.AppendLine($"   {timeSlots[1]}  →  {subject2}  |  {activity2}");
                if (d < 5)
                {
                    string subject3 = subjects[(d + 2) % subjects.Count];
                    sb.AppendLine($"   {timeSlots[2]}  →  {subject3}  |  {activities[(d + 1) % activities.Length]}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("💡 Tips:");
            sb.AppendLine("   • Review your weakest subject first each morning.");
            sb.AppendLine("   • Use 5-minute breaks between blocks.");
            sb.AppendLine("   • Drink water and take a short walk between sessions.");

            return sb.ToString();
        }

        private string BuildCgpaUpgradePlan()
        {
            // Try to read current CGPA from CGPA panel
            double totalQP = 0, totalCredits = 0;
            foreach (Border row in CgpaSemestersPanel.Children)
            {
                var grid = (Grid)row.Child;
                TextBox? gpaBox = null, creditsBox = null;
                foreach (UIElement child in grid.Children)
                    if (child is TextBox tb)
                    {
                        if (tb.Tag?.ToString() == "SemGpa")         gpaBox     = tb;
                        else if (tb.Tag?.ToString() == "SemCredits") creditsBox = tb;
                    }
                if (gpaBox == null || creditsBox == null) continue;
                if (double.TryParse(gpaBox.Text, out double g) && double.TryParse(creditsBox.Text, out double c))
                { totalQP += g * c; totalCredits += c; }
            }

            double currentCgpa = totalCredits > 0 ? totalQP / totalCredits : 0;
            string currentClass = GetClassification(currentCgpa);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"📈 CGPA Upgrade Roadmap\n");
            sb.AppendLine($"Your Current CGPA: {currentCgpa:F2}  ({currentClass})\n");

            if (totalCredits == 0)
            {
                sb.AppendLine("⚠️ No semesters found in your CGPA Calculator.");
                sb.AppendLine("Please add your past semesters first, then ask again!\n");
                sb.AppendLine("→ Go to 📈 CGPA Calculator → Add Semester → enter your GPA and credits.");
                return sb.ToString();
            }

            // Calculate requirements for each tier above current
            bool foundUpgrade = false;
            foreach (var (tierName, minGpa, color) in Tiers)
            {
                if (currentCgpa >= minGpa) continue;
                foundUpgrade = true;

                // Assuming 60 remaining credits / 4 semesters as example
                double remCredits = 60, remSem = 4;
                double rqp  = minGpa * (totalCredits + remCredits);
                double rgpa = (rqp - totalQP) / remCredits;
                double rgpaPerSem = rgpa;

                sb.AppendLine($"🎯 To reach {tierName} (≥{minGpa:F2}):");
                if (rgpa > 4.0)
                    sb.AppendLine($"   ❌ Requires {rgpa:F2}/sem — not achievable in {remSem} sems with {remCredits} credits.");
                else
                    sb.AppendLine($"   ✅ Maintain ≥ {rgpa:F2} GPA each semester\n   (assuming {(int)remCredits} remaining credits over {(int)remSem} semesters)");

                sb.AppendLine($"   Strategy:");
                if (minGpa >= 3.70)
                    sb.AppendLine("   • Aim for A+ / A in every core subject.\n   • Engage in research or honours projects.\n   • Attend every lecture and ask questions.");
                else if (minGpa >= 3.30)
                    sb.AppendLine("   • Target A- and above.\n   • Form study groups for tough modules.\n   • Submit assignments early for feedback.");
                else
                    sb.AppendLine("   • Focus on B+ and above.\n   • Practice past papers weekly.\n   • Seek tutors for subjects below B.");
                sb.AppendLine();
            }

            if (!foundUpgrade)
                sb.AppendLine("🏆 You have already achieved First Class standing! Maintain your current performance and aim for academic excellence awards.");

            sb.AppendLine("💡 For precise numbers, use the 🎯 Target Analytics page and enter your exact remaining credits and semesters.");
            return sb.ToString();
        }

        private List<string> GetCurrentSubjectNames()
        {
            var names = new List<string>();
            foreach (Border row in GpaSubjectsPanel.Children)
            {
                var grid = (Grid)row.Child;
                foreach (UIElement child in grid.Children)
                    if (child is TextBox tb && tb.Tag?.ToString() == "SubjectName" && !string.IsNullOrWhiteSpace(tb.Text))
                        names.Add(tb.Text.Trim());
            }
            return names;
        }

        // ═══════════ CHAT MESSAGE BUILDER ═══════════

        private void AddChatMessage(string sender, string message)
        {
            bool isUser = sender == "user";

            var outer = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            var bubble= new Border
            {
                CornerRadius    = new CornerRadius(isUser ? 16 : 12),
                Padding         = new Thickness(16, 12, 16, 12),
                MaxWidth        = 520,
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            };

            if (isUser)
            {
                bubble.Background = new LinearGradientBrush(
                    (Color)ColorConverter.ConvertFromString("#7c3aed"),
                    (Color)ColorConverter.ConvertFromString("#2563eb"),
                    new System.Windows.Point(0, 0), new System.Windows.Point(1, 1));
            }
            else
            {
                bubble.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#21262d"));
                bubble.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#30363d"));
                bubble.BorderThickness = new Thickness(1);
            }

            // Avatar label for agent
            var sp = new StackPanel();

            if (!isUser)
            {
                sp.Children.Add(new TextBlock
                {
                    Text      = "🤖 Academic Advisor",
                    FontSize  = 11,
                    FontWeight= FontWeights.SemiBold,
                    Foreground= new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7c3aed")),
                    Margin    = new Thickness(0, 0, 0, 6)
                });
            }

            sp.Children.Add(new TextBlock
            {
                Text        = message,
                FontSize    = 13,
                Foreground  = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isUser ? "#ffffff" : "#c9d1d9")),
                TextWrapping= TextWrapping.Wrap,
                LineHeight  = 22
            });

            // Timestamp
            sp.Children.Add(new TextBlock
            {
                Text       = DateTime.Now.ToString("hh:mm tt"),
                FontSize   = 10,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isUser ? "#c4b5fd" : "#484f58")),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Margin     = new Thickness(0, 6, 0, 0)
            });

            bubble.Child = sp;
            outer.Children.Add(bubble);
            ChatMessagesPanel.Children.Add(outer);

            // Scroll to bottom
            ChatScrollViewer.ScrollToBottom();
        }

        // ═══════════ HELPERS ═══════════

        private static Border MakeRowBorder() => new Border
        {
            Background      = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0d1117")),
            CornerRadius    = new CornerRadius(8),
            Padding         = new Thickness(12, 8, 12, 8),
            Margin          = new Thickness(0, 0, 0, 8),
            BorderBrush     = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#30363d")),
            BorderThickness = new Thickness(1)
        };

        private static bool Contains(string input, params string[] keywords)
            => keywords.Any(k => input.Contains(k, StringComparison.OrdinalIgnoreCase));

        private static SolidColorBrush C(string hex)
            => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

        private static string GetClassification(double gpa) => gpa switch
        {
            >= 3.70 => "First Class",
            >= 3.30 => "Second Class Upper",
            >= 3.00 => "Second Class Lower",
            >= 2.00 => "Pass",
            _       => "Below Pass"
        };

        private static SolidColorBrush GetClassBrush(double gpa)
            => C(GetClassColor(gpa));

        private static string GetClassColor(double gpa) => gpa switch
        {
            >= 3.70 => "#3fb950",
            >= 3.30 => "#2563eb",
            >= 3.00 => "#d29922",
            _       => "#f85149"
        };
    }
}
