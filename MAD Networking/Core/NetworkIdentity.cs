using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MADNetworking;
namespace MADNetworking
{
    public class NetworkIdentity : MonoBehaviour
    {
        public int prefabId = -1; //-1 means no prefabId, as prefabId is uint
        public uint netId;
        public bool isDirty { get; set; }
        public bool isLocalPlayer;
        public void SetDirty(bool state)
        {
            isDirty = state;
        }
    }

}

