using GammonX.Server.Extensions;
using GammonX.Server.Queue;

namespace GammonX.Server.Tests.Extensions
{
    public class WorkQueueServiceExtensionsTests
    {
        [Fact]
        public void ConfiguredQueueModeRequiresEveryQueueUrl()
        {
            var options = CreateConfiguredOptions();
            options.RATING_UPDATED_QUEUE_URL = string.Empty;

            var exception = Assert.Throws<InvalidOperationException>(
                () => WorkQueueServiceExtensions.ValidateWorkQueueOptions(options));

            Assert.Contains(nameof(WorkQueueOptions.RATING_UPDATED_QUEUE_URL), exception.Message);
        }

        [Fact]
        public void LoggingQueueModeAllowsMissingTypedQueueUrls()
        {
            WorkQueueServiceExtensions.ValidateWorkQueueOptions(new WorkQueueOptions());
        }

        [Fact]
        public void TypedQueueUrlsRequireConfiguredQueueMode()
        {
            var options = new WorkQueueOptions
            {
                GAME_COMPLETED_QUEUE_URL = "game"
            };

            var exception = Assert.Throws<InvalidOperationException>(
                () => WorkQueueServiceExtensions.ValidateWorkQueueOptions(options));

            Assert.Contains(nameof(WorkQueueOptions.URL), exception.Message);
        }

        [Fact]
        public void ConfiguredQueueModeRejectsInvalidRetrySettings()
        {
            var options = CreateConfiguredOptions();
            options.MAX_RETRY_ATTEMPTS = 0;

            var exception = Assert.Throws<InvalidOperationException>(
                () => WorkQueueServiceExtensions.ValidateWorkQueueOptions(options));

            Assert.Contains("retry attempts", exception.Message);
        }

        private static WorkQueueOptions CreateConfiguredOptions()
        {
            return new WorkQueueOptions
            {
                URL = "http://localhost:4566",
                GAME_COMPLETED_QUEUE_URL = "game",
                MATCH_COMPLETED_QUEUE_URL = "match",
                PLAYER_CREATED_QUEUE_URL = "player",
                STATS_UPDATED_QUEUE_URL = "stats",
                RATING_UPDATED_QUEUE_URL = "rating"
            };
        }
    }
}