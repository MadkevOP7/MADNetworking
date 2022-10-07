using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MADNetworking;

namespace MADNetworking
{
    [Serializable]
    public class NetworkPrefabData
    {
        [SerializeReference]
        public VectorThree position;

        [SerializeReference]
        public VectorThree rotation;

        [SerializeReference]
        public VectorThree scale;

        public uint assignedNetId; //Assign the same netId to all clients

        public uint prefabId; //NM can retrieve object using this id
    }
}

