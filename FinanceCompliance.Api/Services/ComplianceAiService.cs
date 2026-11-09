using FinanceCompliance.Api.Data;
using FinanceCompliance.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace FinanceCompliance.Api.Services
{
    public class ComplianceAiService
    {
        private readonly Kernel _kernel;
        private readonly ComplianceDbContext _context;
        private readonly PolicyVectorService _vectorService;
        private readonly ILogger<ComplianceAiService> _logger;
        private readonly string _modelId;
        private readonly string _provider;

           public ComplianceAiService(
            IConfiguration configuration,
            ComplianceDbContext context,
            PolicyVectorService vectorService,
            ILogger<ComplianceAiService> logger)
        {
            _context = context;
            _vectorService = vectorService;
            _logger = logger;

            var builder = Kernel.CreateBuilder();

            _provider = configuration["AiSettings:Provider"] ?? "Ollama";

            if (_provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
            {
                var endpoint = configuration["AiSettings:Ollama:Endpoint"] ?? "http://localhost:11434";
                _modelId = configuration["AiSettings:Ollama:ModelId"] ?? "llama3";

                builder.AddOpenAIChatCompletion(
                    modelId: _modelId,
                    apiKey: "ollama-dummy-key",
                    endpoint: new Uri($"{endpoint}/v1")
                );
            }
            else
            {
                var endpoint = configuration["AiSettings:OpenAi:Endpoint"] ?? "https://api.openai.com/v1";
                var apiKey = configuration["AiSettings:OpenAi:ApiKey"] ?? throw new InvalidOperationException("API Key mancante per OpenAI");
                _modelId = configuration["AiSettings:OpenAi:ModelId"] ?? "gpt-4o-mini";

                builder.AddOpenAIChatCompletion(
                    modelId: _modelId,
                    apiKey: apiKey,
                    endpoint: new Uri(endpoint)
                );
            }

            _kernel = builder.Build();
        }

        public async Task<RagPipelineResult> AnalyzeTransactionsAsync()
        {
            //SQL Server
            var rules = await _context.ComplianceRules.Where(r => r.IsActive).ToListAsync();
            var pendingTransactions = await _context.FinancialTransactions.Where(t => t.Status == "Pending").ToListAsync();

            // RAG: Qdrant for policy
            const string question = "Verifica transazioni sospette, limiti e giurisdizioni estere";
            var relevantPolicies = await _vectorService.SearchRelevantPoliciesAsync(question);
            var policiesText = string.Join("\n", relevantPolicies.Select((p, index) =>
                $"[CONTEXT-{index + 1} | file={p.FileName} | chunk={p.ChunkIndex} | score={p.Score:F3}]\n{p.Content}"));

            var rulesText = string.Join("\n", rules.Select(r => $"- [{r.RuleCode}] {r.Description} (Soglia: {r.MaxThreshold})"));
            var transactionsText = string.Join("\n", pendingTransactions.Select(t =>
                $"- ID: {t.TransactionId}, Codice: {t.TransactionCode}, Cliente: {t.CustomerName}, Rischio: {t.CustomerRiskRating}, Importo: {t.Amount} {t.Currency}, Paese: {t.DestinationCountry}"));

            //SQL Server + RAG (Qdrant)
            var prompt = $@"
Sei un Agente AI esperto in Compliance Finanziaria e Antiriciclaggio (AML).
Analizza le seguenti transazioni combinando le policy di sicurezza recuperate tramite ricerca vettoriale e le regole aziendali.

POLICY DI COMPLIANCE (Qdrant Vector Store - RAG):
{policiesText}

REGOLE AZIENDALI (SQL Server):
{rulesText}

TRANSAZIONI DA ANALIZZARE:
{transactionsText}

             Per ogni transazione, verifica se viola una regola o una policy. Restituisci esclusivamente JSON valido, senza markdown e senza testo aggiuntivo, con questa struttura:
             {{
               ""transactions"": [
                 {{
                   ""transactionId"": 123,
                   ""classification"": ""Conforme oppure Anomala/Da Bloccare"",
                   ""reason"": ""Motivazione in italiano"",
                   ""citations"": [
                     {{
                       ""contextId"": ""CONTEXT-1"",
                       ""quote"": ""Citazione testuale breve presa dal contesto""
                     }}
                   ]
                 }}
               ]
             }}
             Usa esclusivamente gli identificativi CONTEXT-n presenti nel contesto. Se non hai elementi sufficienti, dichiaralo nella motivazione e non inventare citazioni.
";

            // Logs
            Console.WriteLine("==================================================");
            Console.WriteLine($"[AI DEBUG] Provider attivo: {_provider}");
            Console.WriteLine($"[AI DEBUG] Modello LLM in uso: {_modelId}");
            Console.WriteLine($"[AI DEBUG] Regole SQL caricate: {rules.Count}");
            Console.WriteLine($"[AI DEBUG] Policy RAG caricate da Qdrant: {relevantPolicies.Count}");
            Console.WriteLine($"[AI DEBUG] Transazioni caricate: {pendingTransactions.Count}");
            Console.WriteLine("------------------ PROMPT RAG INVIATO ------------------");
            Console.WriteLine(prompt);
            Console.WriteLine("==================================================");

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();

            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(prompt);

            //OLLAMA (POST /v1/chat/completions sulla porta 11434)
            var result = await chatService.GetChatMessageContentAsync(chatHistory);

            var answer = result.Content ?? "Nessuna risposta generata dall'agente.";

            ComplianceAnalysisResult analysis;
            try
            {
                var json = ExtractJson(answer);
                var parsedAnalysis = JsonSerializer.Deserialize<ComplianceAnalysisResult>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                analysis = parsedAnalysis is null
                    ? CreateUnstructuredAnalysis(answer)
                    : new ComplianceAnalysisResult
                    {
                        Transactions = parsedAnalysis.Transactions,
                        IsStructured = true
                    };
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "La risposta del modello non è un JSON valido.");
                analysis = CreateUnstructuredAnalysis(answer);
            }

            return new RagPipelineResult
            {
                Question = question,
                Contexts = relevantPolicies,
                Analysis = analysis,
                Answer = answer
            };
        }

        private static ComplianceAnalysisResult CreateUnstructuredAnalysis(string answer)
        {
            return new ComplianceAnalysisResult
            {
                IsStructured = false,
                RawResponse = answer
            };
        }

        private static string ExtractJson(string responseText)
        {
            var json = responseText.Trim();
            if (json.StartsWith("```", StringComparison.Ordinal))
            {
                var firstLineEnd = json.IndexOf('\n');
                var lastFence = json.LastIndexOf("```");
                if (firstLineEnd >= 0 && lastFence > firstLineEnd)
                {
                    json = json[(firstLineEnd + 1)..lastFence].Trim();
                }
            }

            return json;
        }
    }
}