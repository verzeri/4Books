using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using _4Books.Pages.Models;
using _4Books.Services;
using System.Security.Claims;
using System.Linq;
using System.Text.RegularExpressions;

namespace _4Books.Pages.Book
{
    public class ResultsModel : PageModel
    {
        private readonly BookApiService _bookApiService;
        private readonly FavoriteService _favoriteService;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public ResultsModel(BookApiService bookApiService, FavoriteService favoriteService,
                            IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _bookApiService = bookApiService;
            _favoriteService = favoriteService;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            SearchResult = new BookSearchResult { TotalItems = 0, Books = new List<Pages.Models.Book>() };
            DebugInfo = string.Empty;
        }

        public string SearchTitle { get; set; }
        public BookSearchResult SearchResult { get; set; }
        public string DebugInfo { get; set; }

        // Dictionary per memorizzare quali libri sono nei preferiti
        public Dictionary<string, bool> FavoriteStatus { get; set; } = new Dictionary<string, bool>();

        public async Task<IActionResult> OnGetAsync(string title, int maxResults = 10)
        {
            DebugInfo = $"Metodo OnGetAsync chiamato con parametro title: {title}\n";

            if (string.IsNullOrWhiteSpace(title))
            {
                DebugInfo += "Il titolo � vuoto, reindirizzamento alla pagina Index\n";
                return RedirectToPage("./Index");
            }

            SearchTitle = title;
            DebugInfo += $"Titolo di ricerca impostato a: {SearchTitle}\n";

            try
            {
                DebugInfo += $"Chiamata al servizio BookApiService per cercare fino a {maxResults} libri...\n";
                SearchResult = await _bookApiService.SearchBooksByTitle(title, maxResults);
                DebugInfo += $"Ricerca completata. Trovati {SearchResult.TotalItems} risultati\n";

                if (SearchResult.Books != null)
                {
                    DebugInfo += $"Numero di libri restituiti: {SearchResult.Books.Count}\n";

                    // Se l'utente � autenticato, verifica quali libri sono nei preferiti
                    if (User.Identity.IsAuthenticated)
                    {
                        int userId = int.Parse(User.FindFirstValue("UserId"));

                        foreach (var book in SearchResult.Books)
                        {
                            FavoriteStatus[book.Id] = await _favoriteService.IsInFavoritesAsync(userId, book.Id);
                        }
                    }

                    // Rimossa la generazione automatica delle trame AI
                }
                else
                {
                    DebugInfo += "La lista dei libri � null\n";
                    SearchResult.Books = new List<Pages.Models.Book>();
                }
            }
            catch (Exception ex)
            {
                DebugInfo += $"ERRORE durante la ricerca: {ex.Message}\n";
                DebugInfo += $"Stack trace: {ex.StackTrace}\n";

                SearchResult = new BookSearchResult { TotalItems = 0, Books = new List<Pages.Models.Book>() };
            }

            return Page();
        }

