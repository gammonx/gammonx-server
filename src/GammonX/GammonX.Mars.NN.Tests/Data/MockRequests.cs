namespace GammonX.Mars.NN.Tests.Data
{
    public static class MockRequests
    {
        #region Cube Eval Requests

        public const string CubeEvalRequestMustOfferDouble = "{\"modus\":0,\"isWhite\":false,\"board\":{\"fields\":[-1,0,2,-1,-1,4,0,2,0,0,-1,-2,5,1,0,-1,-3,0,-4,0,1,0,-1,0],\"bearOffCountWhite\":0,\"bearOffCountBlack\":0,\"pipCountWhite\":164,\"pipCountBlack\":146,\"doublingCubeValue\":1},\"pointsAwayPlayer\":1,\"pointsAwayOpp\":1,\"matchLength\":1,\"botLevel\":2}";

        public const string CubeEvalRequestOnePlayerIsBehindBig = "{\"modus\":0,\"isWhite\":true,\"board\":{\"fields\":[-2,0,0,0,0,5,0,4,0,0,1,-5,3,0,0,0,-3,0,-5,0,0,0,0,2],\"bearOffCountWhite\":0,\"bearOffCountBlack\":0,\"pipCountWhite\":167,\"pipCountBlack\":160,\"doublingCubeValue\":1},\"pointsAwayPlayer\":5,\"pointsAwayOpp\":1,\"matchLength\":5,\"botLevel\":2}";

        public const string CubeEvalRequestBotMustTakeInstantDoubleIfOppBehindBig = "{\"modus\":0,\"isWhite\":true,\"board\":{\"fields\":[-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-2,0,-4,0,-2,0,0,2],\"bearOffCountWhite\":0,\"bearOffCountBlack\":0,\"pipCountWhite\":161,\"pipCountBlack\":167,\"doublingCubeValue\":1},\"pointsAwayPlayer\":1,\"pointsAwayOpp\":5,\"matchLength\":5,\"botLevel\":2}";

        #endregion Cube Eval Requests
    }
}
