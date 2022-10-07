using System;
using System.Threading.Tasks;
using MADNetworking.AWS.DataContainers;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Runtime;

namespace MADNetworking.AWS.DataHandler
{
    public class NetworkConnectionHandler
    {
        private readonly IAmazonDynamoDB dynamoDB;
        private const string dbName = "connectionTable";
        const string SNull = "NULL";
        public NetworkConnectionHandler(IAmazonDynamoDB dynamoDB)
        {
            this.dynamoDB = dynamoDB;
        }

        public async Task<bool> StartMatchMakingConnection(string connectionID, string connectionMode)
        {
            string connID = await GetFirstOpenConnection();

            if (connID == SNull)
            {
                return await CreateConnection(connectionID, connectionMode);
            }
            Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>
            {
                {"connectionId", new AttributeValue{S = connID} }
            };
    
            Dictionary<string, AttributeValueUpdate> update = new Dictionary<string, AttributeValueUpdate>();

            update["clientConnections"] = new AttributeValueUpdate
            {
                Action = AttributeAction.ADD,
                Value = new AttributeValue { SS = new List<string> { connectionID } }
            };

            update["connectionMode"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue("closed")
            };

            var request = new UpdateItemRequest
            {
                TableName = dbName,
                Key = key,
                AttributeUpdates = update
            };

            var response = await dynamoDB.UpdateItemAsync(request);
            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }

        public async Task<bool> CreateConnection(string connectionID, string connectionMode)
        {
            var request = new PutItemRequest
            {
                TableName = dbName,
                Item = new Dictionary<string, AttributeValue>
                {
                    { "connectionId", new AttributeValue(connectionID) },
                    { "clientConnections", new AttributeValue{SS =  new List<string>{"empty"} } },
                    { "connectionMode", new AttributeValue(connectionMode) }
                }
            };
            var response = await dynamoDB.PutItemAsync(request);
            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }

        public async Task<NetworkResponse> CloseConnection(string connectionID)
        {
            try
            {
                var dltRequest = new DeleteItemRequest
                {
                    TableName = dbName,
                    Key = new Dictionary<string, AttributeValue>
                {
                    {"connectionId", new AttributeValue{S = connectionID} }
                }
                };
                var response = await dynamoDB.DeleteItemAsync(dltRequest);
                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    return new NetworkResponse { status = NetworkResponse.Status.success, logMessage = "Connection: " + connectionID + "has been closed" };
                }
                else
                {
                    return new NetworkResponse { status = NetworkResponse.Status.failed, logMessage = "Failed to close connection: " + connectionID + "HTTPStatusCode error"};
                }
            }
            catch (AmazonServiceException e)
            {
                return new NetworkResponse { status = NetworkResponse.Status.failed, logMessage = "Failed to close connection: " + connectionID + "\n Log:" + e.Message};
            }
        }

        public async Task<string> GetFirstOpenConnection()
        {
            var result = await dynamoDB.ScanAsync(new ScanRequest
            {
                TableName = dbName
            });
            var connections = new List<NetworkConnection>();

            if (result != null && result.Items != null)
            {
                foreach (var c in result.Items)
                {
                    c.TryGetValue("connectionMode", out var _connectionMode);
                    c.TryGetValue("connectionId", out var _connectionID);
                    if (_connectionMode != null && _connectionMode.S.Equals("open"))
                    {    
                        return _connectionID?.S;
                    }
                }
            }
            return "NULL";
        }
    }
}

