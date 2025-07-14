namespace GammonX.Engine
{
    /// <summary>
    /// Creates instances of <see cref="IDiceService"/>.
    /// </summary>
    public static class DiceServiceFactory
    {
        /// <summary>
        /// Creates a new instance of <see cref="IDiceService"/>.   
        /// </summary>
        /// <returns>An instance of <see cref="IDiceService"/>.</returns>
        public static IDiceService Create()
        {
            return new SimpleDiceService();
        }
    }
}