        // Metodo per generare le informazioni AI sul libro tramite Gemini
        public async Task<JsonResult> OnPostGenerateBookInfoAsync(string bookId, string title)
        {
            try
            {
                Console.WriteLine($"GenerateBookInfo richiamato per bookId: {bookId}, title: {title}");

                // Recupera i dettagli del libro
                var book = await _bookApiService.GetBookByIdAsync(bookId);

                if (book == null)
                {
                    Console.WriteLine("Libro non trovato");
                    return new JsonResult(new { success = false, error = "Libro non trovato" }) { StatusCode = 404 };
                }

                Console.WriteLine($"Libro trovato: {book.Title} di {(book.Authors?.Count > 0 ? string.Join(", ", book.Authors) : "autore sconosciuto")}");

                // Assicurati che ci siano dettagli sufficienti
                if (string.IsNullOrEmpty(book.Description))
                {
                    Console.WriteLine("Descrizione mancante, aggiungo una generica");
                    book.Description = $"Un libro intitolato {book.Title}" +
                        (book.Authors?.Any() == true ? $" scritto da {string.Join(", ", book.Authors)}" : "") +
                        (book.Categories?.Any() == true ? $" nel genere {string.Join(", ", book.Categories)}" : "");
                }

                // Genera le informazioni del libro con Gemini
                var bookInfo = await GenerateBookInfoWithGeminiAsync(book);

                Console.WriteLine("Book info generata, ritorno risposta");

                return new JsonResult(new
                {
                    success = true,
                    info = bookInfo,
                    bookDetails = new
                    { // Aggiungi dettagli del libro per debug
                        title = book.Title,
                        authors = book.Authors,
                        categories = book.Categories,
                        description = book.Description?.Substring(0, Math.Min(100, book.Description?.Length ?? 0)) + "...",
                        hasDescription = !string.IsNullOrEmpty(book.Description)
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore in OnPostGenerateBookInfo: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return new JsonResult(new
                {
                    success = false,
                    error = $"Errore durante l'analisi del libro: {ex.Message}"
                })
                { StatusCode = 500 };
            }
        }

        // Metodo che chiama l'API di Gemini per generare informazioni dettagliate sul libro
        private async Task<BookAIInfo> GenerateBookInfoWithGeminiAsync(Pages.Models.Book book)
        {
            var apiKey = _configuration["GeminiApi:ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("API key di Gemini non configurata");
                return GetDefaultBookInfo(book.Title);
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(60); // Timeout più lungo

                // Prepara un prompt più efficace per ottenere informazioni sul libro
                var promptText = $@"Analizza in dettaglio il libro ""{book.Title}"" 
        {(book.Authors != null && book.Authors.Count > 0 ? $"di {string.Join(", ", book.Authors)}" : "")}.
        
        Informazioni disponibili sul libro:
        Titolo: {book.Title}
        Autori: {(book.Authors != null && book.Authors.Count > 0 ? string.Join(", ", book.Authors) : "sconosciuto")}
        Categorie/Generi: {(book.Categories != null && book.Categories.Count > 0 ? string.Join(", ", book.Categories) : "sconosciuto")}
        Anno pubblicazione: {book.PublishedDate ?? "sconosciuto"}
        Descrizione: {book.Description ?? "non disponibile"}
        
        Per favore, analizza questo libro e fornisci:
        1. Una trama dettagliata (3-5 frasi)
        2. I personaggi principali e le loro caratteristiche
        3. I temi fondamentali del libro
        4. Il pubblico target ideale
        5. Libri simili consigliati
        
        Se non conosci alcuni dettagli specifici, fai delle ipotesi plausibili basate sul titolo e genere, ma cerca di essere accurato con i dettagli che conosci.";

                // Definisci lo schema JSON per l'output strutturato
                var responseSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        plot = new { type = "string", description = "Trama del libro in 3-5 frasi" },
                        characters = new
                        {
                            type = "array",
                            description = "I personaggi principali e le loro caratteristiche",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    name = new { type = "string", description = "Nome del personaggio" },
                                    description = new { type = "string", description = "Descrizione del personaggio" }
                                },
                                required = new[] { "name", "description" }
                            }
                        },
                        themes = new
                        {
                            type = "array",
                            description = "I temi fondamentali del libro",
                            items = new { type = "string" }
                        },
                        audience = new { type = "string", description = "Pubblico target ideale" },
                        similarBooks = new
                        {
                            type = "array",
                            description = "Libri simili consigliati",
                            items = new { type = "string" }
                        }
                    },
                    required = new[] { "plot", "characters", "themes", "audience", "similarBooks" }
                };

                var requestBody = new
                {
                    contents = new[]
                    {
                                new { parts = new[] { new { text = promptText } } }
                            },
                    generationConfig = new
                    {
                        temperature = 0.2,
                        max_output_tokens = 2048, // Changed to snake_case
                        top_p = 1,                // Changed to snake_case
                        top_k = 32,                // Changed to snake_case
                        response_mime_type = "application/json",
                        response_schema = responseSchema
                    }
                };

                var requestContent = new StringContent(JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

                var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key={apiKey}"; // o gemini-2.0-flash

                Console.WriteLine($"[DEBUG] Invio richiesta a Gemini per: {book.Title} con output strutturato.");

                var response = await httpClient.PostAsync(apiUrl, requestContent);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[DEBUG] Risposta da Gemini: {(jsonResponse.Length > 200 ? jsonResponse.Substring(0, 200) + "..." : jsonResponse)}");

                if (response.IsSuccessStatusCode)
                {
                    var responseDoc = JsonDocument.Parse(jsonResponse);
                    string generatedText = null;

                    try
                    {
                        // Con l'output JSON strutturato, il testo JSON è direttamente in parts[0].text
                        generatedText = responseDoc.RootElement
                            .GetProperty("candidates")[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Errore estraendo testo JSON dalla risposta: {ex.Message}");
                        // Verifica se c'è un blocco di errore nella risposta
                        if (responseDoc.RootElement.TryGetProperty("promptFeedback", out var feedback) &&
                            feedback.TryGetProperty("blockReason", out var reason))
                        {
                            Console.WriteLine($"[ERROR] Prompt bloccato, motivo: {reason.GetString()}");
                        }
                        return GetDefaultBookInfo(book.Title);
                    }


                    if (string.IsNullOrEmpty(generatedText))
                    {
                        Console.WriteLine("[ERROR] Testo JSON generato vuoto.");
                        return GetDefaultBookInfo(book.Title);
                    }

                    Console.WriteLine($"[DEBUG] JSON generato: {(generatedText.Length > 100 ? generatedText.Substring(0, 100) + "..." : generatedText)}");

                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            AllowTrailingCommas = true,
                            ReadCommentHandling = JsonCommentHandling.Skip
                        };

                        var bookInfo = JsonSerializer.Deserialize<BookAIInfo>(generatedText, options);

                        if (bookInfo != null)
                        {
                            // Assicuriamoci che tutte le proprietà abbiano valori validi
                            if (string.IsNullOrEmpty(bookInfo.Plot))
                                bookInfo.Plot = $"La trama di '{book.Title}' esplora temi profondi attraverso personaggi ben sviluppati.";

                            if (bookInfo.Characters == null || !bookInfo.Characters.Any())
                                bookInfo.Characters = new List<BookCharacter> {
                                            new BookCharacter {
                                                Name = "Protagonista",
                                                Description = "Personaggio principale della storia"
                                            }
                                        };
                            else
                            {
                                bookInfo.Characters.ForEach(c =>
                                {
                                    if (string.IsNullOrEmpty(c.Name)) c.Name = "Personaggio";
                                    if (string.IsNullOrEmpty(c.Description)) c.Description = "Descrizione non fornita.";
                                });
                            }


                            if (bookInfo.Themes == null || !bookInfo.Themes.Any())
                                bookInfo.Themes = new List<string> { "Sviluppo personale", "Relazioni" };

                            if (string.IsNullOrEmpty(bookInfo.Audience))
                                bookInfo.Audience = "Lettori interessati a questo genere letterario";

                            if (bookInfo.SimilarBooks == null || !bookInfo.SimilarBooks.Any())
                                bookInfo.SimilarBooks = new List<string> { "Altri libri dello stesso autore" };

                            Console.WriteLine("[SUCCESS] Informazioni libro generate correttamente da JSON strutturato");
                            return bookInfo;
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"[ERROR] Errore deserializzazione JSON strutturato: {ex.Message}");
                        Console.WriteLine($"[DEBUG] JSON che ha causato l'errore: {generatedText}");
                    }
                }
                else
                {
                    Console.WriteLine($"[ERROR] API ha restituito errore: {response.StatusCode}");
                    Console.WriteLine($"[ERROR] Dettaglio: {jsonResponse}");
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"[ERROR] Eccezione HTTP durante chiamata API: {httpEx.Message}");
                if (httpEx.InnerException != null)
                {
                    Console.WriteLine($"[ERROR] Inner Exception: {httpEx.InnerException.Message}");
                }
                Console.WriteLine($"[ERROR] StackTrace: {httpEx.StackTrace}");
            }
            catch (TaskCanceledException tex)
            {
                Console.WriteLine($"[ERROR] Timeout durante chiamata API: {tex.Message}");
                Console.WriteLine($"[ERROR] StackTrace: {tex.StackTrace}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Eccezione generica durante chiamata API: {ex.Message}");
                Console.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
            }

