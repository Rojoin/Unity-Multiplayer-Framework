using System;
using System.Collections.Generic;
using RojoinNetworkSystem;
using UnityEngine;
using UnityEngine.Serialization;

public class ClassA : MonoBehaviour, INetObject
{
    [NetValue(1)] public string a = "213";
    [NetValue(2)] public char publicChar = 'd';
    [NetValue(3)] public string publicstring = "Okami";
    [NetValue(4)] public ClassB classb = new(2);
    [NetValue(5)] public Vector3 aector3 = new Vector3();
    [NetValue(6)] public ClassC classC;
    [NetValue(7)] private List<ClassB> classList = new();
    private NetObject _netObject = new NetObject();

    private void Awake()
    {
        _netObject.id = 1;
        _netObject.owner = 1;
        classC = new ClassC();
        classList.Add(new ClassB(23));
        classList.Add(new ClassB(45));
        classList.Add(classb);
        
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