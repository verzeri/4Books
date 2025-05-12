using System;

namespace _4Books.Pages.Models
{
    public class Favorite
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string BookId { get; set; }
        public DateTime AddedDate { get; set; }

        // Proprietà di navigazione
        public User User { get; set; }
        public Book Book { get; set; }
    }
}
