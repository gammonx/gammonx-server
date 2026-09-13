using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;
using Amazon.DynamoDBv2.Model;
using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;
using GammonX.Lambda.Handlers;
using GammonX.Lambda.Services;
using GammonX.Models.Contracts;
using GammonX.Models.History;

using Microsoft.Extensions.DependencyInjection;

using Moq;

using Newtonsoft.Json;

using Xunit;

namespace GammonX.Lambda.Tests.Sqs
{
	public class PlayerStatsUpdatedLambdaHandlerTests
	{
		[Fact]
		public async Task OnPlayerStatsUpdatedEventTest()
		{
			var parser = HistoryParserFactory.Create<IGameHistoryParser>(Models.Enums.HistoryFormat.MAT);

			// player ids must match with the game history
			var player1Id = Guid.Parse("cf0ab132-2279-43d3-911f-ed139ce5e7ba");
			var player2Id = Guid.Parse("e51f307e-3bf6-4408-b4b7-5fabd41b57b8");

			// GAME 1 :: PORTES
			var portesGameId = Guid.Parse("c57e0961-02e7-4aac-857f-565e9d78db09");
			var portesPath = Path.Combine("Data", "PortesGameHistory.txt");
			var portesGameHistory = await File.ReadAllTextAsync(portesPath, TestContext.Current.CancellationToken);
			var parsedPortesHistory = parser.ParseGame(portesGameHistory);
            Assert.NotNull(parsedPortesHistory);
			var wonPortesGame = new GameRecordContract()
			{
				Id = portesGameId,
				PlayerId = player1Id,
				Result = Models.Enums.GameResult.Gammon,
				DoublingCubeValue = null,
				PipesLeft = 0,
				Format = Models.Enums.HistoryFormat.MAT,
				GameHistory = portesGameHistory
			};
			var lostPortesGame = new GameRecordContract()
			{
				Id = portesGameId,
				PlayerId = player2Id,
				Result = Models.Enums.GameResult.LostSingle,
				DoublingCubeValue = null,
				PipesLeft = 55,
				Format = Models.Enums.HistoryFormat.MAT,
				GameHistory = portesGameHistory
			};
			// GAME 2 :: PLAKOTO
			var plakotoGameId = Guid.Parse("3cf7ebbe-e0dd-4a2d-baa8-361014efa989");
			var plakotoPath = Path.Combine("Data", "PlakotoGameHistory.txt");
			var plakotoGameHistory = await File.ReadAllTextAsync(plakotoPath, TestContext.Current.CancellationToken);
			var parsedPlakotoHistory = parser.ParseGame(plakotoGameHistory);
            Assert.NotNull(parsedPlakotoHistory);
			var wonPlakotoGame = new GameRecordContract()
			{
				Id = plakotoGameId,
				PlayerId = player1Id,
				Result = Models.Enums.GameResult.Single,
				DoublingCubeValue = null,
				PipesLeft = 0,
				Format = Models.Enums.HistoryFormat.MAT,
				GameHistory = plakotoGameHistory
			};
			var lostPlakotoGame = new GameRecordContract()
			{
				Id = plakotoGameId,
				PlayerId = player2Id,
				Result = Models.Enums.GameResult.LostSingle,
				DoublingCubeValue = null,
				PipesLeft = 55,
				Format = Models.Enums.HistoryFormat.MAT,
				GameHistory = plakotoGameHistory
			};
			// GAME 3 :: FEVGA
			var fevgaGameId = Guid.Parse("48fb1a93-9c2b-4245-803b-8361be6c6838");
			var fevgaPath = Path.Combine("Data", "FevgaGameHistory.txt");
			var fevgaGameHistory = await File.ReadAllTextAsync(fevgaPath, TestContext.Current.CancellationToken);
			var parsedFevgaHistory = parser.ParseGame(fevgaGameHistory);
            Assert.NotNull(parsedFevgaHistory);
			var wonFevgaGame = new GameRecordContract()
			{
				Id = fevgaGameId,
				PlayerId = player1Id,
				Result = Models.Enums.GameResult.Single,
				DoublingCubeValue = null,
				PipesLeft = 0,
				Format = Models.Enums.HistoryFormat.MAT,
				GameHistory = fevgaGameHistory
			};
			var lostFevgaGame = new GameRecordContract()
			{
				Id = fevgaGameId,
				PlayerId = player2Id,
				Result = Models.Enums.GameResult.LostSingle,
				DoublingCubeValue = null,
				PipesLeft = 55,
				Format = Models.Enums.HistoryFormat.MAT,
				GameHistory = fevgaGameHistory
			};


			// MATCH 1 :: TAVLI
			var matchId = Guid.Parse("888a356e-e09f-4a0f-b909-581f1ffb167e");
			var path = Path.Combine("Data", "TavliMatchHistory.txt");
			var matchHistory = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
			var wonTavliMatch = new MatchRecordContract()
			{
				Id = matchId,
				PlayerId = player1Id,
				Result = Models.Enums.MatchResult.Won,
				Variant = Models.Enums.MatchVariant.Tavli,
				Modus = Models.Enums.MatchModus.Normal,
				Type = Models.Enums.MatchType.SevenPointGame,
                BotLevel = Models.Enums.BotLevel.Hard,
                Format = Models.Enums.HistoryFormat.MAT,
				MatchHistory = matchHistory,
				Games = new[] { wonPortesGame, wonPlakotoGame, wonFevgaGame }
			};
			var lostTavliMatch = new MatchRecordContract()
			{
				Id = matchId,
				PlayerId = player2Id,
				Result = Models.Enums.MatchResult.Lost,
				Variant = Models.Enums.MatchVariant.Tavli,
				Modus = Models.Enums.MatchModus.Normal,
				Type = Models.Enums.MatchType.SevenPointGame,
                BotLevel = Models.Enums.BotLevel.Hard,
                Format = Models.Enums.HistoryFormat.MAT,
				MatchHistory = matchHistory,
				Games = new[] { lostPortesGame, lostPlakotoGame, lostFevgaGame }
			};

			var messageId1 = Guid.NewGuid().ToString();
			var messageId2 = Guid.NewGuid().ToString();

			var sqsEvent = new SQSEvent
			{
				Records = new List<SQSEvent.SQSMessage>
				{
					new SQSEvent.SQSMessage
					{
						Body = JsonConvert.SerializeObject(wonTavliMatch),
						MessageId = messageId1,
					},
					new SQSEvent.SQSMessage
					{
						Body = JsonConvert.SerializeObject(lostTavliMatch),
						MessageId = messageId2,
					}
				}
			};

			var logger = new TestLambdaLogger();
			var context = new TestLambdaContext
			{
				Logger = logger
			};

			var services = Startup.Configure();
            await Startup.ConfigureDynamoDbTableAsync(services);
			var repository = services.GetRequiredService<IDynamoDbRepository>();
			const string statsSk = "STATS#Tavli#SevenPointGame#Normal";
			await repository.DeleteAsync<PlayerStatsItem>(player1Id, statsSk);
			await repository.DeleteAsync<PlayerStatsItem>(player2Id, statsSk);
			var endedAt = DateTime.UtcNow;

			await repository.SaveAsync(new MatchItem
			{
				Id = matchId,
				PlayerId = player1Id,
				Variant = wonTavliMatch.Variant,
				Modus = wonTavliMatch.Modus,
				Type = wonTavliMatch.Type,
				Result = wonTavliMatch.Result,
				EndedAt = endedAt
			});

			await repository.SaveAsync(new MatchItem
			{
				Id = matchId,
				PlayerId = player2Id,
				Variant = lostTavliMatch.Variant,
				Modus = lostTavliMatch.Modus,
				Type = lostTavliMatch.Type,
				Result = lostTavliMatch.Result,
				EndedAt = endedAt
			});

            var handler = LambdaFunctionFactory.CreateSqsHandler(services, LambdaFunctions.PlayerStatsUpdatedFunc);

			await handler.HandleAsync(sqsEvent, context);

			Assert.Contains($"Processing message with id '{messageId1}'", logger.Buffer.ToString());
			Assert.Contains($"Processing message with id '{messageId2}'", logger.Buffer.ToString());
            Assert.Contains($"Processed stat update for player with id '{player1Id}'", logger.Buffer.ToString());
            Assert.Contains($"Processed stat update for player with id '{player2Id}'", logger.Buffer.ToString());
        }

