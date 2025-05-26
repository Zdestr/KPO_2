namespace AntiPlagiarism.FileAnalysisService.Models
{
    public class AnalysisResult
    {
        public string FileId { get; set; }
        public TextStats Stats { get; set; }
        public string FileHash { get; set; }
        public bool IsPlagiarized { get; set; }
        public string? SourceFileIdIfPlagiarized { get; set; }
    }
}
