using GammonX.Models.Enums;

using GammonX.Server.Bot;

namespace GammonX.Server.Tests.Utils
{
    public static class BotUtils
    {
        private static readonly HttpClient _wildBgClient = new() { BaseAddress = new Uri("http://localhost:8082/bot/wildbg/") };
        private static readonly HttpClient _marsClient = new() { BaseAddress = new Uri("http://localhost:8083/bot/mars/") };

        public static IBotService GetBotService(GameModus modus)
        {
            if (modus == GameModus.Plakoto || modus == GameModus.Fevga)
            {
                return new MarsBotService(_marsClient);
            }

            return new WildbgBotService(_wildBgClient);
        }
    }
}
