using System.Security.Claims;

namespace ReservationMVC.Helpers
{
    public static class ClaimsExtensions
    {
        public static bool IsProvider(this ClaimsPrincipal user) =>
            user?.Claims?.Any(c => c.Type == "app:role" && c.Value == "Provider") == true;

        public static string DisplayName(this ClaimsPrincipal user) =>
            user?.Claims?.FirstOrDefault(c => c.Type == "FullName")?.Value
            ?? user?.Identity?.Name
            ?? "Utilisateur";
    }
}
