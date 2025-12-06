using GammonX.DynamoDb.Items;

namespace GammonX.Lambda.Handlers.Contracts
{
    public static class ResponseContractExtensions
    {
        public static PlayerRatingResponseContract ToResponse(this PlayerRatingItem item)
        {
            return new PlayerRatingResponseContract()
            {
                Rating = item.Rating
            };
        }

        public static ErrorResponseContract ToResponse(string message)
        {
            return new ErrorResponseContract()
            {
                Message = message
            };
        }
    }
}
