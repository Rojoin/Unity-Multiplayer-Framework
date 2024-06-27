
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
            // foreach (GameObject VARIABLE in objectsToAdd)
            // {
            //     foreach (var components in VARIABLE.GetComponents(typeof(Component)))
            //     {
            //         Debug.Log(components.name);
            //         _networkSystem.AddNetObject(VARIABLE);
            //     }
            // }

            _networkSystem.AddNetObject(_classA);
        }

        private void DebugConsoleMessage(string obj)
        {
            Debug.Log("RojoinNetworkLib:" + obj);
        }

        private void WriteData(byte[] data)
        {
            Debug.Log("Event To Write data Called");
          //  Debug.Log($"Data:{arg0.GetType()}:{arg0}. Route {arg1}. ObjetNumber: {arg2}.");
        //    _networkSystem.ChangeExternalNetObjects(arg0, arg1, arg2);
            _networkSystem.HandlerMessage(data);
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
          //  Debug.Log($"DeseializedMessage Lib:{BitConverter.ToSingle(obj, 32)}");
            _clientNetManager.SendToServer(obj);
        }
    }
    
}
public static class FieldInfoExtensions
{
    [NetExtensionMethod]
    public static List<MessageData> GetFields(this Vector3 vector3)
    {
        List<MessageData> output = new List<MessageData>();
        output.Add(new MessageData(vector3.GetType().GetField("x"), 0, MessageFlags.CheckSum));
        output.Add(new MessageData(vector3.GetType().GetField("y"), 1, MessageFlags.CheckSum));
        output.Add(new MessageData(vector3.GetType().GetField("z"), 2, MessageFlags.CheckSum));
        return output;
    }

    [NetExtensionMethod]
    public static List<MessageData> GetFields(this Quaternion quaternion)
    {
        List<MessageData> output = new List<MessageData>();
        output.Add(new MessageData(quaternion.GetType().GetField("x"), 0, MessageFlags.CheckSum));
        output.Add(new MessageData(quaternion.GetType().GetField("y"), 1, MessageFlags.CheckSum));
        output.Add(new MessageData(quaternion.GetType().GetField("z"), 2, MessageFlags.CheckSum));
        output.Add(new MessageData(quaternion.GetType().GetField("w"), 3, MessageFlags.CheckSum));
        return output;
    }


    [NetExtensionMethod]
    public static List<MessageData> GetFields(this Color color)
    {
        List<MessageData> output = new List<MessageData>();
        output.Add(new MessageData(color.GetType().GetField("r"), 0, MessageFlags.CheckSum));
        output.Add(new MessageData(color.GetType().GetField("g"), 1, MessageFlags.CheckSum));
        output.Add(new MessageData(color.GetType().GetField("b"), 2, MessageFlags.CheckSum));
        output.Add(new MessageData(color.GetType().GetField("a"), 3, MessageFlags.CheckSum));
        return output;
    }
}