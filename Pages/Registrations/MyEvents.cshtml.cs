using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagementSystem.Data;
using EventManagementSystem.Models;

namespace EventManagementSystem.Pages.Registrations;

[Authorize]
public class MyEventsModel : PageModel
{
    private readonly RegistrationService _registrationService;

    public MyEventsModel(RegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    public List<Registration> Registrations { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Registrations = await _registrationService.GetByUserAsync(userId);
    }
}
