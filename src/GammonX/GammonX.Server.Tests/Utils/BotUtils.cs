using GammonX.Models.Enums;

using GammonX.Server.Bot;

namespace GammonX.Server.Tests.Utils
{
    public static class BotUtils
    {
        private static readonly HttpClient WildBgClient = new() { BaseAddress = new Uri("http://localhost:8082/bot/wildbg/") };
        private static readonly HttpClient MarsClient = new() { BaseAddress = new Uri("http://localhost:8083/bot/mars/") };

        public static IBotService GetBotService(string botHint)
        {
            if (botHint == WellKnownBotServices.Mars)
            {
                return new MarsBotService(MarsClient);
            }
            else if (botHint == WellKnownBotServices.WildBg)
            {
                return new WildbgBotService(WildBgClient);
            }
            else
            {
                throw new NotSupportedException($"Bot hint '{botHint}' is not supported.");
            }
        }
    }
}
