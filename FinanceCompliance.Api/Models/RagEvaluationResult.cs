namespace FinanceCompliance.Api.Models
{
    public sealed class RagEvaluationResult
    {
        public double ContextRelevance { get; init; }
        public double Groundedness { get; init; }
        public double AnswerRelevance { get; init; }
        public double OverallScore { get; init; }
        public bool Passed { get; init; }
        public string Explanation { get; init; } = string.Empty;
        public List<string> UnsupportedClaims { get; init; } = new();
    }
}
