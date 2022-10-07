using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MADNetworking;
namespace MADNetworking
{
    [Serializable]
    public class VectorThree
    {
        [SerializeField]
        public float x;

        [SerializeField]
        public float y;

        [SerializeField]
        public float z;

        public VectorThree(Vector3 vector3)
        {
            this.x = vector3.x;
            this.y = vector3.y;
            this.z = vector3.z;
        }
        public VectorThree(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public Vector3 ToVector3(VectorThree v)
        {
            return new Vector3(v.x, v.y, v.z);
        }

        public VectorThree ToVectorThree(Vector3 v)
        {
            return new VectorThree(v.x, v.y, v.z);
        }
    }
}

