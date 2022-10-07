using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using MADNetworking;
using NativeWebSocket;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using System.Reflection;
using Random = UnityEngine.Random;

namespace MADNetworking
{
    public class NetworkManager : MonoBehaviour
    {
        #region Singleton Setup & Initialization
        public uint clientId;
        [Header("Initialization")]
        public bool autoStartManager = true;
        public bool registerAllNetworkObjectsOnAwake = true;
        [Header("Prefabs & Spawning")]
        public GameObject playerPrefab;
        public List<GameObject> registeredPrefabs = new List<GameObject>();
        [Header("Sync Settings")]
        [Range(0.1f, 5f)]
        public float syncInterval = 0.3f;
        public static NetworkManager Instance { get; private set; }
        private void Awake()
        {
            //Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }
            OnInitialize();
        }

        void OnInitialize()
        {
            //Generate client id
            clientId = GenerateClientID();
            if (playerPrefab)
            {
                registeredPrefabs.Add(playerPrefab);
            }

            if (registerAllNetworkObjectsOnAwake)
            {
                ScanSceneForAllNetworkObjects();
            }


            RegisterAllNetworkObjects();

        }

        public void OnStartManager()
        {
            //Call OnStartClient
            SendOnStartClientCall();

            if (playerPrefab)
            {
                NetworkSpawn(playerPrefab, false);
            }
        }

        public void ScanSceneForAllNetworkObjects()
        {
            NetworkIdentity[] identities = FindObjectsOfType<NetworkIdentity>();
            networkIdentities = identities.ToList();
            for (uint i = 0; i < identities.Length; i++)
            {
                identities[i].netId = i;
            }
        }

        public void RegisterAllNetworkObjects()
        {
            foreach (NetworkIdentity id in networkIdentities)
            {
                netObjects.Add(id.netId, id);
            }
        }

        public uint GenerateClientID()
        {
            return (uint) (Mathf.Abs(Random.Range(1, int.MaxValue / 2 - 1) - (System.DateTime.Now.Second + System.DateTime.Now.Minute % System.DateTime.Now.Second))) + 1;
        }

        void SendOnStartClientCall()
        {
            NetworkPacket p = new NetworkPacket();
            p.type = NetworkPacket.Type.OpCode;
            p.opCode = NetworkPacket.OPCode.OnStartClient;
            AddNetworkPacket(p);
        }
        #endregion

        #region Websocket
        [Header("Websocket")]
        [Tooltip("Websocket api execution url")]
        public string websocketURL = "wss://mhube5qtuj.execute-api.us-west-1.amazonaws.com/Prod";

        //Private
        private WebSocket websocket;

        #endregion

        #region Initialization
        private void Start()
        {
            if (autoStartManager)
            {
                StartManager();
            }
        }

        public void StartManager()
        {
            Debug.Log("MAD Networking\nDiscord: https://discord.gg/jAaQ5Gzyz9\n\nStarting Websocket");
            InitializeSocket();
            OnStartManager();
            InvokeRepeating("OnNetworkTick", 0, syncInterval);
        }

        async void InitializeSocket()
        {
            websocket = new WebSocket(websocketURL);
            websocket.OnOpen += OnStartConnection;
            websocket.OnClose += OnStopConnection;
            websocket.OnMessage += OnMessage;
            await websocket.Connect();
        }
        #endregion

        #region Core
        public List<NetworkIdentity> networkIdentities = new List<NetworkIdentity>();
        public Dictionary<uint, NetworkIdentity> netObjects = new Dictionary<uint, NetworkIdentity>();

        //GameObjects spwned by NetworkSpawn(), key is netID
        public Dictionary<uint, GameObject> spawned = new Dictionary<uint, GameObject>();
        public List<NetworkIdentity> local_spawned = new List<NetworkIdentity>(); //For sync spawn to later connected clients

        //Websocket per frame required
        private void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            websocket.DispatchMessageQueue();
#endif
        }

        void OnNetworkTick()
        {
            if (websocket == null) return;
            SendMessage();
        }
      
