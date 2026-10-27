using Microsoft.Extensions.VectorData;

namespace FinanceCompliance.Api.Models
{
    public class PolicyRecord
    {
        [VectorStoreKey]
        public ulong Id { get; set; }

        [VectorStoreData]
        public string Title { get; set; } = string.Empty;

        [VectorStoreData]
        public string Content { get; set; } = string.Empty;

        [VectorStoreData]
        public string Category { get; set; } = string.Empty;

        //Vector dimension (e.g., 1536 for OpenAI or embedding models)
        [VectorStoreVector(1536)]
        public ReadOnlyMemory<float>? Vector { get; set; }
    }
}