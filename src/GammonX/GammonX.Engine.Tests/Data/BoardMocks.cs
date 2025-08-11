namespace GammonX.Engine.Tests.Data
{
	internal static class BoardMocks
	{
		public static int[] StandardCanBearOffBoard = new int[24]
        {
			 5, // 0 – Black Home
             5, // 1 – Black Home
             3, // 2 – Black Home
             1, // 3 - Black Home
             1, // 4 - Black Home
             1, // 5 - Black Home
             0, // 6
             0, // 7
             0, // 8
             0, // 9
             0, // 10
             0, // 11
             0, // 12
             0, // 13
             0, // 14
             0, // 15
             0, // 16
             0, // 17
            -3, // 18 – White Home
            -3, // 19 – White Home
            -3, // 20 – White Home
            -3, // 21 – White Home
            -2, // 22 – White Home
            -1  // 23 - White Home
        };

		public static int[] StandardCanNotBearOffBoard = new int[24]
		{
			 4, // 0 – Black Home
             5, // 1 – Black Home
             3, // 2 – Black Home
             1, // 3 - Black Home
             1, // 4 - Black Home
             1, // 5 - Black Home
             1, // 6
             0, // 7
             0, // 8
             0, // 9
             0, // 10
             0, // 11
             0, // 12
             0, // 13
             0, // 14
             0, // 15
             0, // 16
            -1, // 17
            -2, // 18 – White Home
            -3, // 19 – White Home
            -3, // 20 – White Home
            -3, // 21 – White Home
            -2, // 22 – White Home
            -1  // 23 - White Home
        };

		public static int[] FevgaCanBearOffBoard = new int[24]
		{
			 0, // 0
             0, // 1
             0, // 2
             0, // 3
             0, // 4
             0, // 5
             5, // 6  – Black Home
             5, // 7  – Black Home
             2, // 8  – Black Home
             1, // 9  – Black Home
             1, // 10 – Black Home
             1, // 11 – Black Home
             0, // 12
             0, // 13
             0, // 14
             0, // 15
             0, // 16
             0, // 17
            -3, // 18 – White Home
            -3, // 19 – White Home
            -3, // 20 – White Home
            -3, // 21 – White Home
            -2, // 22 – White Home
            -1  // 23 - White Home
        };

		public static int[] FevgaCanNotBearOffBoard = new int[24]
		{
			 1, // 0
            -1, // 1
             0, // 2
             0, // 3
             0, // 4
             0, // 5
             4, // 6  – Black Home
             5, // 7  – Black Home
             2, // 8  – Black Home
             1, // 9  – Black Home
             1, // 10 – Black Home
             1, // 11 – Black Home
             0, // 12
             0, // 13
             0, // 14
             0, // 15
             0, // 16
             0, // 17
            -2, // 18 – White Home
            -3, // 19 – White Home
            -3, // 20 – White Home
            -3, // 21 – White Home
            -2, // 22 – White Home
            -1  // 23 - White Home
        };
	}
}
