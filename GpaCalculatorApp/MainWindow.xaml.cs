using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GpaCalculatorApp
{
    public partial class MainWindow : Window
    {
        private static readonly Dictionary<string, double> GradePoints = new()
        {
            {"A+", 4.0}, {"A", 3.7}, {"A-", 3.3},
            {"B+", 3.0}, {"B", 2.7}, {"B-", 2.3},
            {"C+", 2.0}, {"C", 1.7}, {"C-", 1.3},
            {"D", 1.0}, {"F", 0.0}
        };

        private static readonly string[] Grades = ["A+", "A", "A-", "B+", "B", "B-", "C+", "C", "C-", "D", "F"];
        private static readonly double[] TargetCgpaValues = [3.70, 3.30, 3.00];

        private int _subjectCount = 0;
        private int _semesterCount = 0;

        public MainWindow()
        {
            InitializeComponent();
            ShowPage(GpaPage, NavGpaBtn);

            AddSubjectRow();
            AddSubjectRow();
            AddSemesterRow();
        }

        // ═══════════ NAVIGATION ═══════════

        private void ShowPage(Grid page, Button? navBtn)
        {
            GpaPage.Visibility = Visibility.Collapsed;
            CgpaPage.Visibility = Visibility.Collapsed;
            TargetPage.Visibility = Visibility.Collapsed;
            AdvisorPage.Visibility = Visibility.Collapsed;
            LoginPage.Visibility = Visibility.Collapsed;

            page.Visibility = Visibility.Visible;

            ResetNavButtons();
            if (navBtn != null) SetNavActive(navBtn);
        }

        private void ResetNavButtons()
        {
            var secondary = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b949e"));
            NavGpaBtn.Foreground = secondary; NavCgpaBtn.Foreground = secondary;
            NavTargetBtn.Foreground = secondary; NavAdvisorBtn.Foreground = secondary;
            NavGpaBtn.Background = Brushes.Transparent; NavCgpaBtn.Background = Brushes.Transparent;
            NavTargetBtn.Background = Brushes.Transparent; NavAdvisorBtn.Background = Brushes.Transparent;
        }

        private void SetNavActive(Button btn)
        {
            btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f0f6fc"));
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#21262d"));
        }

        private void NavGpa_Click(object sender, RoutedEventArgs e) => ShowPage(GpaPage, NavGpaBtn);
        private void NavCgpa_Click(object sender, RoutedEventArgs e) => ShowPage(CgpaPage, NavCgpaBtn);
        private void NavTarget_Click(object sender, RoutedEventArgs e) => ShowPage(TargetPage, NavTargetBtn);
        private void NavAdvisor_Click(object sender, RoutedEventArgs e) => ShowPage(AdvisorPage, NavAdvisorBtn);
        private void ShowLogin_Click(object sender, RoutedEventArgs e) => ShowPage(LoginPage, null);

        private void OnlineMode_Click(object sender, RoutedEventArgs e)
        {
            ApiClient.IsOnlineMode = OnlineModeToggle.IsChecked ?? false;
            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            if (!ApiClient.IsOnlineMode)
            {
                UserStatusText.Text = "Offline / Local Mode";
                UserStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b949e"));
                SaveSemestersBtn.Visibility = Visibility.Collapsed;
            }
            else if (ApiClient.IsLoggedIn)
            {
                UserStatusText.Text = "Online / Logged In";
                UserStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3fb950"));
                SaveSemestersBtn.Visibility = Visibility.Visible;
            }
            else
            {
                UserStatusText.Text = "Online / Not Logged In";
                UserStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#d29922"));
                SaveSemestersBtn.Visibility = Visibility.Collapsed;
            }
        }

        // ═══════════ LOGIN ═══════════

        private async void DoLogin_Click(object sender, RoutedEventArgs e)
        {
            // Bug Fix: Validate input before calling API
            if (string.IsNullOrWhiteSpace(LoginUsername.Text) || string.IsNullOrWhiteSpace(LoginPassword.Password))
            {
                LoginMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f85149"));
                LoginMessage.Text = "Please enter username and password.";
                return;
            }

            LoginMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b949e"));
            LoginMessage.Text = "Logging in...";

            // Bug Fix 1: Use LoginPassword.Password (PasswordBox) instead of .Text
            var res = await ApiClient.LoginAsync(LoginUsername.Text, LoginPassword.Password);
            if (res.Success)
            {
                ApiClient.SetToken(res.Token!);
                LoginMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3fb950"));
                LoginMessage.Text = $"Welcome, {res.Username}!";
                UpdateStatusText();
            }
            else
            {
                LoginMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f85149"));
                LoginMessage.Text = res.Message;
            }
        }

        private async void DoRegister_Click(object sender, RoutedEventArgs e)
        {
            // Bug Fix: Validate input
            if (string.IsNullOrWhiteSpace(LoginUsername.Text) || LoginUsername.Text.Length < 3)
            {
                LoginMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f85149"));
                LoginMessage.Text = "Username must be at least 3 characters.";
                return;
            }
            if (string.IsNullOrWhiteSpace(LoginPassword.Password) || LoginPassword.Password.Length < 6)
            {
                LoginMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f85149"));
                LoginMessage.Text = "Password must be at least 6 characters.";
                return;
            }

            LoginMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b949e"));
            LoginMessage.Text = "Registering...";

            // Bug Fix 1: Use LoginPassword.Password (PasswordBox) instead of .Text
            var res = await ApiClient.RegisterAsync(LoginUsername.Text, LoginPassword.Password);
            LoginMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(res.Success ? "#3fb950" : "#f85149"));
            LoginMessage.Text = res.Message;
        }

        // ═══════════ GPA CALCULATOR ═══════════

        private void AddSubject_Click(object sender, RoutedEventArgs e) => AddSubjectRow();

        private void AddSubjectRow()
        {
            _subjectCount++;
            var row = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0d1117")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 8),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#30363d")),
                BorderThickness = new Thickness(1)
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

            var nameBox = new TextBox { Text = $"Subject {_subjectCount}", Margin = new Thickness(0, 0, 8, 0), Tag = "SubjectName" };
            Grid.SetColumn(nameBox, 0); grid.Children.Add(nameBox);

            var creditsCombo = new ComboBox { Margin = new Thickness(0, 0, 8, 0), Tag = "Credits" };
            for (int i = 1; i <= 5; i++) creditsCombo.Items.Add(i);
            creditsCombo.SelectedIndex = 2;
            Grid.SetColumn(creditsCombo, 1); grid.Children.Add(creditsCombo);

            var gradeCombo = new ComboBox { Margin = new Thickness(0, 0, 8, 0), Tag = "Grade" };
            foreach (var g in Grades) gradeCombo.Items.Add(g);
            gradeCombo.SelectedIndex = 0;
            Grid.SetColumn(gradeCombo, 2); grid.Children.Add(gradeCombo);

            var removeBtn = new Button { Style = (Style)FindResource("DangerButton") };
            removeBtn.Click += (s, e) => GpaSubjectsPanel.Children.Remove(row);
            Grid.SetColumn(removeBtn, 3); grid.Children.Add(removeBtn);

            row.Child = grid;
            GpaSubjectsPanel.Children.Add(row);
        }

        private async void CalculateGpa_Click(object sender, RoutedEventArgs e)
        {
            // Bug Fix: Check if there are any subjects first
            if (GpaSubjectsPanel.Children.Count == 0)
            {
                MessageBox.Show("Please add at least one subject.", "No Subjects", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var subjects = new List<object>();
            double totalQualityPoints = 0;
            double totalCredits = 0;

            foreach (Border row in GpaSubjectsPanel.Children)
            {
                var grid = (Grid)row.Child;
                ComboBox? creditsCombo = null; ComboBox? gradeCombo = null;
                foreach (UIElement child in grid.Children)
                {
                    if (child is ComboBox cb)
                    {
                        if (cb.Tag?.ToString() == "Credits") creditsCombo = cb;
                        else if (cb.Tag?.ToString() == "Grade") gradeCombo = cb;
                    }
                }
                if (creditsCombo?.SelectedItem == null || gradeCombo?.SelectedItem == null) continue;

                int credits = (int)creditsCombo.SelectedItem;
                string grade = gradeCombo.SelectedItem.ToString()!;
                subjects.Add(new { Credits = credits, Grade = grade });

                if (GradePoints.TryGetValue(grade, out double points))
                {
                    totalQualityPoints += credits * points;
                    totalCredits += credits;
                }
            }

            if (totalCredits == 0) return;

            double gpa = 0;
            if (ApiClient.IsOnlineMode)
            {
                var onlineGpa = await ApiClient.CalculateGpaOnlineAsync(subjects);
                // Bug Fix 2: Graceful fallback (no crash) — try-catch is in ApiClient
                gpa = onlineGpa.HasValue ? onlineGpa.Value : totalQualityPoints / totalCredits;
                if (!onlineGpa.HasValue)
                    MessageBox.Show("API unavailable. Showing offline result.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                gpa = totalQualityPoints / totalCredits;
            }

            GpaResultText.Text = gpa.ToString("F2");
            GpaClassText.Text = GetClassification(gpa);
            GpaClassText.Foreground = GetClassBrush(gpa);
            GpaResultBorder.Visibility = Visibility.Visible;
        }

        // ═══════════ CGPA CALCULATOR ═══════════

        private void AddSemester_Click(object sender, RoutedEventArgs e) => AddSemesterRow();

        private void AddSemesterRow(string name = "", string gpa = "0.00", string credits = "15")
        {
            _semesterCount++;
            var row = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0d1117")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 8),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#30363d")),
                BorderThickness = new Thickness(1)
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

            var nameBox = new TextBox { Text = string.IsNullOrEmpty(name) ? $"Semester {_semesterCount}" : name, Margin = new Thickness(0, 0, 8, 0), Tag = "SemName" };
            Grid.SetColumn(nameBox, 0); grid.Children.Add(nameBox);

            var gpaBox = new TextBox { Text = gpa, Margin = new Thickness(0, 0, 8, 0), Tag = "SemGpa" };
            Grid.SetColumn(gpaBox, 1); grid.Children.Add(gpaBox);

            var creditsBox = new TextBox { Text = credits, Margin = new Thickness(0, 0, 8, 0), Tag = "SemCredits" };
            Grid.SetColumn(creditsBox, 2); grid.Children.Add(creditsBox);

            var removeBtn = new Button { Style = (Style)FindResource("DangerButton") };
            removeBtn.Click += (s, e) => CgpaSemestersPanel.Children.Remove(row);
            Grid.SetColumn(removeBtn, 3); grid.Children.Add(removeBtn);

            row.Child = grid;
            CgpaSemestersPanel.Children.Add(row);
        }

        private void CalculateCgpa_Click(object sender, RoutedEventArgs e)
        {
            if (CgpaSemestersPanel.Children.Count == 0)
            {
                MessageBox.Show("Please add at least one semester.", "No Semesters", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double totalQualityPoints = 0;
            double totalCredits = 0;

            foreach (Border row in CgpaSemestersPanel.Children)
            {
                var grid = (Grid)row.Child;
                TextBox? gpaBox = null; TextBox? creditsBox = null;
                foreach (UIElement child in grid.Children)
                {
                    if (child is TextBox tb)
                    {
                        if (tb.Tag?.ToString() == "SemGpa") gpaBox = tb;
                        else if (tb.Tag?.ToString() == "SemCredits") creditsBox = tb;
                    }
                }
                if (gpaBox == null || creditsBox == null) continue;

                // Bug Fix: Validate GPA range (0.0 – 4.0)
                if (!double.TryParse(gpaBox.Text, out double gpa))
                {
                    MessageBox.Show($"Invalid GPA value: '{gpaBox.Text}'. Please enter a number.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (gpa < 0 || gpa > 4.0)
                {
                    MessageBox.Show($"GPA must be between 0.0 and 4.0. Got: {gpa}", "Invalid GPA", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!double.TryParse(creditsBox.Text, out double credits) || credits <= 0)
                {
                    MessageBox.Show($"Invalid credits value: '{creditsBox.Text}'. Please enter a positive number.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                totalQualityPoints += gpa * credits;
                totalCredits += credits;
            }

            if (totalCredits == 0) return;

            double cgpa = totalQualityPoints / totalCredits;
            CgpaResultText.Text = cgpa.ToString("F2");
            CgpaClassText.Text = GetClassification(cgpa);
            CgpaClassText.Foreground = GetClassBrush(cgpa);
            CgpaResultBorder.Visibility = Visibility.Visible;
        }

        // Bug Fix 3: Implemented — actually saves semesters to cloud
        private async void SaveSemesters_Click(object sender, RoutedEventArgs e)
        {
            if (!ApiClient.IsLoggedIn)
            {
                MessageBox.Show("Please log in first to save to cloud.", "Not Logged In", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (CgpaSemestersPanel.Children.Count == 0)
            {
                MessageBox.Show("No semesters to save.", "Empty", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int saved = 0;
            int failed = 0;

            foreach (Border row in CgpaSemestersPanel.Children)
            {
                var grid = (Grid)row.Child;
                TextBox? nameBox = null, gpaBox = null, creditsBox = null;
                foreach (UIElement child in grid.Children)
                {
                    if (child is TextBox tb)
                    {
                        if (tb.Tag?.ToString() == "SemName") nameBox = tb;
                        else if (tb.Tag?.ToString() == "SemGpa") gpaBox = tb;
                        else if (tb.Tag?.ToString() == "SemCredits") creditsBox = tb;
                    }
                }
                if (nameBox == null || gpaBox == null || creditsBox == null) continue;
                if (!double.TryParse(gpaBox.Text, out double gpa)) continue;
                if (!double.TryParse(creditsBox.Text, out double credits)) continue;

                bool ok = await ApiClient.SaveSemesterAsync(nameBox.Text, gpa, credits);
                if (ok) saved++; else failed++;
            }

            if (failed == 0)
                MessageBox.Show($"✅ {saved} semester(s) saved to cloud!", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show($"⚠️ {saved} saved, {failed} failed. Check your connection.", "Partial Save", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // ═══════════ TARGET GPA PLANNER ═══════════

        private void LoadTargetFromCgpa_Click(object sender, RoutedEventArgs e)
        {
            double totalQualityPoints = 0;
            double totalCredits = 0;

            foreach (Border row in CgpaSemestersPanel.Children)
            {
                var grid = (Grid)row.Child;
                TextBox? gpaBox = null; TextBox? creditsBox = null;
                foreach (UIElement child in grid.Children)
                {
                    if (child is TextBox tb)
                    {
                        if (tb.Tag?.ToString() == "SemGpa") gpaBox = tb;
                        else if (tb.Tag?.ToString() == "SemCredits") creditsBox = tb;
                    }
                }
                if (gpaBox == null || creditsBox == null) continue;
                if (double.TryParse(gpaBox.Text, out double gpa) && double.TryParse(creditsBox.Text, out double credits))
                {
                    totalQualityPoints += gpa * credits;
                    totalCredits += credits;
                }
            }

            TargetCreditsCompleted.Text = totalCredits.ToString();
            TargetCurrentCgpa.Text = totalCredits > 0 ? (totalQualityPoints / totalCredits).ToString("F2") : "0.00";
        }

        private async void CalculateTarget_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(TargetCurrentCgpa.Text, out double currentCgpa))
            {
                MessageBox.Show("Please enter a valid Current CGPA.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!double.TryParse(TargetCreditsCompleted.Text, out double creditsCompleted))
            {
                MessageBox.Show("Please enter a valid Credits Completed value.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!double.TryParse(TargetRemainingCredits.Text, out double remainingCredits) || remainingCredits <= 0)
            {
                MessageBox.Show("Please enter a valid Remaining Credits (must be > 0).", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Bug Fix: Guard against invalid SelectedIndex
            if (TargetClassCombo.SelectedIndex < 0 || TargetClassCombo.SelectedIndex >= TargetCgpaValues.Length)
            {
                MessageBox.Show("Please select a target class.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double targetCgpa = TargetCgpaValues[TargetClassCombo.SelectedIndex];
            TargetResultBorder.Visibility = Visibility.Visible;

            if (ApiClient.IsOnlineMode)
            {
                // Bug Fix: Pass actual semesters from CGPA panel for more accurate result
                var sems = new List<object>();
                foreach (Border row in CgpaSemestersPanel.Children)
                {
                    var grid = (Grid)row.Child;
                    TextBox? nameBox = null, gpaBox = null, creditsBox = null;
                    foreach (UIElement child in grid.Children)
                    {
                        if (child is TextBox tb)
                        {
                            if (tb.Tag?.ToString() == "SemName") nameBox = tb;
                            else if (tb.Tag?.ToString() == "SemGpa") gpaBox = tb;
                            else if (tb.Tag?.ToString() == "SemCredits") creditsBox = tb;
                        }
                    }
                    if (double.TryParse(gpaBox?.Text, out double g) && double.TryParse(creditsBox?.Text, out double c))
                        sems.Add(new { Name = nameBox?.Text ?? "Semester", Gpa = g, Credits = c });
                }
                // If no semesters in CGPA panel, use compressed history
                if (sems.Count == 0 && creditsCompleted > 0)
                    sems.Add(new { Name = "History", Gpa = currentCgpa, Credits = creditsCompleted });

                var res = await ApiClient.PredictTargetAsync(sems, remainingCredits, targetCgpa);
                if (res != null)
                {
                    TargetResultTitle.Text = res.AlreadyAchieved ? "🎉 Already Achieved!" :
                                            res.Achievable ? $"📌 Required GPA: {res.RequiredGpa:F2}" : "⚠️ Not Achievable";
                    TargetResultTitle.Foreground = GetClassBrush(res.Achievable ? res.RequiredGpa : 0);
                    TargetResultText.Text = res.Message + (res.Achievable && !res.AlreadyAchieved
                        ? $"\n\nTo achieve your target, maintain a minimum GPA of {res.RequiredGpa:F2} over your remaining credits."
                        : "");
                    return;
                }
            }

            // Offline Fallback
            double currentQualityPoints = currentCgpa * creditsCompleted;
            double totalCredits = creditsCompleted + remainingCredits;
            double requiredQualityPoints = targetCgpa * totalCredits;
            double requiredGpa = (requiredQualityPoints - currentQualityPoints) / remainingCredits;

            if (requiredGpa > 4.0)
            {
                TargetResultTitle.Text = "⚠️ Not Achievable";
                TargetResultTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f85149"));
                TargetResultText.Text = $"You would need {requiredGpa:F2}, which exceeds the max GPA of 4.0.";
            }
            else if (requiredGpa <= 0)
            {
                TargetResultTitle.Text = "🎉 Already Achieved!";
                TargetResultTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3fb950"));
                TargetResultText.Text = "You've already met your target! Keep it up!";
            }
            else
            {
                TargetResultTitle.Text = $"📌 Required GPA: {requiredGpa:F2}";
                TargetResultTitle.Foreground = GetClassBrush(requiredGpa);
                TargetResultText.Text = $"You need a minimum GPA of {requiredGpa:F2} over your remaining {remainingCredits} credits.";
            }
        }

        // ═══════════ ACADEMIC ADVISOR ═══════════

        private async void GetAdvice_Click(object sender, RoutedEventArgs e)
        {
            // Bug Fix: Better input validation with user-friendly message
            if (!double.TryParse(AdvisorGpaInput.Text, out double gpa))
            {
                MessageBox.Show("Please enter a valid GPA (e.g. 3.50).", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (gpa < 0 || gpa > 4.0)
            {
                MessageBox.Show("GPA must be between 0.0 and 4.0.", "Invalid GPA", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AdvicePanel.Children.Clear();

            if (ApiClient.IsOnlineMode)
            {
                var res = await ApiClient.GetAdviceAsync(gpa);
                if (res != null)
                {
                    CurrentTierLabel.Text = $"Current: {res.Classification}";
                    NextTierLabel.Text = $"Target: {res.NextTier.Name} ({res.NextTier.Target:F2})";
                    NextTierProgress.Value = gpa;
                    NextTierProgress.Maximum = res.NextTier.Target;
                    AddAdviceCard("📊 AI Analysis", res.Advice, GetClassColor(gpa));
                    AdvisorTargetInfo.Text = $"Your next logical target is {res.NextTier.Name}. You need to raise your GPA to {res.NextTier.Target:F2}.";
                    return;
                }
                // Falls through to offline if API is unavailable
            }

            // Offline Fallback — Bug Fix: NextTierLabel was never set in offline mode
            string classification = GetClassification(gpa);
            string nextTierName = GetNextTierName(gpa);
            double nextTierTarget = GetNextTierTarget(gpa);

            CurrentTierLabel.Text = $"Current: {classification}";
            NextTierLabel.Text = $"Target: {nextTierName} ({nextTierTarget:F2})";
            NextTierProgress.Value = gpa;
            NextTierProgress.Maximum = nextTierTarget > gpa ? nextTierTarget : 4.0;

            AddAdviceCard("🏆 Current Standing", $"Your GPA: {gpa:F2} — {classification}", GetClassColor(gpa));
            if (gpa >= 3.70) AddAdviceCard("✅ Excellent Performance", "Maintain consistency. Aim for A+ in every subject.", "#3fb950");
            else if (gpa >= 3.30) AddAdviceCard("📈 Improvement Strategy", "Push for First Class! Target A- or above in upcoming subjects.", "#2563eb");
            else if (gpa >= 3.00) AddAdviceCard("📊 Action Plan", "Focus on B+ and above. Form study groups and attend all lectures.", "#d29922");
            else AddAdviceCard("⚠️ Urgent Improvement Needed", "Meet with your academic advisor immediately. Focus on all subjects.", "#f85149");

            AdvisorTargetInfo.Text = $"Your next target is {nextTierName}. You need to raise your GPA to {nextTierTarget:F2}.";
        }

        private void AddAdviceCard(string title, string content, string accentColor)
        {
            var card = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0d1117")),
                CornerRadius = new CornerRadius(10),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accentColor)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 12)
            };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accentColor)),
                Margin = new Thickness(0, 0, 0, 8)
            });
            stack.Children.Add(new TextBlock
            {
                Text = content,
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c9d1d9")),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 22
            });
            card.Child = stack;
            AdvicePanel.Children.Add(card);
        }

        // ═══════════ HELPERS ═══════════

        private static string GetClassification(double gpa) =>
            gpa switch { >= 3.70 => "First Class", >= 3.30 => "Second Class Upper", >= 3.00 => "Second Class Lower", >= 2.00 => "Pass", _ => "Below Pass" };

        private static SolidColorBrush GetClassBrush(double gpa) =>
            new SolidColorBrush((Color)ColorConverter.ConvertFromString(GetClassColor(gpa)));

        private static string GetClassColor(double gpa) =>
            gpa switch { >= 3.70 => "#3fb950", >= 3.30 => "#2563eb", >= 3.00 => "#d29922", _ => "#f85149" };

        // Bug Fix: Helper to get next tier name (was missing in offline advisor)
        private static string GetNextTierName(double gpa)
        {
            if (gpa >= 3.70) return "Max (4.0)";
            if (gpa >= 3.30) return "First Class";
            if (gpa >= 3.00) return "Second Class Upper";
            if (gpa >= 2.00) return "Second Class Lower";
            return "Pass";
        }

        // Bug Fix: Helper to get next tier target value (was missing in offline advisor)
        private static double GetNextTierTarget(double gpa)
        {
            if (gpa >= 3.70) return 4.0;
            if (gpa >= 3.30) return 3.70;
            if (gpa >= 3.00) return 3.30;
            if (gpa >= 2.00) return 3.00;
            return 2.00;
        }
    }
}