            // Se arriviamo qui, qualcosa è andato storto
            return GetDefaultBookInfo(book.Title);
        }

        // Modello fallback per le informazioni del libro
        private BookAIInfo GetDefaultBookInfo(string title)
        {
            return new BookAIInfo
            {
                Plot = $"La trama di '{title}' segue personaggi avvincenti attraverso un viaggio emozionante pieno di colpi di scena. I protagonisti affrontano sfide che li trasformeranno profondamente, in un racconto che esplora la condizione umana.",
                Characters = new List<BookCharacter>
                {
                    new BookCharacter {
                        Name = "Protagonista",
                        Description = "Il personaggio principale che guida la narrazione attraverso la storia"
                    },
                    new BookCharacter {
                        Name = "Personaggio di supporto",
                        Description = "Un alleato o compagno del protagonista che offre supporto e nuove prospettive"
                    }
                },
                Themes = new List<string> { "Crescita personale", "Relazioni interpersonali", "Scoperta", "Avventura" },
                Audience = "Lettori interessati a storie coinvolgenti con personaggi ben sviluppati",
                SimilarBooks = new List<string> { "Opere di autori dello stesso genere", "Classici dello stesso tema", "Titoli recenti con temi simili" }
            };
        }

        // Metodo per gestire l'aggiunta/rimozione dai preferiti
        public async Task<IActionResult> OnPostToggleFavoriteAsync(string bookId, string title)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login", new { returnUrl = Url.Page("./Results", new { title }) });
            }

            int userId = int.Parse(User.FindFirstValue("UserId"));

            // Verifica se il libro � gi� nei preferiti
            bool isInFavorites = await _favoriteService.IsInFavoritesAsync(userId, bookId);

            if (isInFavorites)
            {
                await _favoriteService.RemoveFromFavoritesAsync(userId, bookId);
            }
            else
            {
                // Recupera i dettagli del libro dalla ricerca attuale
                var book = await _bookApiService.GetBookByIdAsync(bookId);
                if (book != null)
                {
                    await _favoriteService.AddToFavoritesAsync(userId, bookId, book);
                }
            }

            // Ritorna alla stessa pagina di risultati
            return RedirectToPage("./Results", new { title = title });
        }

        // Corretto: rinominato per evitare conflitti
        public async Task<JsonResult> OnPostToggleFavoriteAjaxAsync(string bookId, string title)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return new JsonResult(new { success = false, message = "Utente non autenticato" }) { StatusCode = 401 };
            }

            try
            {
                int userId = int.Parse(User.FindFirstValue("UserId"));

                // Verifica se il libro � gi� nei preferiti
                bool isInFavorites = await _favoriteService.IsInFavoritesAsync(userId, bookId);

                if (isInFavorites)
                {
                    await _favoriteService.RemoveFromFavoritesAsync(userId, bookId);
                    return new JsonResult(new { success = true, isFavorite = false, message = "Libro rimosso dai preferiti" });
                }
                else
                {
                    // Recupera i dettagli del libro dall'API
                    var book = await _bookApiService.GetBookByIdAsync(bookId);
                    if (book != null)
                    {
                        await _favoriteService.AddToFavoritesAsync(userId, bookId, book);
                        return new JsonResult(new { success = true, isFavorite = true, message = "Libro aggiunto ai preferiti" });
                    }
                    else
                    {
                        return new JsonResult(new { success = false, message = "Libro non trovato" }) { StatusCode = 404 };
                    }
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message }) { StatusCode = 500 };
            }
        }
    }

    // Classe per memorizzare le informazioni AI generate sul libro
    public class BookAIInfo
    {
        public string Plot { get; set; }
        public List<BookCharacter> Characters { get; set; }
        public List<string> Themes { get; set; }
        public string Audience { get; set; }
        public List<string> SimilarBooks { get; set; }
    }

    // Classe che rappresenta un personaggio del libro
    public class BookCharacter
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}