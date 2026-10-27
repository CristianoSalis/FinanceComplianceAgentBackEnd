using FinanceCompliance.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace FinanceCompliance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly PolicyVectorService _vectorService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(PolicyVectorService vectorService, IWebHostEnvironment env, ILogger<DocumentsController> logger)
        {
            _vectorService = vectorService;
            _env = env;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "Nessun file valido è stato allegato." });
            }

            //Check if is a PDF
            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "È consentito caricare esclusivamente file in formato PDF." });
            }

            try
            {
                //Path to PoliciesPdf
                var folderPath = Path.Combine(_env.ContentRootPath, "PoliciesPdf");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var filePath = Path.Combine(folderPath, file.FileName);

                //Saving file on server
                using (var stream = new StreamWriter(filePath)) 
                {
                    // Standard asynchronous saving
                }

                // Using the correct FileStream:
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation($"[UPLOAD] File '{file.FileName}' caricato con successo.");

                // Optional: Immediately start ingestion on Qdrant for the newly uploaded document
                await _vectorService.ProcessAndStorePdfAsync(filePath);

                return Ok(new
                {
                    success = true,
                    message = $"Il file '{file.FileName}' è stato caricato e indicizzato con successo nel sistema RAG.",
                    fileName = file.FileName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UPLOAD ERROR] Errore durante il caricamento del PDF.");
                return StatusCode(500, new { success = false, message = "Errore interno durante l'elaborazione del file.", error = ex.Message });
            }
        }
    }
}