using System;
using System.Collections.Generic;

namespace _4Books.Pages.Models
{
    public class Book
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public List<string> Authors { get; set; }
        public string Publisher { get; set; }
        public string PublishedDate { get; set; }
        public string? Description { get; set; }
        public int PageCount { get; set; }
        public string ThumbnailUrl { get; set; }
        public string InfoLink { get; set; }
        public List<string> Categories { get; set; } = new List<string>();
        public string Language { get; set; }
        public double AverageRating { get; set; }
        public int RatingsCount { get; set; }
    }

    public class BookSearchResult
    {
        public int TotalItems { get; set; }
        public List<Book> Books { get; set; }
    }
}