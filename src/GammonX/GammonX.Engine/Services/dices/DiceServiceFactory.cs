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
		/// <returns>An instance of <see cref="IDiceService"/>.</returns>
		IDiceService Create();
    }

	// <inheritdoc />
	public class DiceServiceFactory : IDiceServiceFactory
    {
		// <inheritdoc />
		public IDiceService Create()
        {
            return new SimpleDiceService();
        }
    }
}
