using AntiPlagiarism.FileAnalysisService.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AntiPlagiarism.FileAnalysisService.Services
{
    public class InMemoryAnalysisRepository : IAnalysisRepository
    {
        private readonly ConcurrentDictionary<string, AnalysisResult> _analysisResults = new();
        private readonly ConcurrentDictionary<string, string> _fileHashesToOriginalFileId = new();

        public Task StoreAnalysisResultAsync(AnalysisResult result)
        {
            _analysisResults[result.FileId] = result;
            return Task.CompletedTask;
        }

        public Task<AnalysisResult?> GetAnalysisResultAsync(string fileId)
        {
            _analysisResults.TryGetValue(fileId, out var result);
            return Task.FromResult(result);
        }

        public Task<string?> GetOriginalFileIdByHashAsync(string fileHash)
        {
            _fileHashesToOriginalFileId.TryGetValue(fileHash, out var originalFileId);
            return Task.FromResult(originalFileId);
        }

        public Task AddHashToFileMappingAsync(string fileHash, string fileId)
        {
            _fileHashesToOriginalFileId.TryAdd(fileHash, fileId);
            return Task.CompletedTask;
        }
        
        public Task<IEnumerable<AnalysisResult>> GetAllResultsAsync()
        {
            return Task.FromResult(_analysisResults.Values.AsEnumerable());
        }
    }
}
