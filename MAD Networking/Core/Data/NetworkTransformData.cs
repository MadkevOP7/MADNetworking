using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MADNetworking;

namespace MADNetworking
{
    [Serializable]
    public class NetworkTransformData
    {
        [SerializeReference]
        public VectorThree position;

        [SerializeReference]
        public VectorThree rotation;

        [SerializeReference]
        public VectorThree scale;

    }
}

