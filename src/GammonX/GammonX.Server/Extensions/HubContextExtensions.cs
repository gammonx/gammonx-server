using Microsoft.AspNetCore.SignalR;

using System.Security.Claims;

namespace GammonX.Server.Extensions
{
    public static class HubContextExtensions
    {
        public static Guid? GetPlayerId(this HubCallerContext context)
        {
            var user = context.User;
            var claim = user?.FindFirst("playerId")
                     ?? user?.FindFirst(ClaimTypes.NameIdentifier)
                     ?? user?.FindFirst("sub");
            if (claim != null && Guid.TryParse(claim.Value, out var id))
            {
                return id;
            }
            return null;
        }

        public static Guid? GetMatchId(this HubCallerContext context)
        {
            var value = context.GetHttpContext()?.Request.Query["matchId"].FirstOrDefault();
            if (value != null && Guid.TryParse(value, out var id))
            {
                return id;
            }
            return null;
        }
    }
}
