using System.Text.Json.Serialization;

namespace FinanceCompliance.Api.Models
{
    public sealed class ComplianceAnalysisResult
    {
        public List<TransactionAnalysisResult> Transactions { get; init; } = new();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RawResponse { get; init; }

        public bool IsStructured { get; init; }
    }

    public sealed class TransactionAnalysisResult
    {
        public int TransactionId { get; init; }
        public string Classification { get; init; } = string.Empty;
        public string Reason { get; init; } = string.Empty;
        public List<ContextCitation> Citations { get; init; } = new();
    }

    public sealed class ContextCitation
    {
        public string ContextId { get; init; } = string.Empty;
        public string Quote { get; init; } = string.Empty;
    }
}
