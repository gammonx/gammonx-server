namespace GammonX.Server
{
	[AttributeUsage(AttributeTargets.Method)]
	public class ServerCommandAttribute : Attribute
	{
		public string Value { get; }

		public ServerCommandAttribute(string value)
		{
			Value = value;
		}
	}
}
