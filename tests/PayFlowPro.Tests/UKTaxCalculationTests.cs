using PayFlowPro.Core.Services;
using PayFlowPro.Models.Enums;

namespace PayFlowPro.Tests
{
    public class UKTaxCalculationTests
    {
        [Fact]
        public void Test_UKTaxCalculation_For_30000_Annual_Salary()
        {
            // Arrange
            var ukTaxService = new UKTaxCalculationService();
            decimal annualSalary = 30000m; // £30,000 annual salary

            // Act
            var result = ukTaxService.CalculateUKTaxDeductions(annualSalary, "1257L", PayFrequency.Monthly);

            // Assert
            // For £30,000 annual salary:
            // Personal allowance: £12,570 (2024-25)
            // Taxable income: £30,000 - £12,570 = £17,430
            // Income tax (20%): £17,430 * 0.20 = £3,486 annually
            // Monthly income tax: £3,486 / 12 = £290.50

            // National Insurance:
            // Primary threshold: £12,570 annually
            // NI on (£30,000 - £12,570) = £17,430 * 12% = £2,091.60 annually
            // Monthly NI: £2,091.60 / 12 = £174.30

            // Expected values (with some tolerance for rounding)
            Assert.True(Math.Abs(result.IncomeTax - 290.50m) < 1m, $"Income tax should be approximately £290.50, got £{result.IncomeTax}");
            Assert.True(Math.Abs(result.NationalInsurance - 174.30m) < 1m, $"National Insurance should be approximately £174.30, got £{result.NationalInsurance}");

            // Verify monthly gross salary
            Assert.Equal(2500m, result.GrossSalaryForPeriod); // £30,000 / 12

            // Verify net salary calculation
            var expectedNetSalary = 2500m - result.IncomeTax - result.NationalInsurance;
            Assert.Equal(expectedNetSalary, result.NetSalary);
        }

        [Fact]
        public void Test_UKTaxCalculation_For_50000_Annual_Salary()
        {
            // Arrange
            var ukTaxService = new UKTaxCalculationService();
            decimal annualSalary = 50000m; // £50,000 annual salary

            // Act
            var result = ukTaxService.CalculateUKTaxDeductions(annualSalary, "1257L", PayFrequency.Monthly);

            // Assert
            // For £50,000 annual salary:
            // Taxable income: £50,000 - £12,570 = £37,430
            // Income tax: £37,430 * 0.20 = £7,486 annually
            // Monthly income tax: £7,486 / 12 = £624.00

            // National Insurance:
            // NI on (£50,000 - £12,570) = £37,430 * 12% = £4,491.60 annually
            // Monthly NI: £4,491.60 / 12 = £374.30

            Assert.True(Math.Abs(result.IncomeTax - 624.00m) < 1m, $"Income tax should be approximately £624.00, got £{result.IncomeTax}");
            Assert.True(Math.Abs(result.NationalInsurance - 374.30m) < 1m, $"National Insurance should be approximately £374.30, got £{result.NationalInsurance}");
        }

        [Fact]
        public void Test_PersonalAllowance_Tapering_For_High_Salary()
        {
            // Arrange
            var ukTaxService = new UKTaxCalculationService();
            decimal annualSalary = 120000m; // £120,000 annual salary (above £100,000 tapering threshold)

            // Act
            var result = ukTaxService.CalculateUKTaxDeductions(annualSalary, "1257L", PayFrequency.Monthly);

            // Assert
            // Personal allowance should be tapered
            // Tapering starts at £100,000: (£120,000 - £100,000) / 2 = £10,000 reduction
            // Tapered allowance: £12,570 - £10,000 = £2,570
            Assert.Equal(2570m, result.PersonalAllowance);
        }
    }
}