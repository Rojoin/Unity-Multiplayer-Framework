using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
public class Reflection : MonoBehaviour
{
    public ClassB classB;
    // Start is called before the first frame update
    void Start()
    {
        Inspect(typeof(ClassB), classB);
        // foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
        // {
        // }

    }

    public void Inspect(Type type, object obj)
    {
        if (obj != null)
        {

            foreach (FieldInfo info in type.GetFields(
                BindingFlags.NonPublic |
                BindingFlags.Public |
                BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                ReadValue(info, obj);
            }

            if (type.BaseType != null)
            {
                Inspect(type.BaseType, obj);
            }
        }
    }

    public void ReadValue(FieldInfo info, object obj)
    {
        if (info.FieldType.IsValueType || info.FieldType == typeof(string) || info.FieldType.IsEnum)
        {
            Debug.Log(info.Name + ": " + info.GetValue(obj));
        }
        else if (typeof(System.Collections.ICollection).IsAssignableFrom(info.FieldType))
        {
            foreach (object item in (info.GetValue(obj) as System.Collections.ICollection))
            {
                Inspect(item.GetType(), item);
            }
        }
        else
        {
            Inspect(info.FieldType, info.GetValue(obj));
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
