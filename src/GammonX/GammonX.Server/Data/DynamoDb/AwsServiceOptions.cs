namespace GammonX.Server.Data.DynamoDb
{
	public class AwsServiceOptions
	{
		public string AWS_ACCESS_KEY_ID { get; set; } = string.Empty;

		public string AWS_SECRET_ACCESS_KEY { get; set; } = string.Empty;

		public string DYNAMODB_SERVICEURL { get; set; } = string.Empty;

		public string DYNAMODB_TABLENAME { get; set; } = string.Empty;

		public string REGION { get; set; } = string.Empty;

		public bool Required => !string.IsNullOrEmpty(DYNAMODB_TABLENAME);
	}
}
