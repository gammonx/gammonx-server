using GammonX.DynamoDb.Repository;

namespace GammonX.Lambda.Handlers
{
	public abstract class LambdaHandlerBaseImpl
	{
		private protected IDynamoDbRepository? Repo;

		/// <summary>
		/// Custom runtime constructor with DI services.
		/// </summary>
		/// <param name="repo">DI service.</param>
		protected LambdaHandlerBaseImpl(IDynamoDbRepository repo)
		{
			Repo = repo;
		}

		/// <summary>
		/// ZIP based runtime constructor without DI services.
		/// </summary>
		protected LambdaHandlerBaseImpl()
		{
			Repo = null;
		}
	}
}
