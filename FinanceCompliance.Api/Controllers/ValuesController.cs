using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinanceCompliance.Api.Data;
using FinanceCompliance.Api.Models;

namespace FinanceCompliance.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly ComplianceDbContext _context;

        public TransactionsController(ComplianceDbContext context)
        {
            _context = context;
        }

        // GET: api/transactions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FinancialTransaction>>> GetTransactions()
        {
            try
            {
                var transactions = await _context.FinancialTransactions.ToListAsync();
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Errore durante la lettura dal database", error = ex.Message });
            }
        }

        // GET: api/transactions/pending
        [HttpGet("pending")]
        public async Task<ActionResult<IEnumerable<FinancialTransaction>>> GetPendingTransactions()
        {
            var pending = await _context.FinancialTransactions
                .Where(t => t.Status == "Pending")
                .ToListAsync();

            return Ok(pending);
        }
    }
}