using GammonX.DynamoDb.Repository;

namespace GammonX.Lambda.Handlers
{
	public abstract class LambdaHandlerBaseImpl
	{
		protected private IDynamoDbRepository? _repo;

		/// <summary>
		/// Custom runtime constructor with DI services.
		/// </summary>
		/// <param name="repo">DI service.</param>
		public LambdaHandlerBaseImpl(IDynamoDbRepository repo)
		{
			_repo = repo;
		}

		/// <summary>
		/// ZIP based runtime constructor without DI services.
		/// </summary>
		public LambdaHandlerBaseImpl()
		{
			_repo = null;
		}
	}
}
