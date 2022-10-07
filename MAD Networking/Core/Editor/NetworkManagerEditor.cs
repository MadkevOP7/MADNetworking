using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MADNetworking;
using System.Linq;

namespace MADNetworking
{
    [CustomEditor(typeof(NetworkManager))]
    public class NetworkManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Register All Network Objects"))
            {
                NetworkIdentity[] identities = FindObjectsOfType<NetworkIdentity>();
                NetworkManager.Instance.networkIdentities = identities.ToList();
                for(uint i=0; i < identities.Length; i++)
                {
                    identities[i].netId = i;
                }
            }
        }
    }
}
