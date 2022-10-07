using System;
using System.IO;
using System.Text;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using MADNetworking.AWS.DataContainers;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Amazon.ApiGatewayManagementApi;

using Amazon.ApiGatewayManagementApi.Model;
using Amazon.Runtime;

using static Amazon.RegionEndpoint;
using System.Net.Sockets;

namespace MADNetworking.AWS.DataHandler
{
    public class NetworkMessenger
    {
        private const string dbName = "connectionTable";
        private readonly IAmazonDynamoDB dynamoDB;
        private readonly NetworkConnectionHandler networkConnectionHandler;
        public NetworkMessenger(IAmazonDynamoDB dynamoDB)
        {
            this.dynamoDB = dynamoDB;
            networkConnectionHandler = new NetworkConnectionHandler(dynamoDB);
        }

        public async Task<NetworkResponse> SendMessage(string endPoint, MemoryStream stream, IEnumerable<string> conns)
        {
            AmazonWebServiceResponse response = new AmazonWebServiceResponse();
            try
            {
                
                LambdaLogger.Log("Received API End Point: " + endPoint);  
                var apiClient = new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig
                {
                    ServiceURL = endPoint
                });

                uint echoCount = 0;
                foreach (var conn in conns)
                {
                    var echoRequest = new PostToConnectionRequest
                    {
                        ConnectionId = conn,
                        Data = stream
                    };

                    try
                    {
                        response = await apiClient.PostToConnectionAsync(echoRequest);
                        echoCount++;

                    }

                    catch (AmazonServiceException e)
                    {
                        //Handle removal from dynamoDB table
                        if (e.StatusCode == HttpStatusCode.Gone)
                        {
                            await networkConnectionHandler.CloseConnection(conn);
                        }
                    }

                }
                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    return new NetworkResponse { status = NetworkResponse.Status.success };
                }
                return new NetworkResponse { status = NetworkResponse.Status.failed, logMessage = "Successfully echoed message to " + echoCount + " subscribed clients" };
            }
            catch (AmazonServiceException e)
            {
                return new NetworkResponse { status = NetworkResponse.Status.failed, logMessage = e.Message };
            }
        }

        //public async Task<NetworkResponse> EchoMessage(APIGatewayProxyRequest request)
        //{
        //    try
        //    {
        //        var scanRequest = new ScanRequest
        //        {
        //            TableName = dbName,
        //            ProjectionExpression = "connectionId"
        //        };
        //        var scanResponse = await dynamoDB.ScanAsync(scanRequest);
        //        List<string> conns = new List<string>();
        //        foreach (var item in scanResponse.Items)
        //        {
        //            var connID = item["connectionId"].S;
        //            conns.Add(connID);
        //        }
        //        return await SendMessage(request, conns);
        //    }
        //    catch(AmazonServiceException e)
        //    {
        //        return new NetworkResponse { status = NetworkResponse.Status.failed, logMessage = e.Message };
        //    }
        //}
    }
}

