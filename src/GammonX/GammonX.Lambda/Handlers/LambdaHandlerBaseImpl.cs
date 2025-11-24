using GammonX.DynamoDb.Repository;

namespace GammonX.Lambda.Handlers
{
	public abstract class LambdaHandlerBaseImpl
	{
		protected static readonly IServiceProvider Services = Startup.Configure();

		protected private readonly IDynamoDbRepository _repo;

		public LambdaHandlerBaseImpl(IDynamoDbRepository repo)
		{
			_repo = repo;
		}
	}
}
