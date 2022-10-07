using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MADNetworking;

namespace MADNetworking
{
    public class ExampleNetworkBehaviorScript : NetworkBehaviour
    {
        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                NetworkInvoke("DemoFunction", true);
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                string message = "hello, I'm client " + NetworkManager.Instance.clientId;
                NetworkInvoke("DemoFunction2", new object[] { message }, true);
            }

            if (Input.GetKeyDown(KeyCode.K))
            {
                string message = "hello, I'm client " + NetworkManager.Instance.clientId;
                NetworkInvoke("DemoFunction3", new object[] { message }, false);
            }

        }

        //Have each client introduce themself!
        public void DemoFunction()
        {
            Debug.Log("Hello, I'm client: " + NetworkManager.Instance.clientId);
        }

        //Prints out hello and the invoking client's id on all clients
        public void DemoFunction2(string message)
        {
            Debug.Log("Message: " + message);
        }

        //Prints out hello and the invoking client's id on all clients except the invoking client
        public void DemoFunction3(string message)
        {
            Debug.Log("Message: " + message);
        }
    }
}

