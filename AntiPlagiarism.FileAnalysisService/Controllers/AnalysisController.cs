using AntiPlagiarism.FileAnalysisService.Models;
using AntiPlagiarism.FileAnalysisService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace AntiPlagiarism.FileAnalysisService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalysisController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITextAnalyzer _textAnalyzer;
        private readonly IAnalysisRepository _analysisRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AnalysisController> _logger;

        public AnalysisController(
            IHttpClientFactory httpClientFactory,
            ITextAnalyzer textAnalyzer,
            IAnalysisRepository analysisRepository,
            IConfiguration configuration,
            ILogger<AnalysisController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _textAnalyzer = textAnalyzer;
            _analysisRepository = analysisRepository;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Analyzes a file for statistics and plagiarism.
        /// </summary>
        /// <param name="fileId">The ID of the file (obtained from File Storing Service).</param>
        /// <returns>The analysis result.</returns>
        [HttpPost("{fileId}")]
        [ProducesResponseType(typeof(AnalysisResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AnalyzeFile(string fileId)
        {
            if (string.IsNullOrWhiteSpace(fileId))
            {
                return BadRequest(new { message = "FileId cannot be empty." });
            }

            var fssBaseUrl = _configuration["FileStoringService:BaseUrl"];
            if (string.IsNullOrEmpty(fssBaseUrl))
            {
                _logger.LogError("File Storing Service URL not configured.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal configuration error: File Storing Service URL is missing." });
            }

            var client = _httpClientFactory.CreateClient("FSSClient");
            string fileContent;

            try
            {
                _logger.LogInformation("Requesting file {FileId} from FSS at {FssBaseUrl}", fileId, fssBaseUrl);
                var response = await client.GetAsync($"{fssBaseUrl}/api/files/{fileId}");

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning("File {FileId} not found in File Storing Service.", fileId);
                        return NotFound(new { message = $"File with id {fileId} not found in File Storing Service." });
                    }
                    _logger.LogError("Error fetching file {FileId} from FSS. Status: {StatusCode}, Reason: {ReasonPhrase}", fileId, response.StatusCode, response.ReasonPhrase);
                    return StatusCode((int)response.StatusCode, new { message = $"Error fetching file from File Storing Service: {response.ReasonPhrase}" });
                }
                fileContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Successfully fetched file {FileId} content.", fileId);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HttpRequestException while contacting File Storing Service for file {FileId}.", fileId);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = $"File Storing Service is unavailable or encountered an error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching file {FileId}.", fileId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"An unexpected error occurred while fetching file: {ex.Message}" });
            }

            var stats = _textAnalyzer.AnalyzeText(fileContent);
            var fileHash = _textAnalyzer.CalculateSHA256(fileContent);

            var originalFileInstanceId = await _analysisRepository.GetOriginalFileIdByHashAsync(fileHash);

            bool isPlagiarized = false;
            string? sourceFileId = null;

            if (originalFileInstanceId != null)
            {
                if (originalFileInstanceId != fileId)
                {
                    isPlagiarized = true;
                    sourceFileId = originalFileInstanceId;
                    _logger.LogInformation("File {FileId} (hash: {FileHash}) is a plagiarism of {SourceFileId}.", fileId, fileHash, sourceFileId);
                }
                else
                {
                     _logger.LogInformation("File {FileId} (hash: {FileHash}) is a re-analysis of an already known original file.", fileId, fileHash);
                }
            }
            else
            {
                await _analysisRepository.AddHashToFileMappingAsync(fileHash, fileId);
                _logger.LogInformation("File {FileId} (hash: {FileHash}) is new. Added to repository.", fileId, fileHash);
            }

            var analysisResult = new AnalysisResult
            {
                FileId = fileId,
                Stats = stats,
                FileHash = fileHash,
                IsPlagiarized = isPlagiarized,
                SourceFileIdIfPlagiarized = sourceFileId
            };

            await _analysisRepository.StoreAnalysisResultAsync(analysisResult);
            _logger.LogInformation("Analysis result for {FileId} stored.", fileId);
            return Ok(analysisResult);
        }

        /// <summary>
        /// Retrieves a previously stored analysis result.
        /// </summary>
        /// <param name="fileId">The ID of the file.</param>
        /// <returns>The analysis result if found.</returns>
        [HttpGet("{fileId}")]
        [ProducesResponseType(typeof(AnalysisResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAnalysis(string fileId)
        {
            var result = await _analysisRepository.GetAnalysisResultAsync(fileId);
            if (result == null)
            {
                _logger.LogWarning("Analysis result for file {FileId} not found.", fileId);
                return NotFound(new { message = $"Analysis result for file {fileId} not found." });
            }
            _logger.LogInformation("Retrieved analysis result for file {FileId}.", fileId);
            return Ok(result);
        }
        
        /// <summary>
        /// Generates and returns a word cloud image for the specified file.
        /// </summary>
        /// <param name="fileId">The ID of the file.</param>
        /// <returns>A PNG image of the word cloud.</returns>
        [HttpGet("{fileId}/wordcloud")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetWordCloud(string fileId)
        {
             if (string.IsNullOrWhiteSpace(fileId))
            {
                return BadRequest(new { message = "FileId cannot be empty." });
            }

            var fssBaseUrl = _configuration["FileStoringService:BaseUrl"];
            var clientFSS = _httpClientFactory.CreateClient("FSSClient");
            string fileContent;
            try
            {
                _logger.LogInformation("Requesting file {FileId} from FSS for word cloud.", fileId);
                var response = await clientFSS.GetAsync($"{fssBaseUrl}/api/files/{fileId}");
                if (!response.IsSuccessStatusCode)
                {
                     if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning("File {FileId} not found in FSS for word cloud.", fileId);
                        return NotFound(new { message = $"File {fileId} not found in File Storing Service for word cloud generation." });
                    }
                    _logger.LogError("Error fetching file {FileId} from FSS for word cloud. Status: {StatusCode}", fileId, response.StatusCode);
                    return StatusCode((int)response.StatusCode, new { message = "Error fetching file from File Storing Service for word cloud."});
                }
                fileContent = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(fileContent))
                {
                    _logger.LogWarning("File {FileId} is empty, cannot generate word cloud.", fileId);
                    return BadRequest(new { message = "File content is empty, cannot generate word cloud." });
                }
                _logger.LogInformation("Successfully fetched file {FileId} content for word cloud.", fileId);
            }
            catch (HttpRequestException ex)
            {
                 _logger.LogError(ex, "FSS unavailable for word cloud generation of file {FileId}.", fileId);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = $"File Storing Service unavailable for word cloud: {ex.Message}" });
            }

            var quickChartClient = _httpClientFactory.CreateClient("QuickChartClient");
            var quickChartApiUrl = _configuration.GetValue<string>("QuickChart:ApiUrl");
            var chartRequest = new
            {
                format = _configuration.GetValue<string>("QuickChart:Format", "png"),
                width = _configuration.GetValue<int>("QuickChart:Width", 600),
                height = _configuration.GetValue<int>("QuickChart:Height", 400),
                fontScale = _configuration.GetValue<int>("QuickChart:FontScale", 15),
                scale = _configuration.GetValue<string>("QuickChart:Scale", "linear"),
                text = fileContent
            };

            try
            {
                _logger.LogInformation("Requesting word cloud from QuickChart for file {FileId}.", fileId);
                HttpResponseMessage chartResponse = await quickChartClient.PostAsJsonAsync(quickChartApiUrl, chartRequest);

                if (chartResponse.IsSuccessStatusCode)
                {
                    var imageBytes = await chartResponse.Content.ReadAsByteArrayAsync();
                    _logger.LogInformation("Successfully generated word cloud for file {FileId}.", fileId);
                    return File(imageBytes, $"image/{chartRequest.format}");
                }
                else
                {
                    var errorContent = await chartResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Error generating word cloud from QuickChart for file {FileId}. Status: {StatusCode}. Details: {ErrorContent}", fileId, chartResponse.StatusCode, errorContent);
                    return StatusCode((int)chartResponse.StatusCode, new { message = $"Error generating word cloud: {chartResponse.ReasonPhrase}. Details: {errorContent}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling QuickChart API for file {FileId}.", fileId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Error calling QuickChart API: {ex.Message}" });
            }
        }
    }
}
