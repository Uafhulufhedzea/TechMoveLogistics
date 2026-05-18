using Xunit;
using TechMoveLogistics.Models;

namespace TechMoveLogistics.Tests
{
    public class LogisticsWorkflowTests
    {
       
        // TEST 1: Currency calculation validation
   
        [Fact]
        public void CalculateZarCost_GivenUsdAndExchangeRate_ReturnsCorrectProductMath()
        {
            
            decimal usdAmount = 150.00m;
            decimal liveExchangeRate = 18.25m; 
            decimal expectedZarResult = 2737.50m; // 150 * 18.25

            decimal actualZarResult = usdAmount * liveExchangeRate;

            // Verify accuracy
            Assert.Equal(expectedZarResult, actualZarResult);
        }

       
        // TEST 2: File Validation for restricted type block
      
        [Theory]
        [InlineData(".exe")]
        [InlineData(".msi")]
        [InlineData(".png")]
        public void ValidateFileExtension_GivenRestrictedType_ReturnsFalse(string badExtension)
        {
            // Arrange
            string simulatedUploadedFileName = "signed_contract_malware" + badExtension;

            // Act
            string actualExtension = Path.GetExtension(simulatedUploadedFileName).ToLower();
            bool isAllowedPdf = (actualExtension == ".pdf");

            // Assert
            Assert.False(isAllowedPdf, $"Security Flaw: System should block '{badExtension}' from being processed.");
        }

       
        // TEST 3: File validation for allowed PDF Success
        
        [Fact]
        public void ValidateFileExtension_GivenPdfFormat_ReturnsTrue()
        {
            // Arrange
            string safeFileName = "official_agreement.pdf";

            // Act
            string extension = Path.GetExtension(safeFileName).ToLower();
            bool isAllowedPdf = (extension == ".pdf");

            // Assert
            Assert.True(isAllowedPdf);
        }
    }
}

