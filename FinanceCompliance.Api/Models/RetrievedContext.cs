namespace FinanceCompliance.Api.Models
{
    public sealed class RetrievedContext
    {
        public string Content { get; init; } = string.Empty;
        public string FileName { get; init; } = string.Empty;
        public int ChunkIndex { get; init; }
        public double Score { get; init; }
    }
}
