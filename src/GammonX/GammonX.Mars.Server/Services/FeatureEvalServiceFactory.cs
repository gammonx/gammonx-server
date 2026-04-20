using GammonX.Models.Enums;

namespace GammonX.Mars.Server.Services
{
    public static class FeatureEvalServiceFactory
    {
        public static IFeatureEvalService Create(GameModus modus)
        {
            switch (modus)
            {
                case GameModus.Plakoto:
                    return new PlakotoFeatureEvalService();
                case GameModus.Fevga:
                    return new FevgaFeatureEvalService();
                default:
                    throw new InvalidOperationException("the given game modus is not yet supported");
            }
        }
    }
}
