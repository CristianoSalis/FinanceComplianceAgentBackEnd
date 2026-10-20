namespace FinanceCompliance.Api.Models
{
    public class ComplianceRule
    {
        public int RuleId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string RuleCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? MaxThreshold { get; set; }
        public bool IsActive { get; set; }
    }
}