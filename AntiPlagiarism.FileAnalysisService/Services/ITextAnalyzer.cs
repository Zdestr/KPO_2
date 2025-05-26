using AntiPlagiarism.FileAnalysisService.Models;

namespace AntiPlagiarism.FileAnalysisService.Services
{
    public interface ITextAnalyzer
    {
        TextStats AnalyzeText(string text);
        string CalculateSHA256(string text);
    }
}
