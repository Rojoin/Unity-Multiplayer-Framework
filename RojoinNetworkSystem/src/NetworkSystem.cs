using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Reflection;

namespace RojoinNetworkSystem
{
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

        public static List<FieldInfo> GetFields(this Color color)
        {
            List<FieldInfo> output = new List<FieldInfo>();
            output.Add(color.GetType().GetField("r"));
            output.Add(color.GetType().GetField("g"));
            output.Add(color.GetType().GetField("b"));
            output.Add(color.GetType().GetField("a"));
            return output;
        }
    }

    public class NetworkSystem<T> where T : INetObject
    {
        private Assembly gameAssembly;
        private Assembly executingAssembly;

        private List<T> netObjects = new List<T>();
        private int owner;

        void StartNetworkSystem(int ownerId)
        {
            gameAssembly = Assembly.GetCallingAssembly();
            executingAssembly = Assembly.GetExecutingAssembly();
            owner = ownerId;
        }

        public void AddNetObject(T netObject)
        {
            netObjects.Add(netObject);
        }

        void GetObjectsToSend()
        {
            foreach (T ne in netObjects)
            {
                if (ne.GetOwner() == owner)
                {
                    Type aux = ne.GetType();
                }
            }
        }

        public void CheckNetObjects()
        {
            foreach (T netObject in netObjects)
            {
                List<int> route = new List<int>();
                InspectCreateMessage(netObject.GetType(), netObject, route);
            }
        }

        public void CreateMessage(Type type, object obj)
        {
            if (obj != null)
            {
                foreach (FieldInfo info in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public |
                                                          BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    NetValue customAttribute = info.GetCustomAttribute<NetValue>();
                    if (customAttribute != null)
                    {
                        //TODO:Create Message
                    }

                    if (type.BaseType != null)
                    {
                        // InspectCreateMessage(type.BaseType, obj);
                    }
                }
            }
        }

        public void InspectCreateMessage(Type type, object obj, List<int> route)
        {
            if (obj != null)
            {
                foreach (FieldInfo info in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public |
                                                          BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    NetValue netValue = info.GetCustomAttribute<NetValue>();

                    if (netValue != null)
                    {
                        //BUG: Change postion of list
                        route.Add(netValue.id);
                        //1
                        ReadValue(info, obj, route, netValue);
                    }

                    //      ReadValue(info, obj);
                }

                if (type.BaseType != null)
                {
                    InspectCreateMessage(type.BaseType, obj);
                }
            }
        }

        //1
        public void ReadValue(FieldInfo info, object obj, List<int> route, NetValue value)
        {
            if (info.FieldType.IsValueType || info.FieldType == typeof(string) || info.FieldType.IsEnum)
            {
                // Debug.Log(info.Name + ": " + info.GetValue(obj));
                //SendMessage(info, obj, value,route)
            }
            else if (typeof(System.Collections.ICollection).IsAssignableFrom(info.FieldType))
            {
                foreach (object item in (info.GetValue(obj) as System.Collections.ICollection))
                {
                    InspectCreateMessage(item.GetType(), item);
                }
            }
            else
            {
                InspectCreateMessage(info.FieldType, info.GetValue(obj));
            }
        }

        public void SetValues(FieldInfo info, object obj, object data)
        {
            if (info.FieldType.IsValueType || info.FieldType == typeof(string) || info.FieldType.IsEnum)
            {
                info.SetValue(obj, data);
            }
            else if (typeof(System.Collections.ICollection).IsAssignableFrom(info.FieldType))
            {
                foreach (object item in (info.GetValue(obj) as System.Collections.ICollection))
                {
                    InspectCreateMessage(item.GetType(), item);
                }
            }
            else
            {
                InspectCreateMessage(info.FieldType, info.GetValue(obj));
            }
        }

        private void SendMessage(FieldInfo info, object obj, int objId, List<int> route, NetValue value)
        {
            object package = info.GetValue(obj);
            Type packageType = package.GetType();

            foreach (Type currentType in executingAssembly.GetTypes())
            {
                if (currentType.BaseType != null && currentType.IsClass && currentType.BaseType.IsGenericType)
                {
                    Type[] generic = currentType.BaseType.GetGenericArguments();
                    foreach (Type arg in generic)
                    {
                        if (packageType == arg)
                        {
                            //Create message
                            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;

                            Type[] parametersToApply =
                                { packageType, objId.GetType(), route.GetType(), value.messageFlags.GetType() };

                            object[] parameters = new[] { package, objId, route, value.messageFlags };
                            ConstructorInfo? ctor = currentType.GetConstructor(parametersToApply);
                            if (ctor != null)
                            {
                                object message = ctor.Invoke(parameters);
                               var a = (message as BaseMessage );
                               a.Serialize();
                               //Todo: Send message
                            }
                        }
                    }
                }
            }
        }
    }
}