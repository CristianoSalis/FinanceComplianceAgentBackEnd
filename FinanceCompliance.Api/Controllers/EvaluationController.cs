using FinanceCompliance.Api.Models;
using FinanceCompliance.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceCompliance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class EvaluationController : ControllerBase
    {
        private readonly RagEvaluationService _evaluationService;
        private readonly ILogger<EvaluationController> _logger;

        public EvaluationController(
            RagEvaluationService evaluationService,
            ILogger<EvaluationController> logger)
        {
            _evaluationService = evaluationService;
            _logger = logger;
        }

        [HttpPost("context-relevance")]
        public async Task<IActionResult> EvaluateContextRelevance([FromBody] RagEvaluationRequest request)
        {
            if (request is null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Il corpo della richiesta è obbligatorio."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Il campo Question è obbligatorio."
                });
            }

            if (request.Contexts is null || request.Contexts.Count == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "È necessario fornire almeno un elemento in Contexts."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Answer))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Il campo Answer è obbligatorio."
                });
            }

            try
            {
                var result = await _evaluationService.EvaluateAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la valutazione della context relevance.");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Errore durante la valutazione del contesto."
                });
            }
        }
    }
}
