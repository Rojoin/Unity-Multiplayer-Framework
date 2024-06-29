using System;
using System.Collections.Generic;
using RojoinNetworkSystem;
using UnityEngine;
using UnityEngine.Serialization;

public class ClassA : MonoBehaviour, INetObject
{
    [NetValue(1)] public float a = 2;
    public char publicChar = 'd';
    public string publicstring = "Okami";
    public ClassB classb = new(2);
    public Vector3 aector3 = new Vector3();
    public ClassC classC;
   [SerializeField] [NetValue(7)] private List<ClassB> classList = new();
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