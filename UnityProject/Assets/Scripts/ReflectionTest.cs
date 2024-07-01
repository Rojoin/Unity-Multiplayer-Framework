using System;
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
            NetObjectFactory.Instance.NetworkSystem = _networkSystem;
            NetObjectFactory.Instance.dataToSend.AddListener(SendCustomData);
            _networkSystem.idToDelete += NetObjectFactory.Instance.DeleteGameObjects;
        }

        

        [ContextMenu("Add Objects")]
        private void AddObjects()
        {
            AskForNetObject aux = new AskForNetObject();
            aux.objectType = 0;
            aux.owner = onwner;
            aux.intanceID = 0;
            aux.parentId = -1;
            aux.pos = new System.Numerics.Vector3(1, 1, 1);
            aux.rot = new System.Numerics.Vector3(0, 0, 0);
            aux.scale = new System.Numerics.Vector3(1, 1, 1);

            NetGetObjectID messageToSend = new NetGetObjectID(aux);
            SendCustomData(messageToSend.Serialize());
            //_networkSystem.AddNetObject(_classA);
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
            NetObjectFactory.Instance.dataToSend.RemoveAllListeners();
            _networkSystem.idToDelete -= NetObjectFactory.Instance.DeleteGameObjects;
        }

        public void SendCustomData(byte[] obj)
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

//BUG:NotUsable
    public static TRS GetTRS(this Transform transform)
    {
        TRS aux = new TRS();
        aux.position.x = (transform.position.x);
        aux.position.y = (transform.position.y);
        aux.position.z = (transform.position.z);
        aux.rotation.x = (transform.rotation.x);
        aux.rotation.y = (transform.rotation.y);
        aux.rotation.z = (transform.rotation.z);
        aux.rotation.w = (transform.rotation.w);
        aux.scale.x = (transform.localScale.x);
        aux.scale.y = (transform.localScale.y);
        aux.scale.z = (transform.localScale.z);
        Debug.Log($"GameObject:{transform.gameObject.activeSelf}");
        aux.isActive = (transform.gameObject.activeSelf);
        return aux;
    }

    public static void SetTRS(this Transform transform, TRS aux, TRSFlags flags)
    {
        if (!flags.HasFlag(TRSFlags.NotPos))
        {
            transform.position = new Vector3(aux.position.x, aux.position.y, aux.position.z);
        }

        if (!flags.HasFlag(TRSFlags.NotRotation))
        {
            transform.rotation = new Quaternion(aux.rotation.x, aux.rotation.y, aux.rotation.z, aux.rotation.w);
        }

        if (!flags.HasFlag(TRSFlags.NotScale))
        {
            transform.localScale = new Vector3(aux.scale.x, aux.scale.y, aux.scale.z);
        }

        if (!flags.HasFlag(TRSFlags.NotActive))
        {
            transform.gameObject.SetActive(aux.isActive);
        }
    }
}