		[Fact]
		public async Task OnPlayerStatsUpdatedUsesPlayerIdAndAccumulatesExistingMatches()
		{
			var playerId = Guid.Parse("cf0ab132-2279-43d3-911f-ed139ce5e7ba");
			var existingMatchId = Guid.NewGuid();
			var newMatchId = Guid.NewGuid();
			var existingMatch = new MatchItem
			{
				Id = existingMatchId,
				PlayerId = playerId,
				Variant = Models.Enums.MatchVariant.Tavli,
				Modus = Models.Enums.MatchModus.Normal,
				Type = Models.Enums.MatchType.SevenPointGame,
				Result = Models.Enums.MatchResult.Won,
				Length = 3,
				Duration = TimeSpan.FromMinutes(30),
				AvgDuration = TimeSpan.FromMinutes(10),
				EndedAt = DateTime.UtcNow.AddDays(-1)
			};
			var matchHistory = await File.ReadAllTextAsync(
				Path.Combine("Data", "TavliMatchHistory.txt"),
				TestContext.Current.CancellationToken);

			var newMatch = new MatchRecordContract
			{
				Id = newMatchId,
				PlayerId = playerId,
				Result = Models.Enums.MatchResult.Won,
				Variant = Models.Enums.MatchVariant.Tavli,
				Modus = Models.Enums.MatchModus.Normal,
				Type = Models.Enums.MatchType.SevenPointGame,
				Format = Models.Enums.HistoryFormat.MAT,
				MatchHistory = matchHistory,
				Games = Array.Empty<GameRecordContract>()
			};

			var persistedNewMatch = new MatchItem
			{
				Id = newMatchId,
				PlayerId = playerId,
				Variant = newMatch.Variant,
				Modus = newMatch.Modus,
				Type = newMatch.Type,
				Result = newMatch.Result,
				Length = 3,
				Duration = TimeSpan.FromMinutes(20),
				AvgDuration = TimeSpan.FromMinutes(7),
				EndedAt = DateTime.UtcNow
			};

			var repository = new Mock<IDynamoDbRepository>();
			repository
				.Setup(repo => repo.GetItemsByGSIPKAsync<MatchItem>(playerId, "MATCH#Tavli#SevenPointGame#Normal"))
				.ReturnsAsync(new[] { existingMatch, persistedNewMatch });

			var transactionWriter = repository.As<IDynamoDbTransactionWriter>();

			transactionWriter
				.Setup(writer => writer.TransactPutAsync(It.IsAny<IEnumerable<DynamoDbPutOperation>>()))
				.Returns(Task.CompletedTask);

			var handler = new PlayerStatsUpdatedHandler(repository.Object);
			var context = new TestLambdaContext { Logger = new TestLambdaLogger() };
			var @event = new SQSEvent
			{
				Records = new List<SQSEvent.SQSMessage>
				{
					new()
					{
						MessageId = Guid.NewGuid().ToString(),
						Body = JsonConvert.SerializeObject(newMatch)
					}
				}
			};

			await handler.HandleAsync(@event, context);

			repository.Verify(
				repo => repo.GetItemsByGSIPKAsync<MatchItem>(playerId, "MATCH#Tavli#SevenPointGame#Normal"),
				Times.Once);

			transactionWriter.Verify(
				writer => writer.TransactPutAsync(It.Is<IEnumerable<DynamoDbPutOperation>>(operations => operations.Count() == 1)),
				Times.Once);
		}

