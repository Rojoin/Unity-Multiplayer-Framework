using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using RojoinNetworkSystem;
using UnityEngine;

public class ClassA
{
    [NetValue(0)] public float publicFloat;
    [NetValue(1)] private string privateString;
    [NetValue(2)] protected bool protectedBool;
    public ClassA()
    {
        publicFloat = 10.0f;
        privateString = "patata";
        protectedBool = true;
    }
}





