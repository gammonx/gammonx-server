using Xunit;

using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.SQSEvents;

using GammonX.Lambda.Services;

namespace GammonX.Lambda.Tests;

public class SqsEventLambdaFuncHandlerTests
{
	[Theory]
	[InlineData(LambdaFunctions.PlayerRatingUpdatedFunc)]
	[InlineData(LambdaFunctions.PlayerStatsUpdatedFunc)]
	public async Task TestSQSEventLambdaFunction(string funcName)
	{
		var sqsEvent = new SQSEvent
		{
			Records = new List<SQSEvent.SQSMessage>
			{
				new SQSEvent.SQSMessage
				{
					Body = "foobar",
					MessageId = Guid.NewGuid().ToString(),

				}
			}
		};

		var logger = new TestLambdaLogger();
		var context = new TestLambdaContext
		{
			Logger = logger
		};

		var services = Startup.Configure();
		var handler = LambdaFunctionFactory.Create(services, funcName);

		await handler.HandleAsync(sqsEvent, context);

		Assert.Contains("Processed message foobar", logger.Buffer.ToString());
	}
}