		[Fact]
		public async Task OnPlayerStatsUpdatedContinuesAfterMalformedMessage()
		{
			var playerId = Guid.Parse("cf0ab132-2279-43d3-911f-ed139ce5e7ba");
			var matchHistory = await File.ReadAllTextAsync(
				Path.Combine("Data", "TavliMatchHistory.txt"),
				TestContext.Current.CancellationToken);
			var validMatch = new MatchRecordContract
			{
				Id = Guid.NewGuid(),
				PlayerId = playerId,
				Result = Models.Enums.MatchResult.Won,
				Variant = Models.Enums.MatchVariant.Tavli,
				Modus = Models.Enums.MatchModus.Normal,
				Type = Models.Enums.MatchType.SevenPointGame,
				Format = Models.Enums.HistoryFormat.MAT,
				MatchHistory = matchHistory,
				Games = Array.Empty<GameRecordContract>()
			};

			var persistedMatch = new MatchItem
			{
				Id = validMatch.Id,
				PlayerId = playerId,
				Variant = validMatch.Variant,
				Modus = validMatch.Modus,
				Type = validMatch.Type,
				Result = validMatch.Result,
				EndedAt = DateTime.UtcNow
			};

			var repository = new Mock<IDynamoDbRepository>();
			repository
				.Setup(repo => repo.GetItemsByGSIPKAsync<MatchItem>(playerId, "MATCH#Tavli#SevenPointGame#Normal"))
				.ReturnsAsync(new[] { persistedMatch });

			var transactionWriter = repository.As<IDynamoDbTransactionWriter>();
			transactionWriter
				.Setup(writer => writer.TransactPutAsync(It.IsAny<IEnumerable<DynamoDbPutOperation>>()))
				.Returns(Task.CompletedTask);

			var invalidMessageId = Guid.NewGuid().ToString();
			var validMessageId = Guid.NewGuid().ToString();
			var logger = new TestLambdaLogger();
			var handler = new PlayerStatsUpdatedHandler(repository.Object);
			var @event = new SQSEvent
			{
				Records = new List<SQSEvent.SQSMessage>
				{
					new() { MessageId = invalidMessageId, Body = "{" },
					new() { MessageId = validMessageId, Body = JsonConvert.SerializeObject(validMatch) }
				}
			};

			await Assert.ThrowsAsync<AggregateException>(() => handler.HandleAsync(@event, new TestLambdaContext { Logger = logger }));

			Assert.Contains($"Processing message with id '{validMessageId}'", logger.Buffer.ToString());
			Assert.Contains($"Processed stat update for player with id '{playerId}'", logger.Buffer.ToString());
			transactionWriter.Verify(writer => writer.TransactPutAsync(It.IsAny<IEnumerable<DynamoDbPutOperation>>()), Times.Once);
		}

