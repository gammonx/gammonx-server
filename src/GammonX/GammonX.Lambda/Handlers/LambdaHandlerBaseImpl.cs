using GammonX.Lambda.Services;

namespace GammonX.Lambda.Handlers
{
	public abstract class LambdaHandlerBaseImpl
	{
		protected static readonly IServiceProvider Services = Startup.Configure();

		protected private readonly IDynamoRepository _repo;

		public LambdaHandlerBaseImpl(IDynamoRepository repo)
		{
			_repo = repo;
		}
	}
}
