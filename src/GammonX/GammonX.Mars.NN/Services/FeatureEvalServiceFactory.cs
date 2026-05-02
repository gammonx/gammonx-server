using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Services
{
    public static class FeatureEvalServiceFactory
    {
        public static IFeatureEvalService Create(GameModus modus, INeuralEvalService evalService)
        {
            switch (modus)
            {
                case GameModus.Plakoto:
                    return new PlakotoFeatureEvalService(evalService);
                case GameModus.Fevga:
                    return new FevgaFeatureEvalService(evalService);
                default:
                    throw new InvalidOperationException("the given game modus is not yet supported");
            }
        }
    }
}
