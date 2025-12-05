using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;
using GammonX.Lambda.Services;
using GammonX.Models.Contracts;

using Newtonsoft.Json;

using Xunit;

namespace GammonX.Lambda.Tests.Sqs
{
    public class PlayerCreatedLambdaHandlerTests
    {
        [Fact]
        public async Task OnPlayerStatsUpdatedEventTest()
        {
            var playerId = Guid.NewGuid();
            var userName = "bestInTown";

            var playerRecord = new PlayerRecordContract()
            {
                Id = playerId,
                UserName = userName
            };

            var messageId1 = Guid.NewGuid().ToString();

            var sqsEvent = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    new SQSEvent.SQSMessage
                    {
                        Body = JsonConvert.SerializeObject(playerRecord),
                        MessageId = messageId1,
                    },
                }
            };

            var logger = new TestLambdaLogger();
            var context = new TestLambdaContext
            {
                Logger = logger
            };

            var services = Startup.Configure();
            await Startup.ConfigureDynamoDbTableAsync(services);
            var handler = LambdaFunctionFactory.CreateSqsHandler(services, LambdaFunctions.PlayerCreatedFunc);

            await handler.HandleAsync(sqsEvent, context);
            Assert.Contains($"Processing message with id '{messageId1}'", logger.Buffer.ToString());
        }
    }
}
