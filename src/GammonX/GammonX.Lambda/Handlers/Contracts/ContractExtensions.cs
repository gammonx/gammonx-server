using GammonX.DynamoDb.Items;

namespace GammonX.Lambda.Handlers.Contracts
{
    public static class ContractExtensions
    {
        public static PlayerRatingResponseContract ToResponse(this PlayerRatingItem item)
        {
            return new PlayerRatingResponseContract()
            {
                Rating = item.Rating
            };
        }
    }
}
