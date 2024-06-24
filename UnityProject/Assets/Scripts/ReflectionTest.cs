using System;
using System.Collections.Generic;
using RojoinNetworkSystem;
using UnityEngine;

namespace DefaultNamespace
{
    public class ReflectionTest : MonoBehaviour
    {
        private NetworkSystem _networkSystem = new NetworkSystem();
        [SerializeField] private int onwner = 1;
        [SerializeField] private List<GameObject> objectsToAdd;
        [SerializeField] private ClientNetManager _clientNetManager;
        [SerializeField] private ClassA _classA;

        private void Awake()
        {
            _networkSystem.StartNetworkSystem(onwner, DebugConsoleMessage);
            _networkSystem.dataToSend += SendCustomData;
            _clientNetManager.OnValueDataReceived.AddListener(WriteData);
        }

        [ContextMenu("Add Objects")]
        private void AddObjects()
        {
            foreach (GameObject VARIABLE in objectsToAdd)
            {
                foreach (var components in VARIABLE.GetComponents(typeof(Component)))
                {
                    Debug.Log(components.name);
                    _networkSystem.AddNetObject(VARIABLE);
                }
            }

            _networkSystem.AddNetObject(_classA);
        }

        private void DebugConsoleMessage(string obj)
        {
            Debug.Log("RojoinNetworkLib:" + obj);
        }

        private void WriteData(object arg0, List<int> arg1, int arg2)
        {
            Debug.Log("Event To Write data Called");
            Debug.Log($"Data:{arg0.GetType()}:{arg0}. Route {arg1}. ObjetNumber: {arg2}.");
            _networkSystem.ChangeExternalNetObjects(arg0, arg1, arg2);
        }

        private void Update()
        {
            _networkSystem.CheckNetObjectsToSend();
        }

        private void OnDisable()
        {
            _networkSystem.dataToSend -= SendCustomData;
            _networkSystem.consoleMessage -= DebugConsoleMessage;
            _clientNetManager.OnValueDataReceived.RemoveListener(WriteData);
        }

        private void SendCustomData(byte[] obj)
        {
            Debug.Log($"DeseializedMessage Lib:{BitConverter.ToSingle(obj, 32)}");
            _clientNetManager.SendToServer(obj);
        }
    }
}