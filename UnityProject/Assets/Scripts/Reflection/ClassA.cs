using System;
using System.Collections.Generic;
using RojoinNetworkSystem;
using UnityEngine;
using UnityEngine.Serialization;

public class ClassA : MonoBehaviour
{
    [NetValue(1)] public float a = 2;
    public char publicChar = 'd';
    public string publicstring = "Okami";
    public ClassB classb = new(2);
    public Vector3 aector3 = new Vector3();
    public ClassC classC;
    [SerializeField] private List<ClassB> classList = new();
    [SerializeField] [NetValue(9)] private ClassB[] classArray = new ClassB[2];
    [SerializeField] private List<Vector3> vector3s = new();
    private NetObject _netObject = new NetObject();
    private INetObject _netObjectImplementation;

    private void Awake()
    {
        _netObject.id = 1;
        _netObject.owner = 1;
        classC = new ClassC();
        classList.Add(new ClassB(23));
        classList.Add(new ClassB(45));
        classList.Add(classb);
        classArray[0] = new ClassB(12);
        classArray[1] = new ClassB(42);
        vector3s.Add(new Vector3(1, 2, 3));
        vector3s.Add(new Vector3(3, 2, 1));
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

    public TRS GetTRS()
    {
        return transform.GetTRS();
    }public void SetTRS(TRS trs,TRSFlags flags)
    {
       transform.SetTRS(trs,flags);
    }
    

    [ContextMenu("NUll Object")]
    public void KillObject()
    {
        classArray = null;
    }
}