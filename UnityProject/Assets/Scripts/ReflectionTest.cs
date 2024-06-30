using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

[NetExtensionClass]
public static class FieldInfoExtensions
{
    [NetExtensionMethod(typeof(Vector3))]
    public static List<MessageData> GetFields(this Vector3 vector3)
    {
        List<MessageData> output = new List<MessageData>();
        output.Add(new MessageData(vector3.GetType().GetField(nameof(vector3.x)), 0, MessageFlags.CheckSum));
        output.Add(new MessageData(vector3.GetType().GetField(nameof(vector3.y)), 1, MessageFlags.CheckSum));
        output.Add(new MessageData(vector3.GetType().GetField(nameof(vector3.z)), 2, MessageFlags.CheckSum));
        return output;
    }

    [NetExtensionMethod(typeof(Quaternion))]
    public static List<MessageData> GetFields(this Quaternion quaternion)
    {
        List<MessageData> output = new List<MessageData>();
        output.Add(new MessageData(quaternion.GetType().GetField(nameof(quaternion.x)), 0, MessageFlags.CheckSum));
        output.Add(new MessageData(quaternion.GetType().GetField(nameof(quaternion.y)), 1, MessageFlags.CheckSum));
        output.Add(new MessageData(quaternion.GetType().GetField(nameof(quaternion.z)), 2, MessageFlags.CheckSum));
        output.Add(new MessageData(quaternion.GetType().GetField(nameof(quaternion.w)), 3, MessageFlags.CheckSum));
        return output;
    }


    [NetExtensionMethod(typeof(Color))]
    public static List<MessageData> GetFields(this Color color)
    {
        List<MessageData> output = new List<MessageData>();
        output.Add(new MessageData(color.GetType().GetField(nameof(color.r)), 0, MessageFlags.CheckSum));
        output.Add(new MessageData(color.GetType().GetField(nameof(color.g)), 1, MessageFlags.CheckSum));
        output.Add(new MessageData(color.GetType().GetField(nameof(color.b)), 2, MessageFlags.CheckSum));
        output.Add(new MessageData(color.GetType().GetField(nameof(color.a)), 3, MessageFlags.CheckSum));
        return output;
    }
}