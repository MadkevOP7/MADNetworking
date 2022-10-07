using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MADNetworking;
using System;
using System.Reflection;
[Serializable]
public class NetworkAttributeData
{
    [SerializeField]
    public uint netId;

    [SerializeField]
    public string methodName;

    [SerializeField]
    public string varName;

    [SerializeReference]
    public List<NetworkParameterData> parameters = null;

    [SerializeField]
    public string componentName;

    [SerializeField]
    public bool includeOwner;
}

[Serializable]
public class NetworkParameterData
{
    [SerializeField]
    public uint netId;

    [SerializeField]
    public string componentName;

    [SerializeField]
    public enum ValueType
    {
        Reference,
        String,
        Int,
        UInt,
        Float
    }

    [SerializeField]
    public ValueType valueType;

    [SerializeField]
    public string sVal;

    [SerializeField]
    public int iVal;

    [SerializeField]
    public uint uiVal;

    [SerializeField]
    public float fVal;

}