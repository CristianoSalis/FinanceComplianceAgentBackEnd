using System;

namespace FinanceCompliance.Api.Models
{
    public class FinancialTransaction
    {
        public int TransactionId { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerRiskRating { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string DestinationCountry { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}