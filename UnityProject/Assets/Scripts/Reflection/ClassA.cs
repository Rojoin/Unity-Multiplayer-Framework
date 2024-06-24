using System;
using RojoinNetworkSystem;
using UnityEngine;

public class ClassA : MonoBehaviour, INetObject
{
    [SerializeField] [NetValue(2)] public float publicFloat = 213;
    [SerializeField] [NetValue(3)] public char publicChar = 'd';
    private NetObject _netObject = new NetObject();

    private void Awake()
    {
        _netObject.id = 1;
        _netObject.owner = 1;
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