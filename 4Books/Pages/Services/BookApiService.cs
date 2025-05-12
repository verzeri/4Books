using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using _4Books.Pages.Models;

namespace _4Books.Services
{
    public class BookApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl = "https://www.googleapis.com/books/v1/volumes";

        public BookApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Metodo per recuperare un libro specifico tramite ID
        public async Task<Book> GetBookByIdAsync(string bookId)
        {
            Console.WriteLine($"Recuperando libro con ID: {bookId}");

            if (string.IsNullOrWhiteSpace(bookId))
            {
                return null;
            }

            var url = $"{_apiBaseUrl}/{bookId}";
            Console.WriteLine($"URL API: {url}");

            try
            {
                var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"Stato risposta API: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();

                    var bookItem = JsonSerializer.Deserialize<GoogleBookItem>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (bookItem != null && bookItem.VolumeInfo != null)
                    {
                        var book = new Book
                        {
                            Id = bookItem.Id,
                            Title = bookItem.VolumeInfo.Title ?? "Titolo non disponibile",
                            Authors = bookItem.VolumeInfo.Authors ?? new List<string>(),
                            Publisher = bookItem.VolumeInfo.Publisher ?? "Editore non disponibile",
                            PublishedDate = bookItem.VolumeInfo.PublishedDate ?? "Data non disponibile",
                            Description = bookItem.VolumeInfo.Description ?? "Nessuna descrizione disponibile", // Assicurati che non sia mai null
                            PageCount = bookItem.VolumeInfo.PageCount,
                            ThumbnailUrl = bookItem.VolumeInfo.ImageLinks?.Thumbnail ?? "",
                            InfoLink = bookItem.VolumeInfo.InfoLink ?? "",
                            Categories = bookItem.VolumeInfo.Categories ?? new List<string>(),
                            Language = bookItem.VolumeInfo.Language ?? "non specificato",
                            AverageRating = bookItem.VolumeInfo.AverageRating,
                            RatingsCount = bookItem.VolumeInfo.RatingsCount
                        };

                        Console.WriteLine($"Libro recuperato con successo: {book.Title}");
                        return book;
                    }
                }

                Console.WriteLine($"API ha restituito errore o libro non trovato: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eccezione durante chiamata API per il libro ID {bookId}: {ex.Message}");
                return null;
            }
        }

        public async Task<BookSearchResult> SearchBooksByTitle(string title, int maxResults = 10)
        {
            Console.WriteLine($"Cercando libri con titolo: {title}");

            if (string.IsNullOrWhiteSpace(title))
            {
                return new BookSearchResult { TotalItems = 0, Books = new List<Book>() };
            }

            var encodedTitle = Uri.EscapeDataString(title);
            // Modificata la query per ottenere risultati migliori combinando intitle e ricerca generale
            maxResults = Math.Clamp(maxResults, 1, 40);

            // Usa "intitle:" per dare priorità al titolo ma senza limitare solo al titolo esatto
            var url = $"{_apiBaseUrl}?q={encodedTitle}+intitle:{encodedTitle}&maxResults={maxResults}&projection=full";
            Console.WriteLine($"URL API: {url}");

            try
            {
                var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"Stato risposta API: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Risposta JSON: {jsonString.Substring(0, Math.Min(500, jsonString.Length))}..."); // Log parziale della risposta JSON

                    var searchResult = JsonSerializer.Deserialize<GoogleBooksApiResponse>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    var result = ConvertToBookSearchResult(searchResult);
                    Console.WriteLine($"Totale libri trovati: {searchResult.TotalItems}, Elaborati: {(result.Books?.Count ?? 0)} libri");
                    return result;
                }

                Console.WriteLine($"API ha restituito errore: {response.StatusCode}");
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Dettagli errore: {errorContent}");
                return new BookSearchResult { TotalItems = 0, Books = new List<Book>() };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eccezione durante chiamata API: {ex.Message}");
                if (ex is JsonException jsonEx)
                {
                    Console.WriteLine($"Errore di deserializzazione JSON: {jsonEx.Message}");
                }
                return new BookSearchResult { TotalItems = 0, Books = new List<Book>() };
            }
        }

        // Nuovo metodo per cercare libri con più parametri
        public async Task<BookSearchResult> SearchBooks(string query, string title = null, string author = null, int maxResults = 10)
        {
            Console.WriteLine($"Cercando libri con query: {query}, titolo: {title}, autore: {author}");

            if (string.IsNullOrWhiteSpace(query) && string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(author))
            {
                return new BookSearchResult { TotalItems = 0, Books = new List<Book>() };
            }

            var queryParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(query))
            {
                queryParts.Add(Uri.EscapeDataString(query));
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                queryParts.Add($"intitle:{Uri.EscapeDataString(title)}");
            }

            if (!string.IsNullOrWhiteSpace(author))
            {
                queryParts.Add($"inauthor:{Uri.EscapeDataString(author)}");
            }

            var queryString = string.Join("+", queryParts);
            maxResults = Math.Clamp(maxResults, 1, 40);

            var url = $"{_apiBaseUrl}?q={queryString}&maxResults={maxResults}&projection=full";
            Console.WriteLine($"URL API: {url}");

            try
            {
                var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"Stato risposta API: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();

                    var searchResult = JsonSerializer.Deserialize<GoogleBooksApiResponse>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    var result = ConvertToBookSearchResult(searchResult);
                    Console.WriteLine($"Totale libri trovati: {searchResult?.TotalItems ?? 0}, Elaborati: {(result.Books?.Count ?? 0)} libri");
                    return result;
                }

                Console.WriteLine($"API ha restituito errore: {response.StatusCode}");
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Dettagli errore: {errorContent}");
                return new BookSearchResult { TotalItems = 0, Books = new List<Book>() };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eccezione durante chiamata API: {ex.Message}");
                return new BookSearchResult { TotalItems = 0, Books = new List<Book>() };
            }
        }

        private BookSearchResult ConvertToBookSearchResult(GoogleBooksApiResponse apiResponse)
        {
            var result = new BookSearchResult
            {
                TotalItems = apiResponse?.TotalItems ?? 0,
                Books = new List<Book>()
            };

            if (apiResponse?.Items != null)
            {
                foreach (var item in apiResponse.Items)
                {
                    var volumeInfo = item.VolumeInfo;
                    if (volumeInfo != null)
                    {
                        // Aggiungi più informazioni dal libro
                        result.Books.Add(new Book
                        {
                            Id = item.Id,
                            Title = volumeInfo.Title,
                            Authors = volumeInfo.Authors ?? new List<string>(),
                            Publisher = volumeInfo.Publisher,
                            PublishedDate = volumeInfo.PublishedDate,
                            Description = volumeInfo.Description,
                            PageCount = volumeInfo.PageCount,
                            ThumbnailUrl = volumeInfo.ImageLinks?.Thumbnail,
                            InfoLink = volumeInfo.InfoLink,
                            Categories = volumeInfo.Categories ?? new List<string>(),
                            Language = volumeInfo.Language,
                            AverageRating = volumeInfo.AverageRating,
                            RatingsCount = volumeInfo.RatingsCount,
                            // Aggiungi altre proprietà se necessario
                        });
                    }
                }
            }

            return result;
        }

        // Classi per mappare la risposta dell'API di Google Books
        private class GoogleBooksApiResponse
        {
            public int TotalItems { get; set; }
            public List<GoogleBookItem> Items { get; set; }
        }

        private class GoogleBookItem
        {
            public string Id { get; set; }
            public GoogleVolumeInfo VolumeInfo { get; set; }
        }

        private class GoogleVolumeInfo
        {
            public string Title { get; set; }
            public List<string> Authors { get; set; }
            public string Publisher { get; set; }
            public string PublishedDate { get; set; }
            public string Description { get; set; }
            public int PageCount { get; set; }
            public GoogleImageLinks ImageLinks { get; set; }
            public string InfoLink { get; set; }
            public List<string> Categories { get; set; }
            public string Language { get; set; }
            public double AverageRating { get; set; }
            public int RatingsCount { get; set; }
        }

        private class GoogleImageLinks
        {
            public string Thumbnail { get; set; }
            public string SmallThumbnail { get; set; }
        }
    }
}