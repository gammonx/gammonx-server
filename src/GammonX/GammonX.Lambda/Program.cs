using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;

using GammonX.Models.Contracts;
using GammonX.Models.Helpers;

using GammonX.Lambda.Services;

using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

namespace GammonX.Lambda
{
	public class Program
	{
		public static async Task Main(string[] _)
		{
			var services = Startup.Configure();

			async Task<object> Router(object input, ILambdaContext context)
			{
                context.Logger.LogInformation($"Received input of type: '{input.GetType().FullName}' for function '{context.FunctionName}'");

                if (input is MemoryStream stream)
                {
                    var json = await ReadStreamAsStringAsync(stream);
                    context.Logger.LogInformation($"Received input JSON: {json.Length} characters");

                    var deserializedInput = DeserializeFunctionInput(json);

                    if (deserializedInput is SQSEvent sqsEvent)
                    {
                        context.Logger.LogInformation("Received SQS event. Creating dedicated function handler...");
                        return await HandleSqsEventAsync(context, services, sqsEvent);
                    }
                    else if (deserializedInput is APIGatewayProxyRequest apiRequest)
                    {
                        context.Logger.LogInformation("Received API Gateway request. Creating dedicated function handler...");
                        return await HandleGatewayRequestAsync(context, services, apiRequest);
                    }
                    else
                    {
                        context.Logger.LogInformation("Received unknown function input. Unable to create function handler. Returning empty response.");
                        return new object();
                    }
                }                
                else
                {
                    context.Logger.LogInformation("Received unknown function input. Unable to create function handler. Returning empty response.");
                    return new object();
                }
			}

			var bootstrapper = LambdaBootstrapBuilder.Create(Router, new DefaultLambdaJsonSerializer()).Build();
			await bootstrapper.RunAsync();
        }

        /// <summary>
        /// Handles an incoming SQS event by routing messages to the appropriate handlers based on their 'EVENT_TYPE'.
        /// </summary>
        /// <param name="context">The Lambda context.</param>
        /// <param name="services">The service provider.</param>
        /// <param name="sqsEvent">The incoming SQS event.</param>
        /// <returns>A task that represents the asynchronous operation, containing the batch response with any failed message identifiers.</returns>
        internal static async Task<SQSBatchResponse> HandleSqsEventAsync(
            ILambdaContext context,
            IServiceProvider services,
            SQSEvent sqsEvent)
        {
            using var scope = services.CreateScope();
            var failedMessageIds = new HashSet<string>(StringComparer.Ordinal);
            var messagesByEventType = new Dictionary<string, List<SQSEvent.SQSMessage>>(StringComparer.Ordinal);

            // We first scan all event records and group them by their 'EVENT_TYPE'
            foreach (var message in sqsEvent.Records)
            {
                if (message.MessageAttributes == null ||
                    !message.MessageAttributes.TryGetValue("EVENT_TYPE", out var eventTypeAttribute) ||
                    string.IsNullOrWhiteSpace(eventTypeAttribute.StringValue))
                {
                    context.Logger.LogError(
                        $"SQS message '{message.MessageId}' does not contain a valid EVENT_TYPE message attribute.");
                    failedMessageIds.Add(message.MessageId);
                    continue;
                }

                if (!messagesByEventType.TryGetValue(eventTypeAttribute.StringValue, out var messages))
                {
                    messages = [];
                    messagesByEventType.Add(eventTypeAttribute.StringValue, messages);
                }

                messages.Add(message);
            }

            // We await groups in priority order within this invocation. (Match/Game > Stats > Rating)
            // SQS may still invoke this Lambda concurrently for separate batches.
            foreach (var messageGroup in messagesByEventType.OrderBy(group => GetEventTypePriority(group.Key)))
            {
                try
                {
                    // We create the handler for the specific EVENT_TYPE
                    var handler = LambdaFunctionFactory.CreateSqsHandler(scope.ServiceProvider, messageGroup.Key);
                    var response = await handler.HandleAsync(new SQSEvent { Records = messageGroup.Value }, context);
                    // We iterate over all EVENT_TYPE specific messages
                    foreach (var failure in response.BatchItemFailures)
                    {
                        failedMessageIds.Add(failure.ItemIdentifier);
                    }
                }
                catch (Exception ex)
                {
                    context.Logger.LogError(
                        ex,
                        $"An error occurred while dispatching SQS messages of type '{messageGroup.Key}'.");
                    foreach (var message in messageGroup.Value)
                    {
                        failedMessageIds.Add(message.MessageId);
                    }
                }
            }

            var failures = sqsEvent.Records
                .Where(message => failedMessageIds.Contains(message.MessageId))
                .Select(message => new SQSBatchResponse.BatchItemFailure
                {
                    ItemIdentifier = message.MessageId
                })
                .ToList();

            return new SQSBatchResponse(failures);
        }

        private static int GetEventTypePriority(string eventType)
        {
            return eventType switch
            {
                var value when value == LambdaFunctions.MatchCompletedFunc => 0,
                var value when value == LambdaFunctions.PlayerStatsUpdatedFunc => 2,
                var value when value == LambdaFunctions.PlayerRatingUpdatedFunc => 3,
                _ => 1
            };
        }

        private static async Task<object> HandleGatewayRequestAsync(ILambdaContext context, IServiceProvider services, APIGatewayProxyRequest apiRequest)
        {
            try
            {
                using var scope = services.CreateScope();
                var handler = LambdaFunctionFactory.CreateApiHandler(apiRequest, scope.ServiceProvider, context);

                if (handler == null)
                {
                    var notFound = ContractExtensions.ToResponse("The requested api route does not exist");
                    return CreateGatewayResponse(404, notFound);
                }

                var result = await handler.HandleAsync(apiRequest, context);
                if (result == null)
                {
                    var error = ContractExtensions.ToResponse("An error occurred while handling the api request");
                    return CreateGatewayResponse(500, error);
                }
                return CreateGatewayResponse(200, result);
            }
            catch (Exception ex)
            {
                return CreateGatewayResponse(500, ContractExtensions.ToResponse(ex.Message));
            }
        }

        private static APIGatewayProxyResponse CreateGatewayResponse(int httpCode, BaseResponseContract response)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = httpCode,
                Body = JsonConvert.SerializeObject(response, new JsonSerializerSettings
                {
                    Culture = System.Globalization.CultureInfo.InvariantCulture,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    Converters = { new CanonicalUtcDateTimeConverter() }
                }),
                Headers = new Dictionary<string, string>
                {
                    {"Content-Type", "application/json"}
                }
            };
        }

        private static object? DeserializeFunctionInput(string json)
        {
            // we try SQS first — SQS events have a "Records" array
            var sqsEvent = JsonConvert.DeserializeObject<SQSEvent>(json);
            if (sqsEvent?.Records != null && sqsEvent.Records.Count > 0)
                return sqsEvent;

            // we fall back to API Gateway
            var apiEvent = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(json);
            return apiEvent;
        }

        private static async Task<string> ReadStreamAsStringAsync(Stream stream)
        {
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        private sealed class CanonicalUtcDateTimeConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(DateTime) || objectType == typeof(DateTime?);
            }

            public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
            {
                if (value is null)
                {
                    writer.WriteNull();
                    return;
                }

                writer.WriteValue(DateTimeHelper.FormatUtc((DateTime)value));
            }

            public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null && objectType == typeof(DateTime?))
                    return null;

                if (reader.TokenType != JsonToken.String)
                    throw new JsonSerializationException("A UTC timestamp must be a JSON string.");

                return DateTimeHelper.ParseUtc((string)reader.Value!);
            }
        }
    }
}