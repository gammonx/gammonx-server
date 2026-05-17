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
            // we check if we can extract the match id from the query
            var value = context.GetHttpContext()?.Request.Query["id"].FirstOrDefault();
            if (value != null && Guid.TryParse(value, out var matchIdFromQuery))
            {
                return matchIdFromQuery;
            }

            // we fall back and check if the match id is given in the user claims
            var user = context.User;
            var claim = user?.FindFirst("matchId");
            if (claim != null && Guid.TryParse(claim.Value, out var matchIdFromClaim))
            {
                return matchIdFromClaim;
            }

            return null;
        }
    }
}
