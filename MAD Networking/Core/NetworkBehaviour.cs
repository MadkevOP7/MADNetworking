
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MADNetworking;
using System;
using System.Reflection;
using System.Data.Common;

namespace MADNetworking
{
    [RequireComponent(typeof(NetworkIdentity))]
    public class NetworkBehaviour : MonoBehaviour
    {

        //[Implement]
        public bool isLocalAuthority;

        #region Method Invoke
        //Switched to NetworkInvoke() instead of ClientRPC attribute as a work around to mono cecil
        //Invoke a method across all clients
        public void NetworkInvoke(string methodName, object[] funcParameters, bool includeOwner = true)
        {
            var method = this.GetType().GetMethod(methodName, (BindingFlags.Instance | BindingFlags.Public));
            NetworkPacket p = new NetworkPacket();
            p.type = NetworkPacket.Type.Attribute;
            p.attribute = NetworkPacket.Attribute.ClientRPC;
            NetworkAttributeData d = new NetworkAttributeData();
            d.netId = GetComponent<NetworkIdentity>().netId;
            d.methodName = methodName;
            List<NetworkParameterData> parameters = new List<NetworkParameterData>();
            //foreach (var parameter in method.GetParameters())
            //{
            //    NetworkParameterData pd = new NetworkParameterData();
            //    pd.netId = GetComponent(parameter.GetType()).GetComponent<NetworkIdentity>().netId;
            //    pd.componentName = parameter.GetType().ToString();
            //    parameters.Add(pd);
            //}
            if (funcParameters != null)
            {
                foreach (var parameter in funcParameters)
                {
                    NetworkParameterData pd = new NetworkParameterData();
                    pd.valueType = GetValueType(parameter);
                    switch (pd.valueType)
                    {
                        case NetworkParameterData.ValueType.Reference:
                            pd.netId = GetComponent(parameter.GetType()).GetComponent<NetworkIdentity>().netId;
                            pd.componentName = parameter.GetType().ToString();
                            break;

                        case NetworkParameterData.ValueType.Float:
                            pd.fVal = (float)parameter;
                            break;

                        case NetworkParameterData.ValueType.Int:
                            pd.iVal = (int)parameter;
                            break;

                        case NetworkParameterData.ValueType.UInt:
                            pd.uiVal = (uint)parameter;
                            break;

                        case NetworkParameterData.ValueType.String:
                            pd.sVal = (string)parameter;
                            break;
                    }

                    parameters.Add(pd);
                }
            }
            d.parameters = parameters;
            d.componentName = this.GetType().ToString();
            d.includeOwner = includeOwner;
            p.attributeData = d;
            NetworkManager.Instance.AddNetworkPacket(p);
        }

        public void NetworkInvoke(string methodName, bool includeOwner = true)
        {
            NetworkInvoke(methodName, null, includeOwner);
        }

        public void InvokeMethod(string componentName, string methodName, List<NetworkParameterData> parameters)
        {
            //Construct back parameters
            List<object> m_parameters = new List<object>();
            foreach (var p in parameters)
            {
                switch (p.valueType)
                {
                    case NetworkParameterData.ValueType.Reference:
                        GameObject go = NetworkManager.Instance.netObjects[p.netId].gameObject;
                        object obj = go.GetComponent(p.componentName);
                        m_parameters.Add(obj);
                        break;

                    case NetworkParameterData.ValueType.Float:
                        m_parameters.Add(p.fVal);
                        break;

                    case NetworkParameterData.ValueType.Int:
                        m_parameters.Add(p.iVal);
                        break;

                    case NetworkParameterData.ValueType.UInt:
                        m_parameters.Add(p.uiVal);
                        break;

                    case NetworkParameterData.ValueType.String:
                        m_parameters.Add(p.sVal);
                        break;
                }

            }
            this.GetType().GetMethod(methodName).Invoke(GetComponent(componentName), m_parameters.ToArray());
        }

        #endregion
        public NetworkParameterData.ValueType GetValueType(object obj)
        {
            Type t = obj.GetType();
            if (t == typeof(string))
            {
                return NetworkParameterData.ValueType.String;
            }
            else if (t == typeof(int))
            {
                return NetworkParameterData.ValueType.Int;
            }
            else if (t == typeof(uint))
            {
                return NetworkParameterData.ValueType.UInt;
            }
            else if (t == typeof(float))
            {
                return NetworkParameterData.ValueType.Float;
            }
            else
            {
                return NetworkParameterData.ValueType.Reference;
            }
        }

    }

    #region Attributes
    [AttributeUsage(AttributeTargets.Method)]
    public class ClientRPCAttribute : Attribute
    {
        public bool includeOwner = true; //Invoke on the owner again

    }
    #endregion


}

