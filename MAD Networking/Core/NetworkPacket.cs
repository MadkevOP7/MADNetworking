using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace MADNetworking
{
    [Serializable]
    public class NetworkPacket
    {
        [SerializeField]
        public uint netId;

        [SerializeField]
        public enum Type
        {
            Attribute,
            OpCode,
            Transform,
            SpawnPrefab
        }

        [SerializeField]
        public Type type;

        [SerializeField]
        public enum OPCode
        {
            OnStartClient,
            OnStopClient,
        }

        [SerializeField]
        public OPCode opCode;

        [SerializeField]
        public enum Attribute
        {
            ClientRPC,
            TargetRPC //[TODO]Implement
        }

        [SerializeField]
        public Attribute attribute;

        [SerializeReference]
        public NetworkAttributeData attributeData = null;

        [SerializeReference]
        public NetworkTransformData transformData = null;

        [SerializeReference]
        public NetworkPrefabData prefabData = null;

        //Targetting client, avoiding client [0] = no assignment
        [SerializeField]
        public uint requestClientId = 0; //Client ID of client that sent this packet

        //Targetting client, avoiding client
        [SerializeField]
        public uint targetClientId = 0; //Client ID of client that this packet targets

    }
}

