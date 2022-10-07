using System;
namespace MADNetworking.AWS.DataContainers
{
    public class NetworkResponse
    {
        public string logMessage { get; set; }
        public enum Status
        {
            success,
            failed
        }

        public Status status;
    }
}

