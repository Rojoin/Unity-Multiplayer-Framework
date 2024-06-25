using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Reflection;

namespace RojoinNetworkSystem
{
    public class MessageData
    {
        public int ID;
        public FieldInfo FieldInfo;
        public MessageFlags MessageFlags;

        public MessageData(FieldInfo fieldInfo, int id, MessageFlags messageFlags = MessageFlags.CheckSum)
        {
            this.FieldInfo = fieldInfo;
            MessageFlags = messageFlags;
            ID = id;
        }
    }

    public static class FieldInfoExtensions
    {
        [NetExtensionMethod]
        public static List<MessageData> GetFields(this Vector3 vector3)
        {
            List<MessageData> output = new List<MessageData>();
            output.Add(new MessageData(vector3.GetType().GetField("x"), 0, MessageFlags.CheckSum));
            output.Add(new MessageData(vector3.GetType().GetField("y"), 1, MessageFlags.CheckSum));
            output.Add(new MessageData(vector3.GetType().GetField("z"), 2, MessageFlags.CheckSum));
            return output;
        }

        [NetExtensionMethod]
        public static List<FieldInfo> GetFields(this Quaternion quaternion)
        {
            List<FieldInfo> output = new List<FieldInfo>();
            output.Add(quaternion.GetType().GetField("x"));
            output.Add(quaternion.GetType().GetField("y"));
            output.Add(quaternion.GetType().GetField("z"));
            output.Add(quaternion.GetType().GetField("w"));
            return output;
        }


        [NetExtensionMethod]
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

    public class NetworkSystem
    {
        private Assembly gameAssembly;
        private Assembly executingAssembly;

        private List<object> netObjects = new List<object>();
        private int owner;

        public Action<byte[]> dataToSend;
        public Action<string> consoleMessage;

        public void StartNetworkSystem(int ownerId)
        {
            gameAssembly = Assembly.GetCallingAssembly();
            executingAssembly = Assembly.GetExecutingAssembly();
            owner = ownerId;

            List<Type> netObjectTypes = GetNetObjectImplementations();
            foreach (Type netObjectType in netObjectTypes)
            {
                consoleMessage?.Invoke($"Found INetObject implementation: {netObjectType.Name}");
            }
        }

        public void StartNetworkSystem(int ownerId, Action<string> consoleMessage)
        {
            gameAssembly = Assembly.GetCallingAssembly();
            executingAssembly = Assembly.GetExecutingAssembly();
            owner = ownerId;
            this.consoleMessage += consoleMessage;
            List<Type> netObjectTypes = GetNetObjectImplementations();
            foreach (Type netObjectType in netObjectTypes)
            {
                consoleMessage?.Invoke($"Found INetObject implementation: {netObjectType.Name}");
            }
        }

        public void AddNetObject(object netObject)
        {
            if (netObject is INetObject)
            {
                netObjects.Add(netObject);
            }
            else
            {
                Console.Error.WriteLine("The item doesnt have the INetObject interface.");
                consoleMessage.Invoke("The item doesnt have the INetObject interface.");
            }
        }

