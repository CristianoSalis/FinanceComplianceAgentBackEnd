namespace FinanceCompliance.Api.Models
{
    public sealed class RagEvaluationRequest
    {
        public string Question { get; init; } = string.Empty;
        public List<RetrievedContext> Contexts { get; init; } = new();
        public string Answer { get; init; } = string.Empty;
    }
}
