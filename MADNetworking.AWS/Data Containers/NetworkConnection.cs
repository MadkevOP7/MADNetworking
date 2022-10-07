using System;
using System.Collections.Generic;
using System.Text;
namespace MADNetworking.AWS.DataContainers
{
    public class NetworkConnection
    {
        //open, closed, private
        public string connectionMode { get; set; } 

        public uint maxConnections { get; set; }

        public string connectionID { get; set; }
    }
}

