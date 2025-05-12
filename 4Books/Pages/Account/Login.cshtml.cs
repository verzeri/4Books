using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using _4Books.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace _4Books.Pages.Account
{
    [AllowAnonymous] //accesso senza autenticazione
    public class LoginModel : PageModel
    {
        private readonly UserService _userService;

        public LoginModel(UserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ErrorMessage { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "L'email è obbligatoria")]
            [EmailAddress(ErrorMessage = "Formato email non valido")]
            public string Email { get; set; }

            [Required(ErrorMessage = "La password è obbligatoria")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Ricordami")]
            public bool RememberMe { get; set; }
        }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var isValid = await _userService.ValidateCredentialsAsync(Input.Email, Input.Password);
            if (!isValid)
            {
                ErrorMessage = "Email o password non validi.";
                return Page();
            }

            var user = await _userService.GetUserByEmailAsync(Input.Email);

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.FirstName),
        new Claim(ClaimTypes.Surname, user.LastName),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim("UserId", user.Id.ToString()),
        new Claim(ClaimTypes.Role, user.Role) // Il ruolo sarà "Admin" invece di "Medico"
    };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = Input.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Reindirizza in base al ruolo
            // Cambiato da "Medico" a "Admin"
            if (user.Role == "Admin")
            {
                return RedirectToPage("/Admin/Dashboard");
            }

            // Altrimenti, reindirizza alla pagina di ricerca standard
            return RedirectToPage("/Book/Index");
        }
    }
}