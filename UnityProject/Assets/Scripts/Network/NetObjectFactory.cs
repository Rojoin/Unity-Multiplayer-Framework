using System;
using System.Collections.Generic;
using RojoinNetworkSystem;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.Events;

public class NetObjectFactory : MonoBehaviourSingleton<NetObjectFactory>
{
    public List<GameObject> prefabsToInstatiate = new List<GameObject>();
    public List<GameObject> instatiateObjects = new List<GameObject>();
    public NetworkSystem NetworkSystem;
    public AskForPlayerChannelSo myPlayer;
    public AskForPlayerChannelSo otherPlayer;
    public UnityEvent<byte[]> dataToSend = new UnityEvent<byte[]>();

    public void InstanceNetObject(AskForNetObject data)
    {
        GameObject objectToCreate = Instantiate(prefabsToInstatiate[data.objectType]);
        // if (data.parentId < instatiateObjects.Count)
        // {
        //     objectToCreate.transform.SetParent(instatiateObjects[data.parentId].transform);
        // }

        objectToCreate.transform.position = new Vector3(data.pos.X, data.pos.Y, data.pos.Z);
        var rotation = objectToCreate.transform.rotation;
        rotation.eulerAngles = new Vector3(data.rot.X, data.rot.Y, data.rot.Z);
        objectToCreate.transform.rotation = rotation;
        objectToCreate.transform.localScale = new Vector3(data.scale.X, data.scale.Y, data.scale.Z);
        instatiateObjects.Add(objectToCreate);
        INetObject netObject = objectToCreate.GetComponent<INetObject>();
        netObject.GetObject().id = data.intanceID;
        netObject.GetObject().owner = data.owner;
        NetworkSystem.AddNetObject(netObject);
        if (objectToCreate.TryGetComponent<PlayerShooting>(out var shooting))
        {
            if (NetworkSystem.owner == data.owner)
            {
                myPlayer.RaiseEvent(data.owner, "DefaultName", objectToCreate);
            }
            else
            {
                otherPlayer.RaiseEvent(data.owner, "Other", objectToCreate);
            }
        }
    }

    public void SendDeleteMessage(NetObject netObject)
    {
        if (NetworkSystem.owner == netObject.owner)
        {
            NetDelete netDelete = new NetDelete(netObject.id, netObject.id, new List<Route>());
            dataToSend.Invoke(netDelete.Serialize());
            Debug.Log($"Send Message to delete id{netObject.id}");
        }
    }
    public void DeleteGameObjects(int id)
    {
        List<GameObject> copyList = new List<GameObject>(instatiateObjects);
        for (int index = 0; index < copyList.Count; index++)
        {
            GameObject obj = copyList[index];
            if (obj.GetComponent<INetObject>().GetID() == id)
            {
                var aux = instatiateObjects[index];
                instatiateObjects.RemoveAt(index);
                Destroy(aux);
            }
        }
    }
}