        public List<Type> GetNetObjectImplementations()
        {
            List<Type> netObjectTypes = new List<Type>();

            foreach (Type type in gameAssembly.GetTypes())
            {
                if (typeof(INetObject).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
                {
                    netObjectTypes.Add(type);
                }
            }

            return netObjectTypes;
        }

        public void CheckNetObjectsToSend()
        {
            //Todo: can crash
            //No, can trash
            foreach (INetObject netObject in netObjects)
            {
                if (netObject.GetOwner() == owner)
                {
                    Stack<int> route = new Stack<int>();
                    consoleMessage.Invoke("Preparing Object to send");
                    InspectCreateMessage(netObject.GetType(), netObject, netObject.GetID(), route, MessageFlags.None);
                }
            }
        }

        public void ChangeExternalNetObjects(object data, List<int> route, int objId)
        {
            foreach (INetObject netObject in netObjects)
            {
                int iterator = 0;
                if (owner != netObject.GetOwner())
                {
                    consoleMessage.Invoke($"The object id is {objId}  and the owned is{netObject.GetID()}");
                    consoleMessage.Invoke($"The data is {data}");
                    if (objId == netObject.GetID())
                    {
                        InspectDataToChange(netObject.GetType(), netObject, data, route, iterator);
                    }
                    else
                    {
                        consoleMessage.Invoke("The object has a different ID");
                    }
                }
                else
                {
                    consoleMessage.Invoke("The object cant be changed");
                }
            }
        }


        public void InspectCreateMessage(Type type, object obj, int objID, Stack<int> route, MessageFlags flagsFromBase)
        {
            if (obj != null)
            {
                Stack<int> listBeforeIteration = new Stack<int>(route);
                //foreach (FieldInfo info in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public |
                //                                          BindingFlags.Instance | BindingFlags.DeclaredOnly))
                foreach (MessageData info in GetFieldsFromType(type))
                {
                    route = listBeforeIteration;
                    var currentFlag = info.MessageFlags;
                    if (flagsFromBase.HasFlag(MessageFlags.Important) || flagsFromBase.HasFlag(MessageFlags.Ordenable))
                    {
                        currentFlag = flagsFromBase;
                    }

                    consoleMessage.Invoke($"The object has Route {info.ID}");
                    route.Push(info.ID);
                    ReadValue(info.FieldInfo, obj, objID, route, flagsFromBase);
                    route.Pop();
                }
            }
        }

        private List<MessageData> GetFieldsFromType(Type type)
        {
            List<MessageData> output = new List<MessageData>();
            foreach (FieldInfo info in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public |
                                                      BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                NetValue netValue = info.GetCustomAttribute<NetValue>();
                if (netValue != null)
                {
                    output.Add(new MessageData(info, netValue.id, netValue.messageFlags));
                }
            }

            foreach (MethodInfo info in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public |
                                                        BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                NetExtensionMethod netExt = info.GetCustomAttribute<NetExtensionMethod>();
                if (netExt != null && info.ReturnType == typeof(List<MessageData>))
                {
                    object fields = info.Invoke(null, new object[0] { });
                    if (fields != null)
                    {
                        output.AddRange((fields as List<MessageData>));
                    }
                }
            }

            return output;
        }

        //public void InspectCreateMessageMe(Type type, object obj, int objID, Stack<int> route)
        //{
        //    if (obj != null)
        //    {
        //        Stack<int> listBeforeIteration = new Stack<int>(route);
        //        foreach (MethodInfo info in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public |
        //                                                    BindingFlags.Instance | BindingFlags.DeclaredOnly))
        //        {
        //            route = listBeforeIteration;
        //            NetExtensionMethod netExt = info.GetCustomAttribute<NetExtensionMethod>();
        //            if (netExt != null)
        //            {
        //                consoleMessage.Invoke($"The object has NetValue {netExt.id}");
        //                route.Push(netExt.id);
        //                // ReadValue(info, obj, objID, route, netValue);
        //                route.Pop();
        //            }
        //        }
        //    }
        //}

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
        public void ReadValue(FieldInfo info, object obj, int objID, Stack<int> route, MessageFlags flags)
        {
            if (info.FieldType.IsValueType || info.FieldType == typeof(string) || info.FieldType.IsEnum)
            {
                List<int> valuesRoute = new List<int>(route);
                SendMessage(info, obj, objID, valuesRoute, flags);
            }
            else if (typeof(System.Collections.ICollection).IsAssignableFrom(info.FieldType))
            {
                foreach (object item in (info.GetValue(obj) as System.Collections.ICollection))
                {
                    InspectCreateMessage(item.GetType(), obj, objID, route, flags);
                }
            }
            else
            {
                MessageFlags currentFlag = MessageFlags.CheckSum;
                if (flags.HasFlag(MessageFlags.Important) || flags.HasFlag(MessageFlags.Ordenable))
                {
                    currentFlag = flags;
                }

                InspectCreateMessage(info.FieldType, info.GetValue(obj), objID, route, currentFlag);
            }
        }

        private void SendMessage(FieldInfo info, object obj, int objId, List<int> route, MessageFlags value)
        {
            object package = info.GetValue(obj);
            Type packageType = package.GetType();

            foreach (Type currentType in executingAssembly.GetTypes())
            {
                if (currentType.BaseType != null && currentType.BaseType.IsGenericType &&
                    currentType.BaseType.GetGenericTypeDefinition() == typeof(INetObjectMessage<>))
                {
                    Type[] generic = currentType.BaseType.GetGenericArguments();
                    foreach (Type arg in generic)
                    {
                        if (packageType == arg)
                        {
                            //Create message
                            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;

                            consoleMessage?.Invoke($"{objId}");
                            consoleMessage?.Invoke($"Package of type {packageType}:{package}");
                            Type[] parametersToApply =
                                { packageType, objId.GetType(), route.GetType(), value.GetType() };
                            object[] parameters = new[] { package, objId, route, value };
                            //ConstructorInfo? ctor = currentType.GetConstructor(parametersToApply);
                            object netMessage = Activator.CreateInstance(currentType, parameters);
                            if (netMessage != null)
                            {
                                //  object message = ctor.Invoke(parameters);
                                //var a = (message as BaseMessage);
                                BaseMessage message = netMessage as BaseMessage;
                                consoleMessage?.Invoke($"NetMessage Data: {(message as NetFloat).GetData()}");
                                byte[] messageToSend = message.Serialize();
                                consoleMessage?.Invoke(
                                    $"DeseializedMessage Lib:{BitConverter.ToSingle(messageToSend, 32)}");
                                //Todo: Send message
                                dataToSend.Invoke(messageToSend);
                            }
                        }
                    }
                }
            }
        }
    }
}