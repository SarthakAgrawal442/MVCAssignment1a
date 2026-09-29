using Microsoft.AspNetCore.Identity;

namespace MVCSampleApp.Models
{
    // Extends the built-in Identity user. PhoneNumber field (already on IdentityUser)
    // is what we use to send the SMS 2FA code.
    public class ApplicationUser : IdentityUser
    {
    }
}
