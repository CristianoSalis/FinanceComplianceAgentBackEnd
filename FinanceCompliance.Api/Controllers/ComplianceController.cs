using Microsoft.AspNetCore.Mvc;
using FinanceCompliance.Api.Services;

namespace FinanceCompliance.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComplianceController : ControllerBase
    {
        private readonly ComplianceAiService _aiService;

        public ComplianceController(ComplianceAiService aiService)
        {
            _aiService = aiService;
        }

        // POST: api/compliance/analyze
        [HttpPost("analyze")]
        public async Task<IActionResult> RunComplianceAnalysis()
        {
            try
            {
                var analysisResult = await _aiService.AnalyzeTransactionsAsync();
                return Ok(new { success = true, analysis = analysisResult });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Errore durante l'analisi IA", error = ex.Message });
            }
        }
    }
}