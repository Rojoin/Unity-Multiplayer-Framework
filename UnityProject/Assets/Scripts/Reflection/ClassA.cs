using System;
using RojoinNetworkSystem;
using UnityEngine;
using UnityEngine.Serialization;

public class ClassA : MonoBehaviour, INetObject
{
    [SerializeField] [NetValue(2)] public  string  a = "213";
    [SerializeField] [NetValue(3)] public char publicChar = 'd';
    [SerializeField] [NetValue(4)] public string publicstring = "Okami";
    [SerializeField] [NetValue(5)] public Vector3 Vector3 = new Vector3();
    [NetValue(6)] public ClassC classC;
    private NetObject _netObject = new NetObject();

    private void Awake()
    {
        _netObject.id = 1;
        _netObject.owner = 1;
        classC = new ClassC();
    }

    public int GetID()
    {
        return _netObject.id;
    }

    public int GetOwner()
    {
        return _netObject.owner;
    }

    public NetObject GetObject()
    {
        return _netObject;
    }
}