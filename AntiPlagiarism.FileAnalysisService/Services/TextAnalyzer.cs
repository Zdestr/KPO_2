using AntiPlagiarism.FileAnalysisService.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace AntiPlagiarism.FileAnalysisService.Services
{
    public class TextAnalyzer : ITextAnalyzer
    {
        public TextStats AnalyzeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new TextStats { ParagraphCount = 0, WordCount = 0, CharacterCount = 0 };

            var normalizedText = text.Replace("\r\n", "\n").Replace("\r", "\n");
            
            var paragraphs = Regex.Split(normalizedText, @"\n\s*\n")
                                  .Where(p => !string.IsNullOrWhiteSpace(p))
                                  .ToArray();

            char[] delimiters = new char[] { ' ', '\r', '\n', '\t', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '"', '\'' };
            var words = text.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

            return new TextStats
            {
                ParagraphCount = paragraphs.Length > 0 ? paragraphs.Length : (string.IsNullOrWhiteSpace(text) ? 0 : 1), 
                WordCount = words.Length,
                CharacterCount = text.Length
            };
        }

        public string CalculateSHA256(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(text));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
