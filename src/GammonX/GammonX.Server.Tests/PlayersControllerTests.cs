using GammonX.Server.Analysis;
using GammonX.Server.Contracts;
using GammonX.Server.Models;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using Newtonsoft.Json;

using System.Net.Http.Json;

namespace GammonX.Server.Tests
{
	public class PlayersControllerTests : IClassFixture<WebApplicationFactory<Program>>
	{
		private readonly WebApplicationFactory<Program> _factory;
		private const string TestPlayerName = "test123";

		public PlayersControllerTests(WebApplicationFactory<Program> factory)
		{
			_factory = factory.WithWebHostBuilder(builder => {
				builder.ConfigureServices(services =>
				{
					// pass
				});
			});
		}

		[Fact]
		public async Task CanCreateGetAndDeletePlayer()
		{
			var client = _factory.CreateClient();
			var serverUri = client.BaseAddress!.ToString().TrimEnd('/');
			var createPlayerId = Guid.NewGuid();

			var createRequest = new CreateRequest(createPlayerId, TestPlayerName); 

			var response = await client.PostAsJsonAsync("/api/players/create", createRequest);
			var json = await response.Content.ReadAsStringAsync();
			var createResponse = JsonConvert.DeserializeObject<RequestResponseContract<RequestPlayerIdPayload>>(json);
			var createPayload = createResponse?.Payload;
			Assert.NotNull(createPayload);
			var playerId = createPayload.PlayerId;
			Assert.Equal(createPlayerId, playerId);

			response = await client.GetAsync($"/api/players/{playerId}");
			json = await response.Content.ReadAsStringAsync();
			var getResponse = JsonConvert.DeserializeObject<RequestResponseContract<RequestPlayerPayload>>(json);
			var getPayload = getResponse?.Payload;
			Assert.NotNull(getResponse);
			Assert.NotNull(getPayload);
			Assert.NotNull(getPayload.Player);
			Assert.Equal(createPlayerId, getPayload.Player.Id);
			Assert.Equal(TestPlayerName, getPayload.Player.UserName);
			Assert.Null(getPayload.Player.Points);

			response = await client.PostAsync($"/api/players/{playerId}/delete", null);
			json = await response.Content.ReadAsStringAsync();
			var deleteResponse = JsonConvert.DeserializeObject<RequestResponseContract<DeleteRequestPayload>>(json);
			var deletePayload = deleteResponse?.Payload;
			Assert.NotNull(deletePayload);
			Assert.True(deletePayload.Deleted);

			client.Dispose();
		}
	}
}