		[Fact]
		public async Task OnPlayerStatsUpdatedRetriesWhenPersistedSourceIsMissing()
		{
			var playerId = Guid.NewGuid();
			var record = CreateMatchRecord(playerId, Guid.NewGuid());
			var repository = new Mock<IDynamoDbRepository>();
			repository
				.Setup(repo => repo.GetItemsByGSIPKAsync<MatchItem>(playerId, "MATCH#Tavli#SevenPointGame#Normal"))
				.ReturnsAsync(Array.Empty<MatchItem>());

			var transactionWriter = repository.As<IDynamoDbTransactionWriter>();

			var handler = new PlayerStatsUpdatedHandler(repository.Object);

			await Assert.ThrowsAsync<AggregateException>(() => handler.HandleAsync(
				CreateEvent(record),
				new TestLambdaContext { Logger = new TestLambdaLogger() }));

			transactionWriter.Verify(writer => writer.TransactPutAsync(It.IsAny<IEnumerable<DynamoDbPutOperation>>()), Times.Never);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		public async Task OnPlayerStatsUpdatedSkipsStaleOrDuplicateSourceAfterConditionalConflict(int existingSourceOffsetMinutes)
		{
			var playerId = Guid.NewGuid();
			var record = CreateMatchRecord(playerId, Guid.NewGuid());
			var sourceEndedAt = DateTime.UtcNow.AddMinutes(-1);
			var sourceMatch = CreatePersistedMatch(record, sourceEndedAt);

			var repository = new Mock<IDynamoDbRepository>();
			repository
				.Setup(repo => repo.GetItemsByGSIPKAsync<MatchItem>(playerId, "MATCH#Tavli#SevenPointGame#Normal"))
				.ReturnsAsync(new[] { sourceMatch });
			repository
				.Setup(repo => repo.GetItemsAsync<PlayerStatsItem>(playerId, "STATS#Tavli#SevenPointGame#Normal"))
				.ReturnsAsync(new[]
				{
					new PlayerStatsItem
					{
						PlayerId = playerId,
						Variant = record.Variant,
						Modus = record.Modus,
						Type = record.Type,
						SourceMatchEndedAt = sourceEndedAt.AddMinutes(existingSourceOffsetMinutes)
					}
				});

			var transactionWriter = repository.As<IDynamoDbTransactionWriter>();
			transactionWriter
				.Setup(writer => writer.TransactPutAsync(It.IsAny<IEnumerable<DynamoDbPutOperation>>()))
				.ThrowsAsync(new TransactionCanceledException("stale stats update"));

			var handler = new PlayerStatsUpdatedHandler(repository.Object);

			await handler.HandleAsync(
				CreateEvent(record),
				new TestLambdaContext { Logger = new TestLambdaLogger() });
		}

		private static MatchRecordContract CreateMatchRecord(Guid playerId, Guid matchId)
		{
			return new MatchRecordContract
			{
				Id = matchId,
				PlayerId = playerId,
				Result = Models.Enums.MatchResult.Won,
				Variant = Models.Enums.MatchVariant.Tavli,
				Modus = Models.Enums.MatchModus.Normal,
				Type = Models.Enums.MatchType.SevenPointGame,
				Format = Models.Enums.HistoryFormat.MAT,
				Games = Array.Empty<GameRecordContract>()
			};
		}

		private static MatchItem CreatePersistedMatch(MatchRecordContract record, DateTime endedAt)
		{
			return new MatchItem
			{
				Id = record.Id,
				PlayerId = record.PlayerId,
				Variant = record.Variant,
				Modus = record.Modus,
				Type = record.Type,
				Result = record.Result,
				EndedAt = endedAt
			};
		}

		private static SQSEvent CreateEvent(MatchRecordContract record)
		{
			return new SQSEvent
			{
				Records = new List<SQSEvent.SQSMessage>
				{
					new() { MessageId = Guid.NewGuid().ToString(), Body = JsonConvert.SerializeObject(record) }
				}
			};
		}
	}
}
