using System.Collections.Generic;
using RojoinNetworkSystem;
using UnityEngine;

public class NetObjectFactory : MonoBehaviourSingleton<NetObjectFactory>
{
    public List<GameObject> prefabsToInstatiate = new List<GameObject>();
    public List<GameObject> instatiateObjects = new List<GameObject>();
    public NetworkSystem NetworkSystem;

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
        
        INetObject netObject = objectToCreate.GetComponent<INetObject>();
        netObject.GetObject().id = data.intanceID;
        netObject.GetObject().owner = data.owner;
        NetworkSystem.AddNetObject(netObject);
    }
}