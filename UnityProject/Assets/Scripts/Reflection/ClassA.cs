using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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

public class NetValue : Attribute 
{
    int id;
    public NetValue(int id) 
    {
        this.id = id;
    }
}

public static class FieldInfoExtensions 
{
    public static List<FieldInfo> GetFields(this Vector3 vector3) 
    {
        List<FieldInfo> output = new List<FieldInfo>();
        output.Add(vector3.GetType().GetField("x"));
        output.Add(vector3.GetType().GetField("y"));
        output.Add(vector3.GetType().GetField("z"));
        return output;
    }

    public static List<FieldInfo> GetFields(this Quaternion quaternion)
    {
        List<FieldInfo> output = new List<FieldInfo>();
        output.Add(quaternion.GetType().GetField("x"));
        output.Add(quaternion.GetType().GetField("y"));
        output.Add(quaternion.GetType().GetField("z"));
        output.Add(quaternion.GetType().GetField("w"));
        return output;
    }
}


