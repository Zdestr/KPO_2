using Xunit;
using AntiPlagiarism.FileAnalysisService.Services;
using AntiPlagiarism.FileAnalysisService.Models;

namespace AntiPlagiarism.FileAnalysisService.Tests
{
    public class TextAnalyzerTests
    {
        private readonly ITextAnalyzer _analyzer = new TextAnalyzer();

        [Fact]
        public void AnalyzeText_EmptyText_ReturnsZeroCounts()
        {
            var result = _analyzer.AnalyzeText(string.Empty);
            Assert.Equal(0, result.ParagraphCount);
            Assert.Equal(0, result.WordCount);
            Assert.Equal(0, result.CharacterCount);
        }

        [Fact]
        public void AnalyzeText_SingleWord_CorrectCounts()
        {
            var result = _analyzer.AnalyzeText("Hello");
            Assert.Equal(1, result.ParagraphCount);
            Assert.Equal(1, result.WordCount);
            Assert.Equal(5, result.CharacterCount);
        }

        [Fact]
        public void AnalyzeText_SimpleSentence_CorrectCounts()
        {
            string text = "This is a test.";
            var result = _analyzer.AnalyzeText(text);
            Assert.Equal(1, result.ParagraphCount); // Single block of text is one paragraph
            Assert.Equal(4, result.WordCount);
            Assert.Equal(text.Length, result.CharacterCount);
        }

        [Fact]
        public void AnalyzeText_MultipleParagraphs_CorrectCounts()
        {
            string text = "First paragraph.\n\nSecond paragraph, with some words.\n\nThird one.";
            var result = _analyzer.AnalyzeText(text);
            Assert.Equal(3, result.ParagraphCount);
            Assert.Equal(10, result.WordCount);
            Assert.Equal(text.Length, result.CharacterCount);
        }
        
        [Fact]
        public void AnalyzeText_TextWithOnlySpacesAndNewlines_CorrectCounts()
        {
            string text = "  \n\n   \n ";
            var result = _analyzer.AnalyzeText(text);
            Assert.Equal(0, result.ParagraphCount); // No actual content paragraphs
            Assert.Equal(0, result.WordCount);
            Assert.Equal(text.Length, result.CharacterCount);
        }


        [Fact]
        public void CalculateSHA256_KnownText_ReturnsCorrectHash()
        {
            // Hash for "hello"
            string expectedHash = "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824";
            var hash = _analyzer.CalculateSHA256("hello");
            Assert.Equal(expectedHash, hash);
        }

        [Fact]
        public void CalculateSHA256_EmptyText_ReturnsEmptyString()
        {
             var hash = _analyzer.CalculateSHA256(string.Empty);
            Assert.Equal(string.Empty, hash);
        }
    }
}
