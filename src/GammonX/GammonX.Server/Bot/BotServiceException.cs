namespace GammonX.Server.Bot
{
    public class BotServiceException : Exception
    {
        public string BotName { get; }

        public BotServiceException(string botName, string message)
            : base(message)
        {
            BotName = botName;
        }

        public BotServiceException(string botName, string message, Exception innerException)
            : base(message, innerException)
        {
            BotName = botName;
        }
    }
}