        private List<NetworkPacket> packets_cache = new List<NetworkPacket>();
        private List<NetworkIdentity> identities_cache = new List<NetworkIdentity>();
        public void OnStartConnection()
        {
            Debug.Log("Websocket connected");
        }

        public void OnStopConnection(WebSocketCloseCode e)
        {

        }

        public void OnMessage(byte[] bytes)
        {
            NetworkMessage message = (NetworkMessage)DeserializeObject(System.Text.Encoding.UTF8.GetString(bytes));
            //Process per packet
            //Debug.Log("Received packets: " + message.packets.Count);
            foreach(NetworkPacket p in message.packets)
            {
                if (p.targetClientId != 0 && p.targetClientId != clientId) return;
                switch (p.type)
                {
                    case NetworkPacket.Type.Attribute:
                        switch (p.attribute)
                        {
                           case NetworkPacket.Attribute.ClientRPC:
                                if (!p.attributeData.includeOwner && p.requestClientId == clientId) return;
                                NetworkBehaviour networkBehaviour = (NetworkBehaviour) netObjects[p.attributeData.netId].GetComponent(p.attributeData.componentName);
                                networkBehaviour.InvokeMethod(p.attributeData.componentName, p.attributeData.methodName, p.attributeData.parameters);
                                
                                break;
                        }
                        break;
                    case NetworkPacket.Type.OpCode:
                        switch (p.opCode)
                        {
                            case NetworkPacket.OPCode.OnStartClient:
                                OnStartClient(p.requestClientId);
                                break;

                            case NetworkPacket.OPCode.OnStopClient:
                                
                                break;
                        }
                        break;
                    case NetworkPacket.Type.Transform:
                        if (p.requestClientId == clientId) return; //Prevent object moving again on local player
                        NetworkTransformData networkTransformData = p.transformData;
                        netObjects[p.netId]?.GetComponent<NetworkTransform>()?.SyncUpdate(networkTransformData);
                        break;

                    case NetworkPacket.Type.SpawnPrefab:
                        NetworkPrefabData networkPrefabData = p.prefabData;
                        Spawn(networkPrefabData, networkPrefabData.prefabId, networkPrefabData.assignedNetId, p.requestClientId);
                        break;
                        
                  
                }
            }

        }

        public void OnStartClient(uint clientID)
        {
            Debug.Log("New client: " + clientID);
            if(clientID != clientId)
            {
                SyncSpawned(clientID);
            }
        }

        public void OnStopClient()
        {
            Debug.Log("stopped client!");
        }
        #endregion

        #region Messaging
        [Serializable]
        public class NetworkJsonData
        {
            public string action = "echo";
            public string data = "default";

            public NetworkJsonData(string action, string data)
            {
                this.action = action;
                this.data = data;
            }
        }

        public async void SendMessage()
        {
            if (websocket.State != WebSocketState.Open) return;
            if (packets_cache.Count == 0) return;
            NetworkMessage message = new NetworkMessage(packets_cache);
            for (int i = packets_cache.Count - 1; i >= 0; --i)
            {
                identities_cache[i]?.SetDirty(false);
                packets_cache?.RemoveAt(i);
                identities_cache?.RemoveAt(i);
            }

            string messageData = SerializeObject(message);
            NetworkJsonData jdata = new NetworkJsonData("echo", messageData);
            await websocket.SendText(JsonUtility.ToJson(jdata));
        }

        public void AddNetworkPacket(NetworkPacket p, NetworkIdentity networkIdentity)
        {
            packets_cache.Add(p);
            identities_cache.Add(networkIdentity);
        }

        public void AddNetworkPacket(NetworkPacket p)
        {
            p.requestClientId = clientId;
            packets_cache.Add(p);
            identities_cache.Add(null);
        }

