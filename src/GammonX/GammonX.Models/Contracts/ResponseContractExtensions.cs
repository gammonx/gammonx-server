namespace GammonX.Models.Contracts
{
    public static class ContractExtensions
    {
        public static ErrorResponseContract ToResponse(string message)
        {
            return new ErrorResponseContract()
            {
                Message = message
            };
        }
    }
}
