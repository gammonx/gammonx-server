using System.Net;
using System.Text;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using GammonX.Server.Bot;
using GammonX.Server.Tests.Http;

namespace GammonX.Server.Tests.Bot.Mars
{
    internal static class MarsStubs
    {
        public const string MoveResponse = "{\"type\":\"OK\",\"payload\":{\"moveSequence\":{\"moves\":[]}}}";

        public static MarsClient CreateClient(ScriptedHttpMessageHandler handler, int maxAttempts)
        {
            return new MarsClient(maxAttempts, 0, handler)
            {
                BaseAddress = new Uri("https://mars.test/")
            };
        }

        public static EvalMoveRequestContract CreateMoveRequest(BotLevel botLevel = BotLevel.Hard)
        {
            return new EvalMoveRequestContract
            {
                Modus = GameModus.Backgammon,
                IsWhite = true,
                Rolls = [1, 2],
                Board = new BoardModelContract { Fields = new int[24] },
                BotLevel = botLevel,
            };
        }

        public static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string body)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }
    }
}