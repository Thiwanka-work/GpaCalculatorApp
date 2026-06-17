using System;

namespace GpaCalculatorApp
{
    public static class GpaEngine
    {
        public static string GetClassification(double gpa) => gpa switch
        {
            >= 3.70 => "First Class",
            >= 3.30 => "Second Class Upper",
            >= 3.00 => "Second Class Lower",
            >= 2.00 => "Pass",
            _       => "Below Pass"
        };

        public static double CalculateGpa(double totalQualityPoints, double totalCredits)
        {
            if (totalCredits <= 0) return 0;
            return totalQualityPoints / totalCredits;
        }

        public static double CalculateRequiredGpa(double currentCgpa, double creditsCompleted, double targetCgpa, double remainingCredits)
        {
            if (remainingCredits <= 0) return 0;
            double currentQP = currentCgpa * creditsCompleted;
            double requiredQP = targetCgpa * (creditsCompleted + remainingCredits);
            return (requiredQP - currentQP) / remainingCredits;
        }

        public static double CalculateWhatIfCgpa(double currentCgpa, double creditsCompleted, double futureGpa, double futureCredits)
        {
            double totalCredits = creditsCompleted + futureCredits;
            if (totalCredits <= 0) return 0;
            double currentQP = currentCgpa * creditsCompleted;
            double futureQP = futureGpa * futureCredits;
            return (currentQP + futureQP) / totalCredits;
        }

        public static int GetDaysUntil(DateTime targetDate, DateTime fromDate)
        {
            return (int)Math.Floor((targetDate.Date - fromDate.Date).TotalDays);
        }
    }
}
