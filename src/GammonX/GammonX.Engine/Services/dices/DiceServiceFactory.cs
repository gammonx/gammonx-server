namespace GammonX.Engine.Services
{
    /// <summary>
    /// Provides the capability to create a <see cref="IDiceService"/> instance.
    /// </summary>
    public interface IDiceServiceFactory
    {
		/// <summary>
		/// Creates a new instance of <see cref="IDiceService"/>.   
		/// </summary>
        /// <param name="type">Determines the type of the dice service.</param>
		/// <returns>An instance of <see cref="IDiceService"/>.</returns>
		IDiceService Create(DiceServiceType type);
    }

	// <inheritdoc />
	public class DiceServiceFactory : IDiceServiceFactory
    {
		// <inheritdoc />
		public IDiceService Create(DiceServiceType type)
        {
            switch (type)
            {
                case DiceServiceType.Simple:
                    return new SimpleDiceService();
                case DiceServiceType.Crypto:
                    return new CryptoDiceService();
                default:
                    throw new NotSupportedException($"The dice service type '{type}' is not supported.");
            }
        }
    }

    public enum DiceServiceType
    {
        Simple = 0,
        Crypto = 1
    }
}
