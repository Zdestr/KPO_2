using Xunit; 
using Moq;
using AntiPlagiarism.FileAnalysisService.Controllers;
using AntiPlagiarism.FileAnalysisService.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration; 
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AntiPlagiarism.FileAnalysisService.Models;
using System.Net;
using System.Collections.Generic; 
using Moq.Protected; 
using System.Threading; 

namespace AntiPlagiarism.FileAnalysisService.Tests
{
    public class AnalysisControllerTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<ITextAnalyzer> _mockTextAnalyzer;
        private readonly Mock<IAnalysisRepository> _mockAnalysisRepository;
        private readonly Mock<ILogger<AnalysisController>> _mockLogger;
        private readonly IConfiguration _configuration;
        private readonly AnalysisController _controller;

        public AnalysisControllerTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockTextAnalyzer = new Mock<ITextAnalyzer>();
            _mockAnalysisRepository = new Mock<IAnalysisRepository>();
            _mockLogger = new Mock<ILogger<AnalysisController>>();

            var inMemorySettings = new Dictionary<string, string?> {
                {"FileStoringService:BaseUrl", "http://fake-fss-url.com"},
                {"QuickChart:ApiUrl", "http://fake-quickchart-url.com"}
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();
            
            _controller = new AnalysisController(
                _mockHttpClientFactory.Object,
                _mockTextAnalyzer.Object,
                _mockAnalysisRepository.Object,
                _configuration, 
                _mockLogger.Object);
        }

        private Mock<HttpMessageHandler> SetupHttpClientMock(HttpResponseMessage responseMessage)
        {
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
            return mockHttpMessageHandler;
        }

        [Fact]
        public async Task AnalyzeFile_FssReturnsNotFound_ReturnsNotFoundResult()
        {
            // Arrange
            var fileId = "nonexistentfile.txt";
            SetupHttpClientMock(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("") 
            });

            // Act
            var result = await _controller.AnalyzeFile(fileId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var responseValue = (dynamic)notFoundResult.Value!;
            string message = responseValue.GetType().GetProperty("message").GetValue(responseValue, null).ToString();
            Assert.Contains(fileId, message);
        }
        
        [Fact]
        public async Task AnalyzeFile_ValidNewFile_ReturnsOkWithAnalysisResult()
        {
            // Arrange
            var fileId = "newfile.txt";
            var fileContent = "This is new content.";
            var fileHash = "newhash123";
            var textStats = new TextStats { WordCount = 4, CharacterCount = fileContent.Length, ParagraphCount = 1 };

            SetupHttpClientMock(new HttpResponseMessage {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(fileContent)
            });

            _mockTextAnalyzer.Setup(x => x.AnalyzeText(fileContent)).Returns(textStats);
            _mockTextAnalyzer.Setup(x => x.CalculateSHA256(fileContent)).Returns(fileHash);
            _mockAnalysisRepository.Setup(x => x.GetOriginalFileIdByHashAsync(fileHash)).ReturnsAsync((string?)null); 

            // Act
            var result = await _controller.AnalyzeFile(fileId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var analysisResult = Assert.IsType<AnalysisResult>(okResult.Value);
            Assert.Equal(fileId, analysisResult.FileId);
            Assert.Equal(fileHash, analysisResult.FileHash);
            Assert.False(analysisResult.IsPlagiarized);
            _mockAnalysisRepository.Verify(x => x.AddHashToFileMappingAsync(fileHash, fileId), Times.Once);
            _mockAnalysisRepository.Verify(x => x.StoreAnalysisResultAsync(It.Is<AnalysisResult>(ar => ar.FileId == fileId)), Times.Once);
        }
    }
}
