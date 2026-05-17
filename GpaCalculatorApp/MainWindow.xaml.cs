using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GpaCalculatorApp
{
    /// <summary>
    /// Smart Academic GPA Assistant - Main Window Logic
    /// </summary>
    public partial class MainWindow : Window
    {
        // ═══════════ GRADE SYSTEM ═══════════
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
        private Grid? _activePage;
        private Button? _activeNavBtn;

        public MainWindow()
        {
            InitializeComponent();
            _activePage = GpaPage;
            _activeNavBtn = NavGpaBtn;
            SetNavActive(NavGpaBtn);

            // Add initial rows
            AddSubjectRow();
            AddSubjectRow();
            AddSubjectRow();
            AddSemesterRow();
        }

        // ═══════════ NAVIGATION ═══════════

        private void ShowPage(Grid page, Button navBtn)
        {
            GpaPage.Visibility = Visibility.Collapsed;
            CgpaPage.Visibility = Visibility.Collapsed;
            TargetPage.Visibility = Visibility.Collapsed;
            AdvisorPage.Visibility = Visibility.Collapsed;

            page.Visibility = Visibility.Visible;
            _activePage = page;

            // Reset all nav buttons
            ResetNavButtons();
            SetNavActive(navBtn);
            _activeNavBtn = navBtn;
        }

        private void ResetNavButtons()
        {
            var secondary = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b949e"));
            NavGpaBtn.Foreground = secondary;
            NavCgpaBtn.Foreground = secondary;
            NavTargetBtn.Foreground = secondary;
            NavAdvisorBtn.Foreground = secondary;

            NavGpaBtn.Background = Brushes.Transparent;
            NavCgpaBtn.Background = Brushes.Transparent;
            NavTargetBtn.Background = Brushes.Transparent;
            NavAdvisorBtn.Background = Brushes.Transparent;
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

            // Subject Name
            var nameBox = new TextBox
            {
                Text = $"Subject {_subjectCount}",
                Margin = new Thickness(0, 0, 8, 0),
                Tag = "SubjectName"
            };
            Grid.SetColumn(nameBox, 0);
            grid.Children.Add(nameBox);

            // Credits ComboBox
            var creditsCombo = new ComboBox { Margin = new Thickness(0, 0, 8, 0), Tag = "Credits" };
            for (int i = 1; i <= 5; i++) creditsCombo.Items.Add(i);
            creditsCombo.SelectedIndex = 2; // default 3 credits
            Grid.SetColumn(creditsCombo, 1);
            grid.Children.Add(creditsCombo);

            // Grade ComboBox
            var gradeCombo = new ComboBox { Margin = new Thickness(0, 0, 8, 0), Tag = "Grade" };
            foreach (var g in Grades) gradeCombo.Items.Add(g);
            gradeCombo.SelectedIndex = 0; // default A+
            Grid.SetColumn(gradeCombo, 2);
            grid.Children.Add(gradeCombo);

            // Remove Button
            var removeBtn = new Button { Style = (Style)FindResource("DangerButton") };
            removeBtn.Click += (s, e) =>
            {
                GpaSubjectsPanel.Children.Remove(row);
            };
            Grid.SetColumn(removeBtn, 3);
            grid.Children.Add(removeBtn);

            row.Child = grid;
            GpaSubjectsPanel.Children.Add(row);
        }

        private void CalculateGpa_Click(object sender, RoutedEventArgs e)
        {
            double totalQualityPoints = 0;
            double totalCredits = 0;

            foreach (Border row in GpaSubjectsPanel.Children)
            {
                var grid = (Grid)row.Child;
                ComboBox? creditsCombo = null;
                ComboBox? gradeCombo = null;

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

                if (GradePoints.TryGetValue(grade, out double points))
                {
                    totalQualityPoints += credits * points;
                    totalCredits += credits;
                }
            }

            if (totalCredits == 0)
            {
                MessageBox.Show("Please add at least one subject with valid data.", "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double gpa = totalQualityPoints / totalCredits;
            GpaResultText.Text = gpa.ToString("F2");
            GpaClassText.Text = GetClassification(gpa);
            GpaClassText.Foreground = GetClassBrush(gpa);
            GpaResultBorder.Visibility = Visibility.Visible;
        }

        // ═══════════ CGPA CALCULATOR ═══════════

        private void AddSemester_Click(object sender, RoutedEventArgs e) => AddSemesterRow();

        private void AddSemesterRow()
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

            // Semester Name
            var nameBox = new TextBox
            {
                Text = $"Semester {_semesterCount}",
                Margin = new Thickness(0, 0, 8, 0),
                Tag = "SemName"
            };
            Grid.SetColumn(nameBox, 0);
            grid.Children.Add(nameBox);

            // GPA TextBox
            var gpaBox = new TextBox
            {
                Text = "0.00",
                Margin = new Thickness(0, 0, 8, 0),
                Tag = "SemGpa"
            };
            Grid.SetColumn(gpaBox, 1);
            grid.Children.Add(gpaBox);

            // Credits TextBox
            var creditsBox = new TextBox
            {
                Text = "15",
                Margin = new Thickness(0, 0, 8, 0),
                Tag = "SemCredits"
            };
            Grid.SetColumn(creditsBox, 2);
            grid.Children.Add(creditsBox);

            // Remove Button
            var removeBtn = new Button { Style = (Style)FindResource("DangerButton") };
            removeBtn.Click += (s, e) =>
            {
                CgpaSemestersPanel.Children.Remove(row);
            };
            Grid.SetColumn(removeBtn, 3);
            grid.Children.Add(removeBtn);

            row.Child = grid;
            CgpaSemestersPanel.Children.Add(row);
        }

        private void CalculateCgpa_Click(object sender, RoutedEventArgs e)
        {
            double totalQualityPoints = 0;
            double totalCredits = 0;

            foreach (Border row in CgpaSemestersPanel.Children)
            {
                var grid = (Grid)row.Child;
                TextBox? gpaBox = null;
                TextBox? creditsBox = null;

                foreach (UIElement child in grid.Children)
                {
                    if (child is TextBox tb)
                    {
                        if (tb.Tag?.ToString() == "SemGpa") gpaBox = tb;
                        else if (tb.Tag?.ToString() == "SemCredits") creditsBox = tb;
                    }
                }

                if (gpaBox == null || creditsBox == null) continue;

                if (!double.TryParse(gpaBox.Text, out double gpa) ||
                    !double.TryParse(creditsBox.Text, out double credits))
                {
                    MessageBox.Show("Please enter valid numbers for GPA and Credits.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (gpa < 0 || gpa > 4.0)
                {
                    MessageBox.Show("GPA must be between 0.0 and 4.0.", "Invalid GPA", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                totalQualityPoints += gpa * credits;
                totalCredits += credits;
            }

            if (totalCredits == 0)
            {
                MessageBox.Show("Please add at least one semester with valid data.", "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double cgpa = totalQualityPoints / totalCredits;
            CgpaResultText.Text = cgpa.ToString("F2");
            CgpaClassText.Text = GetClassification(cgpa);
            CgpaClassText.Foreground = GetClassBrush(cgpa);
            CgpaResultBorder.Visibility = Visibility.Visible;
        }

        // ═══════════ TARGET GPA PLANNER ═══════════

        private void CalculateTarget_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(TargetCurrentCgpa.Text, out double currentCgpa) ||
                !double.TryParse(TargetCreditsCompleted.Text, out double creditsCompleted) ||
                !double.TryParse(TargetRemainingCredits.Text, out double remainingCredits))
            {
                MessageBox.Show("Please fill in all fields with valid numbers.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (currentCgpa < 0 || currentCgpa > 4.0)
            {
                MessageBox.Show("Current CGPA must be between 0.0 and 4.0.", "Invalid CGPA", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (remainingCredits <= 0)
            {
                MessageBox.Show("Remaining credits must be greater than 0.", "Invalid Credits", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double targetCgpa = TargetCgpaValues[TargetClassCombo.SelectedIndex];
            double currentQualityPoints = currentCgpa * creditsCompleted;
            double totalCredits = creditsCompleted + remainingCredits;
            double requiredQualityPoints = targetCgpa * totalCredits;
            double requiredGpa = (requiredQualityPoints - currentQualityPoints) / remainingCredits;

            TargetResultBorder.Visibility = Visibility.Visible;

            if (requiredGpa > 4.0)
            {
                TargetResultTitle.Text = "⚠️ Not Achievable";
                TargetResultTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f85149"));
                TargetResultText.Text = $"You would need a GPA of {requiredGpa:F2}, which exceeds the maximum 4.0.\n" +
                    "Consider extending your studies or focusing on achieving the best possible grades.";
            }
            else if (requiredGpa <= 0)
            {
                TargetResultTitle.Text = "🎉 Already Achieved!";
                TargetResultTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3fb950"));
                TargetResultText.Text = "Congratulations! You've already met or exceeded your target CGPA. Keep up the great work!";
            }
            else
            {
                string difficulty = requiredGpa >= 3.7 ? "This is challenging — aim for A and A+ grades."
                    : requiredGpa >= 3.3 ? "Very achievable — maintain A- and above."
                    : requiredGpa >= 3.0 ? "Easily achievable — keep consistent effort."
                    : "Very comfortable — just stay on track.";

                TargetResultTitle.Text = $"📌 Required GPA: {requiredGpa:F2}";
                TargetResultTitle.Foreground = GetClassBrush(requiredGpa);
                TargetResultText.Text = $"To achieve {GetClassification(targetCgpa)}, you need a minimum GPA of {requiredGpa:F2} " +
                    $"over your remaining {remainingCredits:F0} credits.\n\n{difficulty}";
            }
        }

        // ═══════════ ACADEMIC ADVISOR ═══════════

        private void GetAdvice_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(AdvisorGpaInput.Text, out double gpa))
            {
                MessageBox.Show("Please enter a valid GPA (e.g., 3.25).", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (gpa < 0 || gpa > 4.0)
            {
                MessageBox.Show("GPA must be between 0.0 and 4.0.", "Invalid GPA", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AdvicePanel.Children.Clear();

            string classification = GetClassification(gpa);
            AddAdviceCard("🏆 Current Standing", $"Your GPA: {gpa:F2} — {classification}", GetClassColor(gpa));

            if (gpa >= 3.70)
            {
                AddAdviceCard("✅ Excellent Performance",
                    "You're on track for First Class! Maintain consistency.\n• Keep aiming for A+ and A grades\n• Help peers — teaching reinforces learning\n• Consider research or honors projects", "#3fb950");
            }
            else if (gpa >= 3.30)
            {
                AddAdviceCard("📈 Improvement Strategy",
                    "You're in Second Upper range. Push for First Class!\n• Target A- or above in high-credit subjects\n• Identify weak subjects and get extra help\n• You need about 3.7+ GPA going forward", "#2563eb");
            }
            else if (gpa >= 3.00)
            {
                AddAdviceCard("📊 Action Plan",
                    "You're in Second Lower range. Here's how to improve:\n• Focus on B+ and above in every subject\n• Prioritize high-credit courses — they impact GPA more\n• Form study groups and attend all lectures\n• Review past exam papers regularly", "#d29922");
            }
            else if (gpa >= 2.00)
            {
                AddAdviceCard("⚠️ Urgent Improvement Needed",
                    "Your GPA needs significant improvement.\n• Meet with academic advisors immediately\n• Focus on passing all subjects — avoid F grades\n• Aim for at least B- in every course\n• Consider reducing course load if possible\n• Use university tutoring services", "#f85149");
            }
            else
            {
                AddAdviceCard("🚨 Critical Situation",
                    "Your academic standing is at risk.\n• Seek immediate academic counseling\n• Consider retaking failed courses\n• Focus entirely on passing all current subjects\n• Explore academic support programs\n• Evaluate study habits and time management", "#f85149");
            }

            AddAdviceCard("💡 General Tips",
                "• Attend every class — participation matters\n• Start assignments early, avoid last-minute rush\n• Use active recall and spaced repetition for studying\n• Sleep well before exams — rest improves performance\n• Break study sessions into 25-min focused blocks", "#7c3aed");
        }

        private void AddAdviceCard(string title, string content, string accentColor)
        {
            var card = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0d1117")),
                CornerRadius = new CornerRadius(10),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accentColor)),
                BorderThickness = new Thickness(1, 1, 1, 1),
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

        private static string GetClassification(double gpa)
        {
            return gpa switch
            {
                >= 3.70 => "First Class",
                >= 3.30 => "Second Class Upper",
                >= 3.00 => "Second Class Lower",
                >= 2.00 => "Pass",
                _ => "Below Pass"
            };
        }

        private static SolidColorBrush GetClassBrush(double gpa)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(GetClassColor(gpa)));
        }

        private static string GetClassColor(double gpa)
        {
            return gpa switch
            {
                >= 3.70 => "#3fb950",
                >= 3.30 => "#2563eb",
                >= 3.00 => "#d29922",
                _ => "#f85149"
            };
        }
    }
}
