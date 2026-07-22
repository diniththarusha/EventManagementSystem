using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagementSystem.Data;
using EventManagementSystem.Models;

namespace EventManagementSystem.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly UserService _userService;

    public RegisterModel(UserService userService)
    {
        _userService = userService;
    }

    [BindProperty, Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [BindProperty, Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [BindProperty, Required, StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (await _userService.EmailExistsAsync(Email))
        {
            ModelState.AddModelError(string.Empty, "An account with this email already exists.");
            return Page();
        }

        var user = new User
        {
            FullName = FullName,
            Email = Email,
            PasswordHash = PasswordHasher.Hash(Password),
            Role = "Attendee" // Promote to Admin manually in the DB when needed
        };

        await _userService.CreateAsync(user);
        return RedirectToPage("Login");
    }
}
