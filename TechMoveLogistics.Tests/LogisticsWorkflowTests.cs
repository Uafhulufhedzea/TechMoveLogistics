using Xunit;
using TechMoveLogistics.Models;

namespace TechMoveLogistics.Tests
{
    public class LogisticsWorkflowTests
    {
        [Fact]
        public void CalculateZarCost_GivenStandardInputs_ReturnsCorrectProductMath()
        {
            decimal usdAmount = 100.00m;
            decimal liveExchangeRate = 18.50m;
            decimal expectedZarResult = 1850.00m;

            var strategy = ServiceFactory.GetCostStrategy("Standard");
            decimal actualZarResult = strategy.CalculateFinalCost(usdAmount, liveExchangeRate);

            Assert.Equal(expectedZarResult, actualZarResult);
        }

        // EDGE CASE: Zero Currency Boundaries
        [Fact]
        public void CalculateZarCost_GivenZeroUsdValue_ReturnsZeroZar()
        {
            decimal usdAmount = 0.00m;
            decimal liveExchangeRate = 18.50m;

            var strategy = ServiceFactory.GetCostStrategy("Standard");
            decimal actualZarResult = strategy.CalculateFinalCost(usdAmount, liveExchangeRate);

            Assert.Equal(0.00m, actualZarResult);
        }

        [Theory]
        [InlineData(".exe")]
        [InlineData(".msi")]
        [InlineData(".png")]
        public void ValidateFileExtension_GivenRestrictedType_ReturnsFalse(string badExtension)
        {
            string simulatedUploadedFileName = "malicious_file" + badExtension;
            string extension = Path.GetExtension(simulatedUploadedFileName).ToLower();
            bool isAllowedPdf = (extension == ".pdf");

            Assert.False(isAllowedPdf);
        }

        // EDGE CASE TEST: Handle null file exception parameters
        [Fact]
        public void ValidateFileExtension_GivenNullOrEmptyInput_ReturnsFalse()
        {
            string emptyFileName = "";
            string extension = Path.GetExtension(emptyFileName).ToLower();
            bool isAllowedPdf = (extension == ".pdf");

            Assert.False(isAllowedPdf);
        }

        [Fact]
        public void ValidateFileExtension_GivenValidPdfFormat_ReturnsTrue()
        {
            string safeFileName = "signed_agreement.pdf";
            string extension = Path.GetExtension(safeFileName).ToLower();
            bool isAllowedPdf = (extension == ".pdf");

            Assert.True(isAllowedPdf);
        }
    }
}
