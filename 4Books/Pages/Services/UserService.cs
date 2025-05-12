using System;
using System.Threading.Tasks;
using _4Books.Pages.Data;
using _4Books.Pages.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace _4Books.Services
{
    public class UserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher; // Dichiarazione campo

        // Aggiorna il costruttore per ricevere l'IPasswordHasher
        public UserService(ApplicationDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<(bool Success, string Message)> RegisterAsync(string firstName, string lastName, string email, string password)
        {
            // Verifica se l'utente esiste già
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                return (false, "Email già registrata.");
            }

            // Determina il ruolo in base all'email
            // Modificato da @medico a @admin
            string role = email.EndsWith("@admin") ? "Admin" : "User";

            // Crea l'hash della password
            var newUser = new User(); // Oggetto temporaneo per l'hashing
            var passwordHash = _passwordHasher.HashPassword(newUser, password);

            // Crea nuovo utente
            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PasswordHash = passwordHash,
                CreatedDate = DateTime.UtcNow,
                Role = role // Assegna il ruolo
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return (true, "Registrazione completata con successo.");
        }

        public async Task<bool> ValidateCredentialsAsync(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return false;
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

            if (result == PasswordVerificationResult.Success)
            {
                // Aggiorna LastLoginDate
                user.LastLoginDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}