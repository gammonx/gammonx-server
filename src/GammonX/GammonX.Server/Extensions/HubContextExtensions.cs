using Microsoft.AspNetCore.SignalR;

using System.Security.Claims;

namespace GammonX.Server.Extensions
{
    public static class HubContextExtensions
    {
        public static Guid? GetPlayerId(this HubCallerContext context)
        {
            var user = context.User;
            var claim = user?.FindFirst("playerId") ?? user?.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null)
            {
                return Guid.Parse(claim.Value);
            }
            return null;
        }

        public static Guid? GetMatchId(this HubCallerContext context)
        {
            var user = context.User;
            var claim = user?.FindFirst("matchId");
            if (claim != null)
            {
                return Guid.Parse(claim.Value);
            }
            return null;
        }
    }
}
