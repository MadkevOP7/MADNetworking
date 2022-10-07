using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MADNetworking;

namespace MADNetworking
{
    [RequireComponent(typeof(NetworkIdentity))]
    public class NetworkTransform : NetworkBehaviour
    {
        private Transform _transform;
        private NetworkIdentity networkIdentity;
        [Header("Sync Settings")]
        public bool syncPosition;
        public bool syncRotation;
        public bool syncScale;

        [Range(0.1f, 1f)]
        public float syncDetectionDifference = 0.25f;
        [Range(0.1f, 1f)]
        public float syncRate = 0.3f;
        [Tooltip("If set to true, only client that owns this object can change the transform. ex. Player A can only change Player A's transform")]
        public bool requireAuthority;
        private bool inSyncOperation;
        Coroutine syncTimeOut;
        private void Awake()
        {
            _transform = GetComponent<Transform>();
            networkIdentity = GetComponent<NetworkIdentity>();
        }

        private Vector3 lastPosition;
        private Vector3 lastRotation;
        private Vector3 lastScale;

        private void Start()
        {
            lastPosition = transform.position;
            lastRotation = transform.eulerAngles;
            lastScale = transform.localScale;
            InvokeRepeating("Sync", 0, syncRate);
        }

        //Detects if position, rotation, or scale has changed
        bool TransformChanged()
        {
            bool changed = Vector3.Distance(transform.position, lastPosition) > syncDetectionDifference || Vector3.Distance(transform.eulerAngles, lastRotation) > syncDetectionDifference || Vector3.Distance(transform.localScale, lastScale) > syncDetectionDifference ? true : false;
            if (changed)
            {
                lastPosition = transform.position;
                lastRotation = transform.eulerAngles;
                lastScale = transform.localScale;
            }
            return changed;
        }

        void Sync()
        {
            if (requireAuthority && !networkIdentity.isLocalPlayer) return;
            if (networkIdentity.isDirty || inSyncOperation) return;
            if (!TransformChanged()) return;
            networkIdentity.SetDirty(true);
            NetworkPacket p = new NetworkPacket();
            NetworkTransformData d = new NetworkTransformData();

            if (syncPosition)
            {
                d.position = new VectorThree(_transform.position);
            }

            if (syncRotation)
            {
                d.rotation = new VectorThree(_transform.eulerAngles);
            }

            if (syncScale)
            {
                d.scale = new VectorThree(_transform.localScale);
            }

            p.netId = networkIdentity.netId;
            p.type = NetworkPacket.Type.Transform;
            p.transformData = d;
            NetworkManager.Instance.AddNetworkPacket(p, networkIdentity);

            if (syncTimeOut != null)
            {
                StopCoroutine(syncTimeOut);
            }
            syncTimeOut = StartCoroutine(SyncTimeOut());
        }

        public void SyncUpdate(NetworkTransformData data)
        {
            if(data.position != null)
            {
                SyncPosition(data.position);
            }

            if (data.rotation != null)
            {
                SyncRotation(data.rotation);
            }

            if (data.scale != null)
            {
                SyncScale(data.scale);
            }
            inSyncOperation = false;
        }
        void SyncPosition(VectorThree position)
        {
            StartCoroutine(InterpolatePosition(position.ToVector3(position)));
        }

        IEnumerator InterpolatePosition(Vector3 position)
        {
            for(float i=0; i < 0.2f; i += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(transform.position, position, i);
            }

            yield return new WaitForEndOfFrame();
        }

        void SyncRotation(VectorThree rotation)
        {
            StartCoroutine(InterpolateRotation(rotation.ToVector3(rotation)));
        }

        IEnumerator InterpolateRotation(Vector3 rotation)
        {
            for (float i = 0; i < 0.2f; i += Time.deltaTime)
            {
                transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, rotation, i);
            }

            yield return new WaitForEndOfFrame();
        }

        void SyncScale(VectorThree scale)
        {
            transform.localScale = scale.ToVector3(scale);
        }

        IEnumerator SyncTimeOut()
        {
            yield return new WaitForSeconds(5f);
            inSyncOperation = false;
        }

    }

}


