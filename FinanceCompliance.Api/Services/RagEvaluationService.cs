using FinanceCompliance.Api.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using FinanceCompliance.Api.Models;
using System.Text.Json;

namespace FinanceCompliance.Api.Services
{
    public sealed class RagEvaluationService
    {
        private readonly Kernel _kernel;
        private readonly ILogger<RagEvaluationService> _logger;

        public RagEvaluationService(
            IConfiguration configuration,
            ILogger<RagEvaluationService> logger)
        {
            _logger = logger;

            var builder = Kernel.CreateBuilder();
            var provider = configuration["AiSettings:Provider"] ?? "Ollama";

            if (provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
            {
                var endpoint = configuration["AiSettings:Ollama:Endpoint"] ?? "http://localhost:11434";
                var modelId = configuration["AiSettings:Ollama:ModelId"] ?? "llama3";

                builder.AddOpenAIChatCompletion(
                    modelId: modelId,
                    apiKey: "ollama-dummy-key",
                    endpoint: new Uri($"{endpoint}/v1"));
            }
            else
            {
                var endpoint = configuration["AiSettings:OpenAi:Endpoint"] ?? "https://api.openai.com/v1";
                var apiKey = configuration["AiSettings:OpenAi:ApiKey"]
                    ?? throw new InvalidOperationException("API Key mancante per OpenAI");
                var modelId = configuration["AiSettings:OpenAi:ModelId"] ?? "gpt-4o-mini";

                builder.AddOpenAIChatCompletion(
                    modelId: modelId,
                    apiKey: apiKey,
                    endpoint: new Uri(endpoint));
            }

            _kernel = builder.Build();
        }

        public async Task<RagEvaluationResult> EvaluateAsync(RagEvaluationRequest request)
        {
            var contextsText = string.Join(
                "\n\n",
                request.Contexts.Select((context, index) =>
                    $"[CONTESTO-{index + 1} | file={context.FileName} | chunk={context.ChunkIndex} | score={context.Score:F3}]\n{context.Content}"));

            var prompt = $@"
Sei un valutatore di sistemi RAG per la compliance finanziaria.
Valuta esclusivamente la pertinenza del contesto recuperato rispetto alla domanda.

DOMANDA:
{request.Question}

CONTESTO RECUPERATO:
{contextsText}

RISPOSTA GENERATA:
{request.Answer}

Assegna un punteggio da 0 a 1:
- 0 significa che il contesto non è pertinente;
- 1 significa che il contesto è direttamente pertinente e sufficiente.

Restituisci esclusivamente JSON valido, senza markdown o testo aggiuntivo:
{{
  ""score"": 0.0,
  ""explanation"": ""spiegazione sintetica in italiano""
}}";

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(prompt);

            var result = await chatService.GetChatMessageContentAsync(chatHistory);
            var responseText = result.Content;

            if (string.IsNullOrWhiteSpace(responseText))
            {
                return CreateFailedResult("Il modello giudice non ha restituito una risposta.");
            }

            try
            {
                var evaluation = JsonSerializer.Deserialize<ContextRelevanceEvaluation>(
                    ExtractJson(responseText),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (evaluation is null)
                {
                    return CreateFailedResult("Il modello giudice ha restituito un risultato vuoto.");
                }

                var contextRelevance = Math.Clamp(evaluation.Score, 0, 1);
                var groundedness = await EvaluateGroundednessAsync(request, contextsText);
                var answerRelevance = await EvaluateAnswerRelevanceAsync(request);
                var overallScore = contextRelevance * 0.3
                    + groundedness.Score * 0.4
                    + answerRelevance.Score * 0.3;

                return new RagEvaluationResult
                {
                    ContextRelevance = contextRelevance,
                    Groundedness = groundedness.Score,
                    AnswerRelevance = answerRelevance.Score,
                    OverallScore = overallScore,
                    Passed = contextRelevance >= 0.75
                        && groundedness.Score >= 0.8
                        && answerRelevance.Score >= 0.75,
                    Explanation = $"Context relevance: {evaluation.Explanation} Groundedness: {groundedness.Explanation} Answer relevance: {answerRelevance.Explanation}",
                    UnsupportedClaims = groundedness.UnsupportedClaims
                };
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "La risposta del valutatore non è un JSON valido.");
                return CreateFailedResult("La risposta del modello giudice non è un JSON valido.");
            }
        }

        private async Task<AnswerRelevanceEvaluation> EvaluateAnswerRelevanceAsync(
            RagEvaluationRequest request)
        {
            var prompt = $@"
Sei un valutatore di sistemi RAG per la compliance finanziaria.
Valuta se la risposta risponde direttamente alla domanda originale.
Considera completezza, chiarezza e aderenza alla richiesta.

DOMANDA:
{request.Question}

RISPOSTA GENERATA:
{request.Answer}

Assegna un punteggio da 0 a 1:
- 0 significa che la risposta non risponde alla domanda;
- 1 significa che la risposta risponde completamente e direttamente alla domanda.

Restituisci esclusivamente JSON valido, senza markdown o testo aggiuntivo:
{{
  ""score"": 0.0,
  ""explanation"": ""spiegazione sintetica in italiano""
}}";

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(prompt);

            var result = await chatService.GetChatMessageContentAsync(chatHistory);
            var responseText = result.Content;

            if (string.IsNullOrWhiteSpace(responseText))
            {
                return new AnswerRelevanceEvaluation
                {
                    Explanation = "Il modello giudice non ha restituito una valutazione di answer relevance."
                };
            }

            try
            {
                var evaluation = JsonSerializer.Deserialize<AnswerRelevanceEvaluation>(
                    ExtractJson(responseText),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (evaluation is null)
                {
                    return new AnswerRelevanceEvaluation
                    {
                        Explanation = "Il modello giudice ha restituito un risultato vuoto."
                    };
                }

                return new AnswerRelevanceEvaluation
                {
                    Score = Math.Clamp(evaluation.Score, 0, 1),
                    Explanation = evaluation.Explanation
                };
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "La risposta di answer relevance non è un JSON valido.");
                return new AnswerRelevanceEvaluation
                {
                    Explanation = "La risposta di answer relevance non è un JSON valido."
                };
            }
        }

        private async Task<GroundednessEvaluation> EvaluateGroundednessAsync(
            RagEvaluationRequest request,
            string contextsText)
        {
            var prompt = $@"
Sei un valutatore di sistemi RAG per la compliance finanziaria.
Verifica se la risposta è supportata dal contesto recuperato.
Non considerare corretta un'affermazione che non può essere dimostrata dal contesto.

CONTESTO RECUPERATO:
{contextsText}

RISPOSTA GENERATA:
{request.Answer}

Assegna un punteggio da 0 a 1:
- 0 significa che la risposta non è supportata dal contesto;
- 1 significa che tutte le affermazioni rilevanti sono supportate dal contesto.

Restituisci esclusivamente JSON valido, senza markdown o testo aggiuntivo:
{{
  ""score"": 0.0,
  ""unsupportedClaims"": [],
  ""explanation"": ""spiegazione sintetica in italiano""
}}
Se non trovi affermazioni non supportate, restituisci un array vuoto.
Non restituire mai testo di esempio o placeholder.
";

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(prompt);

            var result = await chatService.GetChatMessageContentAsync(chatHistory);
            var responseText = result.Content;

            if (string.IsNullOrWhiteSpace(responseText))
            {
                return new GroundednessEvaluation
                {
                    Explanation = "Il modello giudice non ha restituito una valutazione di groundedness."
                };
            }

            try
            {
                var evaluation = JsonSerializer.Deserialize<GroundednessEvaluation>(
                    ExtractJson(responseText),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (evaluation is null)
                {
                    return new GroundednessEvaluation
                    {
                        Explanation = "Il modello giudice ha restituito un risultato vuoto."
                    };
                }

                return new GroundednessEvaluation
                {
                    Score = Math.Clamp(evaluation.Score, 0, 1),
                    UnsupportedClaims = evaluation.UnsupportedClaims,
                    Explanation = evaluation.Explanation
                };
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "La risposta di groundedness non è un JSON valido.");
                return new GroundednessEvaluation
                {
                    Explanation = "La risposta di groundedness non è un JSON valido."
                };
            }
        }

        private static RagEvaluationResult CreateFailedResult(string explanation)
        {
            return new RagEvaluationResult
            {
                ContextRelevance = 0,
                OverallScore = 0,
                Passed = false,
                Explanation = explanation
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

        private sealed class ContextRelevanceEvaluation
        {
            public double Score { get; init; }
            public string Explanation { get; init; } = string.Empty;
        }

        private sealed class GroundednessEvaluation
        {
            public double Score { get; init; }
            public List<string> UnsupportedClaims { get; init; } = new();
            public string Explanation { get; init; } = string.Empty;
        }

        private sealed class AnswerRelevanceEvaluation
        {
            public double Score { get; init; }
            public string Explanation { get; init; } = string.Empty;
        }
    }
}
