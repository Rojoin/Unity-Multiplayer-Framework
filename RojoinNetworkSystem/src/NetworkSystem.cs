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
        

        public void CheckNetObjectsToSend()
        {
            foreach (T netObject in netObjects)
            {
                if (netObject.GetOwner() == owner)
                {
                    Stack<int> route = new Stack<int>();
                    InspectCreateMessage(netObject.GetType(), netObject, netObject.GetID(), route);
                }
            }
        }

        public void ChangeExternalNetObjects(object data, List<int> route, int objId)
        {
            foreach (T netObject in netObjects)
            {
                int iterator = 0;
                if (owner != netObject.GetOwner())
                {
                    if (objId == netObject.GetID())
                    {
                        InspectDataToChange(netObject.GetType(), netObject, netObject.GetID(), route, iterator);
                    }
                }
            }
        }


        public void InspectCreateMessage(Type type, object obj, int objID, Stack<int> route)
        {
            if (obj != null)
            {
                Stack<int> listBeforeIteration = new Stack<int>(route);
                foreach (FieldInfo info in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public |
                                                          BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    route = listBeforeIteration;
                    NetValue netValue = info.GetCustomAttribute<NetValue>();
                    if (netValue != null)
                    {
                        route.Push(netValue.id);
                        ReadValue(info, obj, objID, route, netValue);
                        route.Pop();
                    }
                }
            }
        }

        public void InspectDataToChange(Type type, object obj, object data, List<int> route, int iterator)
        {
            if (obj != null)
            {
                foreach (FieldInfo info in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public |
                                                          BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    NetValue netValue = info.GetCustomAttribute<NetValue>();
                    if (netValue != null && netValue.id == route[iterator])
                    {
                        iterator++;

                        if (iterator >= route.Count)
                        {
                            SetValues(info, obj, data);
                        }
                        else if (iterator < route.Count)
                        {
                            if (typeof(System.Collections.ICollection).IsAssignableFrom(info.FieldType))
                            {
                                foreach (object item in (info.GetValue(obj) as System.Collections.ICollection))
                                {
                                    InspectDataToChange(info.FieldType, obj, data, route, iterator);
                                }
                            }
                            else
                            {
                                InspectDataToChange(info.FieldType, obj, data, route, iterator);
                            }
                        }
                    }
                }
            }
        }

        private void SetValues(FieldInfo info, object obj, object data)
        {
            info.SetValue(obj, data);
        }

        //1
        public void ReadValue(FieldInfo info, object obj, int objID, Stack<int> route, NetValue value)
        {
            if (info.FieldType.IsValueType || info.FieldType == typeof(string) || info.FieldType.IsEnum)
            {
                List<int> valuesRoute = new List<int>(route);
                SendMessage(info, obj, objID, valuesRoute, value);
            }
            else if (typeof(System.Collections.ICollection).IsAssignableFrom(info.FieldType))
            {
                foreach (object item in (info.GetValue(obj) as System.Collections.ICollection))
                {
                    InspectCreateMessage(item.GetType(), obj, objID, route);
                }
            }
            else
            {
                InspectCreateMessage(info.FieldType, info.GetValue(obj), objID, route);
            }
        }

        private void SendMessage(FieldInfo info, object obj, int objId, List<int> route, NetValue value)
        {
            object package = info.GetValue(obj);
            Type packageType = package.GetType();

            foreach (Type currentType in executingAssembly.GetTypes())
            {
                if (currentType.BaseType != null && currentType.IsGenericType &&
                    currentType.BaseType.GetGenericTypeDefinition() == typeof(INetObjectMessage<>))
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
                                var a = (message as BaseMessage);
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