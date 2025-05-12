using System;
using System.ComponentModel.DataAnnotations;

namespace _4Books.Pages.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Il nome è obbligatorio")]
        [StringLength(50, ErrorMessage = "Il nome non può superare i 50 caratteri")]
        [Display(Name = "Nome")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Il cognome è obbligatorio")]
        [StringLength(50, ErrorMessage = "Il cognome non può superare i 50 caratteri")]
        [Display(Name = "Cognome")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "L'email è obbligatoria")]
        [EmailAddress(ErrorMessage = "Formato email non valido")]
        [StringLength(100, ErrorMessage = "L'email non può superare i 100 caratteri")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Password (hash)")]
        public string PasswordHash { get; set; }

        [Display(Name = "Ruolo")]
        public string Role { get; set; } = "User"; // Valore predefinito: utente normale

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginDate { get; set; }

        // Proprietà calcolata (non memorizzata nel DB) per ottenere il nome completo
        [Display(Name = "Nome completo")]
        public string FullName => $"{FirstName} {LastName}";
    }
}