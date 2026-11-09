using Microsoft.AspNetCore.Mvc;
using FinanceCompliance.Api.Models;
using FinanceCompliance.Api.Services;

namespace FinanceCompliance.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComplianceController : ControllerBase
    {
        private readonly ComplianceAiService _aiService;
        private readonly RagEvaluationService _evaluationService;

        public ComplianceController(
            ComplianceAiService aiService,
            RagEvaluationService evaluationService)
        {
            _aiService = aiService;
            _evaluationService = evaluationService;
        }

        // POST: api/compliance/analyze
        [HttpPost("analyze")]
        public async Task<IActionResult> RunComplianceAnalysis([FromQuery] bool evaluate = false)
        {
            try
            {
                var pipelineResult = await _aiService.AnalyzeTransactionsAsync();
                RagEvaluationResult? evaluation = null;

                if (evaluate)
                {
                    evaluation = await _evaluationService.EvaluateAsync(new RagEvaluationRequest
                    {
                        Question = pipelineResult.Question,
                        Contexts = pipelineResult.Contexts,
                        Answer = pipelineResult.Answer
                    });
                }

                return Ok(new
                {
                    success = true,
                    analysis = pipelineResult,
                    evaluation
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Errore durante l'analisi IA", error = ex.Message });
            }
        }
    }
}