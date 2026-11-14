using GammonX.Models.Enums;

namespace GammonX.Server.Bot;

/// <summary>
/// Provides stable player identities for each bot difficulty across matches and rating histories.
/// </summary>
public static class WellKnownBotPlayers
{
    /// <summary>
    /// Connection ID reserved for all server-controlled bots, independent of their player identity and difficulty.
    /// </summary>
    public const string ConnectionId = "gammonx-bot";

    private readonly static Guid LegacyId;

    private readonly static Guid EasyId;

    private readonly static Guid MediumId;

    private readonly static Guid HardId;

    private readonly static Guid ExpertId;

    static WellKnownBotPlayers()
    {
        LegacyId = new Guid("7d7f63ca-112a-4d92-9881-36ee1a66aeb6");
        EasyId = new Guid("3ab8c995-d304-4680-a106-fbc4c56d4e99");
        MediumId = new Guid("94c286c5-ee56-4991-8da0-83e2cb21c440");
        HardId = new Guid("5320200d-2c28-4d02-a2eb-e4e739ae0ac6");
        ExpertId = new Guid("43557e28-09c4-4625-8c29-9001f0b61ea6");
    }

    /// <summary>
    /// Gets the reserved player ID for the specified bot level.
    /// </summary>
    /// <param name="botLevel">Supported bot difficulty.</param>
    /// <returns>The stable player ID for this difficulty.</returns>
    /// <exception cref="ArgumentException">The level is unsupported.</exception>
    public static Guid GetBotPlayerId(BotLevel botLevel)
    {
        switch (botLevel)
        {
            case BotLevel.Easy:
                return EasyId;
            case BotLevel.Medium:
                return MediumId;
            case BotLevel.Hard:
                return HardId;
            case BotLevel.Expert:
                return ExpertId;
            default:
                throw new ArgumentException("Unknown bot level provided.", nameof(botLevel));
        }
    }
}
