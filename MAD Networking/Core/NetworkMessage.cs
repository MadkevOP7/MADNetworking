using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using MADNetworking;
namespace MADNetworking
{
    [Serializable]
    public class NetworkMessage
    {
        [SerializeReference]
        public List<NetworkPacket> packets = new List<NetworkPacket>();

        public NetworkMessage(List<NetworkPacket> packets)
        {
            //Shallow copy but new list is constructed so NM can clear reference
            this.packets = packets.ToList();
        }
    }
}

