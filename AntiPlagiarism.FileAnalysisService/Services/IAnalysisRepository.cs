using AntiPlagiarism.FileAnalysisService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AntiPlagiarism.FileAnalysisService.Services
{
    public interface IAnalysisRepository
    {
        Task StoreAnalysisResultAsync(AnalysisResult result);
        Task<AnalysisResult?> GetAnalysisResultAsync(string fileId);
        Task<string?> GetOriginalFileIdByHashAsync(string fileHash);
        Task AddHashToFileMappingAsync(string fileHash, string fileId);
        Task<IEnumerable<AnalysisResult>> GetAllResultsAsync();
    }
}