        #region Serialization
        public string SerializeObject(object o)
        {
            if (!o.GetType().IsSerializable)
            {
                return null;
            }

            using (MemoryStream stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, o);
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        public object DeserializeObject(string str)
        {
            byte[] bytes = Convert.FromBase64String(str);

            using (MemoryStream stream = new MemoryStream(bytes))
            {
                return new BinaryFormatter().Deserialize(stream);
            }
        }
        #endregion
        #endregion

        #region Life time
        private async void OnApplicationQuit()
        {
            await websocket.Close();
        }
        #endregion

        #region Core Public Functions
        #region Spawning
        public uint GenerateNetId()
        {
            return (uint) Random.Range(500, int.MaxValue);
        }

        public int GetRegisteredSpawnPrefabID(GameObject gameObject)
        {
            for(int i=0; i<registeredPrefabs.Count; i++)
            {
                if (registeredPrefabs[i] == gameObject)
                {
                    return i;
                }
            }
            return -1;
        }

        //Local
        void Spawn(NetworkPrefabData prefabData, uint prefabID, uint netID, uint requestClientId)
        {
            try
            {
                GameObject go = Instantiate(registeredPrefabs[(int)prefabID].gameObject);

                //Update transform
                if (prefabData.position!=null)
                {
                    go.transform.position = prefabData.position.ToVector3(prefabData.position);
                }

                if (prefabData.rotation != null)
                {
                    go.transform.eulerAngles = prefabData.rotation.ToVector3(prefabData.rotation);
                }

                if (prefabData.scale != null)
                {
                    go.transform.localScale = prefabData.scale.ToVector3(prefabData.scale);
                }

                //Update NetworkIdentity
                NetworkIdentity identity = go.GetComponent<NetworkIdentity>();
                if (identity == null)
                {
                    identity = go.AddComponent<NetworkIdentity>();
                }

                identity.netId = netID;
                identity.prefabId = (int) prefabID;
                identity.isLocalPlayer = requestClientId == clientId ? true : false;
                networkIdentities.Add(identity);

                //Update dictionaries
                netObjects.Add(netID, identity);
                spawned.Add(netID, go);
                if(requestClientId == clientId)
                {
                    local_spawned.Add(identity);
                }

            }
            catch(Exception e)
            {
                Debug.LogError("NetworkManager: Spawn prefab error:\n" + e.Message + "\n" + e.StackTrace);
            }
            

        }

        public void NetworkSpawn(uint prefabId, uint netId, uint targetClient)
        {
            
            NetworkPrefabData prefabData = new NetworkPrefabData();
            prefabData.prefabId = prefabId;
            //Assign the netID to be the new future index
            prefabData.assignedNetId = netId;
            NetworkPacket p = new NetworkPacket();
            p.type = NetworkPacket.Type.SpawnPrefab;
            p.prefabData = prefabData;
            p.targetClientId = targetClient;
            AddNetworkPacket(p);
        }

        public void NetworkSpawn(GameObject gameObject, uint targetClient, bool setTransform)
        {
            //Check if the object is registered in the list by id
            int spawnID = GetRegisteredSpawnPrefabID(gameObject);
            if (spawnID == -1)
            {
                Debug.LogError("NetworkManager: GameObject " + gameObject.name + " cannot be spawned, please register to Network Manager");
                return;
            }

            NetworkPrefabData prefabData = new NetworkPrefabData();
            prefabData.prefabId = (uint)spawnID;
            if (setTransform)
            {
                prefabData.position = new VectorThree(gameObject.transform.position);
                prefabData.rotation = new VectorThree(gameObject.transform.eulerAngles);
                prefabData.scale = new VectorThree(gameObject.transform.localScale);
            }
            
            //Current generate net id method has limitations and rare chance for duplication if spawn large amounts of objects
            prefabData.assignedNetId = GenerateNetId();
            NetworkPacket p = new NetworkPacket();
            p.type = NetworkPacket.Type.SpawnPrefab;
            p.prefabData = prefabData;
            p.targetClientId = targetClient;


            AddNetworkPacket(p);
        }

        public void NetworkSpawn(GameObject gameObject, bool setTransform)
        {
            NetworkSpawn(gameObject, 0, setTransform);
        }

        //Spawn previously spawned network objects to newly joined client
        public void SyncSpawned(uint targetClient)
        {
            foreach(NetworkIdentity n in local_spawned)
            {
                NetworkSpawn((uint) n.prefabId, n.netId, targetClient);
            }
        }
        #endregion
        #endregion
    }
}

