using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace _4Books.Services
{
    public interface IBookSummaryService
    {
        Task<string> GenerateSummary(string title, string[] authors, string[] categories);

        // Nuovo metodo per l'analisi dettagliata
        Task<BookAnalysis> GenerateBookAnalysis(string title, string[] authors, string[] categories, string description, string publisher, string publishedDate);
    }

    public class GeminiSummaryService : IBookSummaryService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string ApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";

        public GeminiSummaryService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _apiKey = configuration["GeminiApi:ApiKey"]
                ?? throw new ArgumentNullException("Gemini API key non configurata");
            _httpClient = httpClientFactory.CreateClient("GeminiAPI");
        }

        public async Task<string> GenerateSummary(string title, string[] authors, string[] categories)
        {
            // Costruisci un prompt incentrato sulla trama
            var authorsText = authors != null && authors.Length > 0
                ? string.Join(", ", authors)
                : "autore sconosciuto";

            var categoriesText = categories != null && categories.Length > 0
                ? string.Join(", ", categories)
                : "";

            var promptText = $@"Crea un riassunto coinvolgente della trama o un'ipotetica storia del libro con le seguenti caratteristiche:
            - Titolo: {title}
            - Autori: {authorsText}
            - Categorie: {categoriesText}

            Scrivi una breve trama che catturi l'essenza di questo libro, in italiano, come se dovessi presentarlo a un lettore.
            Il riassunto dovrebbe essere di circa 3-4 frasi, avvincente e dovrebbe far capire di cosa tratta il libro.
            Basati sul titolo e sulle categorie per immaginare la possibile storia.
            Inizia direttamente con il riassunto della trama.";

            try
            {
                var requestBody = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = promptText } } }
                    },
                    generationConfig = new
                    {
                        temperature = 0.8,
                        maxOutputTokens = 400
                    }
                };

                var requestJson = JsonSerializer.Serialize(requestBody);
                var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

                // URL completo con API key
                var url = $"{ApiBaseUrl}?key={_apiKey}";

                var response = await _httpClient.PostAsync(url, requestContent);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>();

                // Estrai il testo dalla risposta JSON
                var generatedText = responseJson
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return generatedText ?? "Trama non disponibile.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nella generazione del riassunto: {ex.Message}");
                return "Un'avvincente storia che attende solo di essere scoperta dal lettore.";
            }
        }
        // Nuovo metodo per generare un'analisi dettagliata del libro
        public async Task<BookAnalysis> GenerateBookAnalysis(string title, string[] authors, string[] categories,
                                                           string description, string publisher, string publishedDate)
        {
            var authorsText = authors != null && authors.Length > 0
                ? string.Join(", ", authors)
                : "autore sconosciuto";

            var categoriesText = categories != null && categories.Length > 0
                ? string.Join(", ", categories)
                : "categorie non specificate";

            // Costruisci un prompt per l'analisi completa
            var promptText = $@"Analizza il seguente libro:
Titolo: {title}
Autori: {authorsText}
Categorie: {categoriesText}
Descrizione: {description ?? "Non disponibile"}
Editore: {publisher ?? "Non specificato"}
Data pubblicazione: {publishedDate ?? "Non specificata"}

Rispondi SOLO con un oggetto JSON contenente i seguenti campi:
- plot: una trama dettagliata del libro
- characters: un array di oggetti con i personaggi principali e le loro caratteristiche (ogni oggetto deve avere i campi 'name' e 'description')
- themes: un array di stringhe con i temi principali del libro
- audience: una descrizione del pubblico ideale per questo libro
- similarBooks: un array di stringhe con titoli di libri simili consigliati

Basa la tua analisi sul titolo, sugli autori, sulle categorie e sulla descrizione fornita. Se non hai informazioni sufficienti su un aspetto specifico, fai delle ipotesi ragionevoli.";

            try
            {
                var requestBody = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = promptText } } }
                    },
                    generationConfig = new
                    {
                        temperature = 0.2,
                        maxOutputTokens = 1024,
                        responseMimeType = "application/json"
                    }
                };

                var requestJson = JsonSerializer.Serialize(requestBody);
                var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

                // URL completo con API key
                var url = $"{ApiBaseUrl}?key={_apiKey}";

                var response = await _httpClient.PostAsync(url, requestContent);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>();

                // Estrai il testo dalla risposta JSON
                var generatedText = responseJson
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrEmpty(generatedText))
                {
                    return CreateDefaultBookAnalysis();
                }

                return ParseBookAnalysis(generatedText);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'analisi del libro: {ex.Message}");
                return CreateDefaultBookAnalysis();
            }
        }

        private BookAnalysis ParseBookAnalysis(string jsonText)
        {
            try
            {
                // Tenta di trovare e analizzare il JSON nella risposta
                int startPos = jsonText.IndexOf("{");
                int endPos = jsonText.LastIndexOf("}");

                if (startPos >= 0 && endPos > startPos)
                {
                    jsonText = jsonText.Substring(startPos, endPos - startPos + 1);
                }

                // Deserializza la risposta
                return JsonSerializer.Deserialize<BookAnalysis>(jsonText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? CreateDefaultBookAnalysis();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nel parsing della risposta: {ex.Message}");
                return CreateDefaultBookAnalysis();
            }
        }

        private BookAnalysis CreateDefaultBookAnalysis()
        {
            return new BookAnalysis
            {
                Plot = "Trama non disponibile.",
                Characters = new[]
                {
                    new BookCharacter { Name = "Informazione non disponibile", Description = "" }
                },
                Themes = new[] { "Informazione non disponibile" },
                Audience = "Informazione non disponibile",
                SimilarBooks = new[] { "Informazione non disponibile" }
            };
        }
    }

    // Classi per rappresentare l'analisi del libro
    public class BookAnalysis
    {
        public string Plot { get; set; }
        public BookCharacter[] Characters { get; set; }
        public string[] Themes { get; set; }
        public string Audience { get; set; }
        public string[] SimilarBooks { get; set; }
    }

    public class BookCharacter
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
