// ProfileBookAPI/Services/NameUserIdProvider.cs
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ProfileBookAPI.Services
{
    public class NameUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            // Use ClaimTypes.NameIdentifier (this matches your MessagesController usage)
            var claim = connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(claim)) return claim;
            // fallback to Name
            return connection.User?.Identity?.Name ?? string.Empty;
        }
    }
}
