using FinanceCompliance.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FinanceCompliance.Api.Services
{
    public class ComplianceAiService
    {
        private readonly Kernel _kernel;
        private readonly ComplianceDbContext _context;
        private readonly ILogger<ComplianceAiService> _logger;
        private readonly string _modelId;
        private readonly string _provider;

        public ComplianceAiService(IConfiguration configuration, ComplianceDbContext context, ILogger<ComplianceAiService> logger)
        {
            _context = context;
            _logger = logger;

            var builder = Kernel.CreateBuilder();
            
            //Active Provider
            _provider = configuration["AiSettings:Provider"] ?? "Ollama";

            if (_provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
            {
                var endpoint = configuration["AiSettings:Ollama:Endpoint"] ?? "http://localhost:11434";
                _modelId = configuration["AiSettings:Ollama:ModelId"] ?? "llama3";

                //Ollama exposes an OpenAI-compatible API on port 11434
                builder.AddOpenAIChatCompletion(
                    modelId: _modelId,
                    apiKey: "ollama-dummy-key", //Ollama does not require an actual API key
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

        public async Task<string> AnalyzeTransactionsAsync()
        {
            var rules = await _context.ComplianceRules.Where(r => r.IsActive).ToListAsync();
            var pendingTransactions = await _context.FinancialTransactions.Where(t => t.Status == "Pending").ToListAsync();

            var rulesText = string.Join("\n", rules.Select(r => $"- [{r.RuleCode}] {r.Description} (Soglia: {r.MaxThreshold})"));
            var transactionsText = string.Join("\n", pendingTransactions.Select(t => 
                $"- ID: {t.TransactionId}, Codice: {t.TransactionCode}, Cliente: {t.CustomerName}, Rischio: {t.CustomerRiskRating}, Importo: {t.Amount} {t.Currency}, Paese: {t.DestinationCountry}"));

            var prompt = $@"
                            Sei un Agente AI esperto in Compliance Finanziaria e Antiriciclaggio (AML).
                            Analizza le seguenti transazioni in base alle regole aziendali fornite.
                            
                            REGOLE AZIENDALI:
                            {rulesText}
                            
                            TRANSAZIONI DA ANALIZZARE:
                            {transactionsText}
                            
                            Per ogni transazione, verifica se viola una regola. Restituisci un'analisi strutturata indicando chiaramente se la transazione è 'Conforme' o 'Anomala/Da Bloccare' e spiega la motivazione in italiano.
                            ";

            //Printing useful information to Console
            Console.WriteLine("==================================================");
            Console.WriteLine($"[AI DEBUG] Provider attivo: {_provider}");
            Console.WriteLine($"[AI DEBUG] Modello LLM in uso: {_modelId}");
            Console.WriteLine($"[AI DEBUG] Regole caricate da SQL Server: {rules.Count}");
            Console.WriteLine($"[AI DEBUG] Transazioni caricate da SQL Server: {pendingTransactions.Count}");
            Console.WriteLine("------------------ PROMPT INVIATO ------------------");
            Console.WriteLine(prompt);
            Console.WriteLine("==================================================");


            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            
            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(prompt);

            var result = await chatService.GetChatMessageContentAsync(chatHistory);

            return result.Content ?? "Nessuna risposta generata dall'agente.";
        }
    }
}