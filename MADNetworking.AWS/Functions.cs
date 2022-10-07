using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

using Amazon.Runtime;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;

using MADNetworking.AWS.DataHandler;
using MADNetworking.AWS.DataContainers;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
//using Newtonsoft.Json;
// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MADNetworking.AWS;

public class Functions
{
    public const string ConnectionIdField = "connectionId";
    private const string TABLE_NAME_ENV = "TABLE_NAME";

    /// <summary>
    /// DynamoDB table used to store the open connection ids. More advanced use cases could store logged on user map to their connection id to implement direct message chatting.
    /// </summary>
    string ConnectionMappingTable { get; }

    /// <summary>
    /// DynamoDB service client used to store and retieve connection information from the ConnectionMappingTable
    /// </summary>
    IAmazonDynamoDB DDBClient { get; }

    /// <summary>
    /// Factory func to create the AmazonApiGatewayManagementApiClient. This is needed to created per endpoint of the a connection. It is a factory to make it easy for tests
    /// to moq the creation.
    /// </summary>
    Func<string, IAmazonApiGatewayManagementApi> ApiGatewayManagementApiClientFactory { get; }


    /// <summary>
    /// Default constructor that Lambda will invoke.
    /// </summary>
    public Functions()
    {
        DDBClient = new AmazonDynamoDBClient();

        // Grab the name of the DynamoDB from the environment variable setup in the CloudFormation template serverless.template
        if (Environment.GetEnvironmentVariable(TABLE_NAME_ENV) == null)
        {
            throw new ArgumentException($"Missing required environment variable {TABLE_NAME_ENV}");
        }

        ConnectionMappingTable = Environment.GetEnvironmentVariable(TABLE_NAME_ENV) ?? "";

        this.ApiGatewayManagementApiClientFactory = (Func<string, AmazonApiGatewayManagementApiClient>)((endpoint) =>
        {
            return new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig
            {
                ServiceURL = endpoint
            });
        });
    }

    /// <summary>
    /// Constructor used for testing allow tests to pass in moq versions of the service clients.
    /// </summary>
    /// <param name="ddbClient"></param>
    /// <param name="apiGatewayManagementApiClientFactory"></param>
    /// <param name="connectionMappingTable"></param>
    public Functions(IAmazonDynamoDB ddbClient, Func<string, IAmazonApiGatewayManagementApi> apiGatewayManagementApiClientFactory, string connectionMappingTable)
    {
        this.DDBClient = ddbClient;
        this.ApiGatewayManagementApiClientFactory = apiGatewayManagementApiClientFactory;
        this.ConnectionMappingTable = connectionMappingTable;
    }

    public async Task<APIGatewayProxyResponse> OnConnectHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            var connectionId = request.RequestContext.ConnectionId;
            context.Logger.LogInformation($"ConnectionId: {connectionId}");

            NetworkConnectionHandler c = new NetworkConnectionHandler(DDBClient);
            await c.StartMatchMakingConnection(request.RequestContext.ConnectionId, "open");
            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = "Successfully established connection"
            };
        }
        catch (Exception e)
        {
            context.Logger.LogInformation("Error connecting: " + e.Message);
            context.Logger.LogInformation(e.StackTrace);
            return new APIGatewayProxyResponse
            {
                StatusCode = 500,
                Body = $"Failed to connect: {e.Message}"
            };
        }
    }


    public async Task<APIGatewayProxyResponse> SendMessageHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {

        try
        {
         
            var domainName = request.RequestContext.DomainName;
            var stage = request.RequestContext.Stage;
            var endpoint = $"https://{domainName}/{stage}";
            context.Logger.LogInformation($"API Gateway management endpoint: {endpoint}");

            JsonElement dataProperty;
            JsonDocument message = JsonDocument.Parse(request.Body);
            if (!message.RootElement.TryGetProperty("data", out dataProperty) || dataProperty.GetString() == null)
            {
                context.Logger.LogInformation("Failed to find data element in JSON document");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
            }

            var data = dataProperty.GetString() ?? "";
            var stream = new MemoryStream(UTF8Encoding.UTF8.GetBytes(data));

            //Gets all connections and manually search for row where context connectionId is in
            var scanRequest = new ScanRequest
            {
                TableName = ConnectionMappingTable,
            };

            var connID = request.RequestContext.ConnectionId;
            var scanResponse = await DDBClient.ScanAsync(scanRequest);

            var apiClient = ApiGatewayManagementApiClientFactory(endpoint);

            foreach (var item in scanResponse.Items)
            {
                if (item["connectionId"].S == connID || item["clientConnections"].SS.Contains(connID))
                {
                    item["clientConnections"].SS.Add(item["connectionId"].S);
                    List<string> allClients = item["clientConnections"].SS;

                    foreach(var client in allClients)
                    {
                        var postConnectionRequest = new PostToConnectionRequest
                        {
                            ConnectionId = client,
                            Data = stream
                        };

                        try
                        {
                            stream.Position = 0;
                            await apiClient.PostToConnectionAsync(postConnectionRequest);
                        }
                        catch (AmazonServiceException e)
                        {
                            if (e.StatusCode == HttpStatusCode.Gone)
                            {
                                var c = new NetworkConnectionHandler(DDBClient);
                                await c.CloseConnection(postConnectionRequest.ConnectionId);
                            }
                            else
                            {
                                context.Logger.LogInformation($"Error posting message to {postConnectionRequest.ConnectionId}: {e.Message}");
                                context.Logger.LogInformation(e.StackTrace);
                            }
                        }
                    }
                    break;
                }      
            }

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = "Data echo success"
            };
        }
        catch (Exception e)
        {
            context.Logger.LogInformation("Error disconnecting: " + e.Message);
            context.Logger.LogInformation(e.StackTrace);
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Failed to send message: {e.Message}"
            };
        }
    }

    public async Task<APIGatewayProxyResponse> OnDisconnectHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        NetworkConnectionHandler c = new NetworkConnectionHandler(DDBClient);
        var connID = request.RequestContext.ConnectionId;
        var response = await c.CloseConnection(connID);
        if (response.status == DataContainers.NetworkResponse.Status.success)
        {
            return new APIGatewayProxyResponse { StatusCode = 200, Body = response.logMessage };
        }

        return new APIGatewayProxyResponse { StatusCode = 500, Body = response.logMessage};

    }
}