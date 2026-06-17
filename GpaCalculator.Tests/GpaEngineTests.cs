using System;
using Xunit;
using GpaCalculatorApp;

namespace GpaCalculator.Tests
{
    public class GpaEngineTests
    {
        [Theory]
        [InlineData(4.0, "First Class")]
        [InlineData(3.7, "First Class")]
        [InlineData(3.69, "Second Class Upper")]
        [InlineData(3.3, "Second Class Upper")]
        [InlineData(3.29, "Second Class Lower")]
        [InlineData(3.0, "Second Class Lower")]
        [InlineData(2.99, "Pass")]
        [InlineData(2.0, "Pass")]
        [InlineData(1.99, "Below Pass")]
        [InlineData(0.0, "Below Pass")]
        public void GetClassification_ReturnsCorrectClass(double gpa, string expectedClass)
        {
            Assert.Equal(expectedClass, GpaEngine.GetClassification(gpa));
        }

        [Fact]
        public void CalculateGpa_ReturnsCorrectGpa()
        {
            // A+ (4.0 * 3) + B (2.7 * 3) = 12.0 + 8.1 = 20.1
            // 20.1 / 6 = 3.35
            double gpa = GpaEngine.CalculateGpa(20.1, 6);
            Assert.Equal(3.35, gpa, 2);
        }

        [Fact]
        public void CalculateGpa_ZeroCredits_ReturnsZero()
        {
            Assert.Equal(0, GpaEngine.CalculateGpa(10, 0));
        }

        [Fact]
        public void CalculateRequiredGpa_ReturnsCorrectGpa()
        {
            // Current CGPA = 3.0, Credits = 60 (QP = 180)
            // Target CGPA = 3.5, Remaining Credits = 60
            // Required QP = 3.5 * 120 = 420
            // Need (420 - 180) = 240 QP over 60 credits => 4.0 GPA
            double required = GpaEngine.CalculateRequiredGpa(3.0, 60, 3.5, 60);
            Assert.Equal(4.0, required, 2);
        }

        [Fact]
        public void CalculateRequiredGpa_ZeroRemaining_ReturnsZero()
        {
            Assert.Equal(0, GpaEngine.CalculateRequiredGpa(3.0, 60, 3.5, 0));
        }

        [Fact]
        public void CalculateWhatIfCgpa_ReturnsCorrectCgpa()
        {
            // Current = 3.0 over 60 (180 QP)
            // Future = 4.0 over 30 (120 QP)
            // Total = 300 QP / 90 Credits = 3.333...
            double whatIf = GpaEngine.CalculateWhatIfCgpa(3.0, 60, 4.0, 30);
            Assert.Equal(3.33, whatIf, 2);
        }

        [Fact]
        public void GetDaysUntil_ReturnsCorrectDays()
        {
            var today = new DateTime(2026, 6, 15);
            var tomorrow = new DateTime(2026, 6, 16);
            var nextWeek = new DateTime(2026, 6, 22);

            Assert.Equal(1, GpaEngine.GetDaysUntil(tomorrow, today));
            Assert.Equal(7, GpaEngine.GetDaysUntil(nextWeek, today));
            Assert.Equal(-1, GpaEngine.GetDaysUntil(today, tomorrow));
        }
    }
}
