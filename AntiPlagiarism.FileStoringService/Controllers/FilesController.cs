using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;

namespace AntiPlagiarism.FileStoringService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads_fss");
        private readonly ILogger<FilesController> _logger;

        public FilesController(ILogger<FilesController> logger)
        {
            _logger = logger;
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
                _logger.LogInformation("Uploads directory created at: {Path}", _storagePath);
            }
        }

        /// <summary>
        /// Uploads a .txt file.
        /// </summary>
        /// <param name="file">The .txt file to upload.</param>
        /// <returns>The ID of the stored file.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("UploadFile: File is not provided or empty.");
                return BadRequest(new { message = "File is not provided or empty." });
            }

            if (Path.GetExtension(file.FileName).ToLower() != ".txt")
            {
                _logger.LogWarning("UploadFile: Invalid file type. Only .txt files are allowed. Received: {FileName}", file.FileName);
                return BadRequest(new { message = "Only .txt files are allowed." });
            }

            var fileId = Guid.NewGuid().ToString("N") + ".txt"; 
            var filePath = Path.Combine(_storagePath, fileId);

            try
            {
                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                _logger.LogInformation("File {FileId} uploaded successfully to {FilePath}", fileId, filePath);
                return Ok(new { fileId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName}", file.FileName);
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Retrieves a stored file by its ID.
        /// </summary>
        /// <param name="fileId">The ID of the file to retrieve.</param>
        /// <returns>The file content.</returns>
        [HttpGet("{fileId}")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetFile(string fileId)
        {
            if (string.IsNullOrWhiteSpace(fileId) || fileId.Contains("..") || !fileId.EndsWith(".txt"))
            {
                _logger.LogWarning("GetFile: Invalid fileId requested: {FileId}", fileId);
                return BadRequest(new { message = "Invalid fileId." });
            }

            var filePath = Path.Combine(_storagePath, fileId);

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("GetFile: File not found with id: {FileId} at path: {FilePath}", fileId, filePath);
                return NotFound(new { message = "File not found." });
            }

            try
            {
                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                 _logger.LogInformation("File {FileId} retrieved successfully.", fileId);
                return File(fileBytes, "text/plain", fileId); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file {FileId}", fileId);
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }
    }
}
