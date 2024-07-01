using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

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


    public class NetworkSystem
    {
        private Assembly gameAssembly;
        private Dictionary<Type, MethodInfo> extensionMethods = new Dictionary<Type, MethodInfo>();
        private Assembly executingAssembly;

        private List<object> netObjects = new List<object>();
        private Dictionary<MessageType, (Type, Type)> typeOfMessage = new Dictionary<MessageType, (Type, Type)>();
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
            this.consoleMessage += consoleMessage;
            foreach (Type type in gameAssembly.GetTypes())
            {
                NetExtensionClass netExtensionClass = type.GetCustomAttribute<NetExtensionClass>();
                if (netExtensionClass != null)
                {
                    foreach (MethodInfo methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        NetExtensionMethod netExtensionMethod = methodInfo.GetCustomAttribute<NetExtensionMethod>();
                        if (netExtensionMethod != null)
                        {
                            if (extensionMethods.TryAdd(netExtensionMethod.extensionType, methodInfo))
                            {
                                this.consoleMessage.Invoke(methodInfo.Name + netExtensionMethod.extensionType.Name);
                            }
                        }
                    }
                }
            }

            foreach (KeyValuePair<Type, MethodInfo> extensionMethod in extensionMethods)
            {
                this.consoleMessage.Invoke(extensionMethod.Key.Name + extensionMethod.Value.Name);
            }

            executingAssembly = Assembly.GetExecutingAssembly();
            foreach (Type type in executingAssembly.GetTypes())
            {
                NetType netType = type.GetCustomAttribute<NetType>();
                if (netType != null)
                {
                    typeOfMessage.Add(netType.msgType, (netType.Type, type));
                }
            }

            owner = ownerId;
            List<Type> netObjectTypes = GetNetObjectImplementations();
            foreach (Type netObjectType in netObjectTypes)
            {
                consoleMessage?.Invoke($"Found INetObject implementation: {netObjectType.Name}");
            }
        }

        public void AddNetObject(INetObject netObject)
        {
            netObjects.Add(netObject);
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
                    List<Route> route = new List<Route>();
                    InspectCreateMessage(netObject.GetType(), netObject, netObject.GetID(), route,
                        MessageFlags.CheckSum);

                    TRS trs = netObject.GetTRS();
                    NetTRS message = new NetTRS(trs, netObject.GetID(), new List<Route>());
                    byte[] messageData = message.Serialize();
                    dataToSend.Invoke(messageData);
                }
            }
        }

        public void ChangeExternalNetObjects(object data, List<Route> route, int objId)
        {
            for (int index = 0; index < netObjects.Count; index++)
            {
                INetObject netObject = (INetObject)netObjects[index];
                int iterator = 0;
                if (owner != netObject.GetOwner())
                {
                    if (objId == netObject.GetID())
                    {
                        consoleMessage.Invoke($"{data}");
                        netObjects[index] = InspectDataToChange(netObject.GetType(), netObjects[index], data, route,
                            iterator);
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

        public void ChangeNullOrEmptyNetObjects(bool data, List<Route> route, int objId)
        {
            for (int index = 0; index < netObjects.Count; index++)
            {
                INetObject netObject = (INetObject)netObjects[index];
                int iterator = 0;
                if (owner != netObject.GetOwner())
                {
                    if (objId == netObject.GetID())
                    {
                        netObjects[index] = InspectNullOrEmptyData(netObject.GetType(), netObjects[index], data, route,
                            iterator);
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

        public void ChangeTRS(TRS data, List<Route> route, int objId)
        {
            for (int index = 0; index < netObjects.Count; index++)
            {
                INetObject netObject = (INetObject)netObjects[index];
                int iterator = 0;
                if (owner != netObject.GetOwner())
                {
                    if (objId == netObject.GetID())
                    {
                        NetNotSync netNotSync = netObject.GetType().GetCustomAttribute<NetNotSync>();

                        netObject.SetTRS(data, netNotSync != null ? netNotSync.flags : TRSFlags.Default);
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


        public void InspectCreateMessage(Type type, object obj, int objID, List<Route> route,
            MessageFlags flagsFromBase)
        {
            if (obj != null)
            {
                foreach (MessageData info in GetFieldsFromType(type))
                {
                    var currentFlag = info.MessageFlags;
                    if (flagsFromBase.HasFlag(MessageFlags.Important) || flagsFromBase.HasFlag(MessageFlags.Ordenable))
                    {
                        currentFlag = flagsFromBase;
                    }

                    Route route1 = new Route(info.ID, -1, -1);
                    route.Add(route1);
                    consoleMessage.Invoke(
                        $"Route id:{route1.id}-Colpos:{route1.collectionPos} -colsize{route1.collectionSize}");
                    ReadValue(info.FieldInfo, obj, objID, route, currentFlag);
                    route.RemoveAt(route.Count - 1);
                }
            }
            else
            {
                SendNullData(objID, route, flagsFromBase);
            }
        }

        private void SendNullData(int objID, List<Route> route, MessageFlags flagsFromBase)
        {
            NullObject aux = new NullObject();
            aux.isNull = true;
            NetNullOrEmpty message = new NetNullOrEmpty(aux, objID, route, flagsFromBase);
            byte[] messageToSend = message.Serialize();
            //Todo: Send message
            dataToSend.Invoke(messageToSend);
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

            if (extensionMethods.TryGetValue(type, out MethodInfo? method))
            {
                consoleMessage.Invoke($"{type.Name}");
                object uninitializedObject = FormatterServices.GetUninitializedObject(type);

                object fields = method.Invoke(null, new object[] { uninitializedObject });
                if (fields != null)
                {
                    //Todo: check if the method is being invoked.
                    output.AddRange((fields as List<MessageData>));
                }
            }


            //Todo: Debug struct types extension methods


            return output;
        }

        private object InspectDataToChange(Type type, object obj, object data, List<Route> route,
            int iterator)
        {
            try
            {
                if (obj != null)
                {
                    foreach (MessageData info in GetFieldsFromType(type))
                    {
                        //Todo: Must change so each id comes with an specific
                        if (info != null && info.ID == route[iterator].id)
                        {
                            if (route[iterator].collectionSize == -1)
                            {
                                iterator++;
                                if (iterator >= route.Count)
                                {
                                    obj = SetValues(info.FieldInfo, obj, data);
                                }
                                else
                                {
                                    object objectReference = info.FieldInfo.GetValue(obj);
                                    objectReference = InspectDataToChange(info.FieldInfo.FieldType, objectReference,
                                        data,
                                        route,
                                        iterator);
                                    info.FieldInfo.SetValue(obj, objectReference);
                                }

                                iterator--;
                            }
                            else
                            {
                                object objectReference = info.FieldInfo.GetValue(obj);
                                if (typeof(System.Collections.ICollection).IsAssignableFrom(info.FieldInfo.FieldType))
                                {
                                    if (route[iterator].collectionSize == 0)
                                    {
                                        objectReference = CreateEmptyCollection(info.FieldInfo.FieldType);
                                        info.FieldInfo.SetValue(obj, objectReference);
                                        return obj;
                                    }

                                    Type elementType = GetElementType(info.FieldInfo.FieldType);
                                    int collectionSize = (objectReference as ICollection).Count;
                                    bool isListCountDifferent = route[iterator].collectionSize != collectionSize;

                                    object[] arrayToIterate = new object[route[iterator].collectionSize];
                                    if (!isListCountDifferent)
                                    {
                                        (objectReference as ICollection).CopyTo(arrayToIterate, 0);
                                    }
                                    else
                                    {
                                        for (int i = 0; i < route[iterator].collectionSize; i++)
                                        {
                                            if (i < collectionSize)
                                            {
                                                arrayToIterate[i] = (objectReference as ICollection).Cast<object>()
                                                    .ElementAt(i);
                                            }
                                            else
                                            {
                                                arrayToIterate[i] = Activator.CreateInstance(elementType);
                                            }
                                        }
                                    }

                                    for (int i = 0; i < arrayToIterate.Length; i++)
                                    {
                                        consoleMessage.Invoke(
                                            $"{arrayToIterate[i].GetType()}:{info.FieldInfo.FieldType}:Data:{data}:Current{i}:Quantity: {arrayToIterate.Length}");
                                        if (i == route[iterator].collectionPos)
                                        {
                                            var updated = InspectDataToChange(arrayToIterate[i].GetType(),
                                                arrayToIterate[i], data, route, iterator + 1);
                                            arrayToIterate[i] = updated;
                                            iterator--;
                                            consoleMessage.Invoke($"New value is :{arrayToIterate.GetValue(i)}");
                                            break;
                                        }
                                    }

                                    object arrayAsGenericList;
                                    if (info.FieldInfo.FieldType.IsArray)
                                    {
                                        arrayAsGenericList = typeof(NetworkSystem)
                                            .GetMethod(nameof(TransaltorArray),
                                                BindingFlags.Instance | BindingFlags.NonPublic)
                                            .MakeGenericMethod(info.FieldInfo.FieldType.GetElementType())
                                            .Invoke(this, new[] { arrayToIterate });
                                        objectReference = Array.CreateInstance(
                                            info.FieldInfo.FieldType.GetElementType(),
                                            ((Array)arrayAsGenericList).Length);


                                        Array.Copy((Array)arrayAsGenericList, (Array)objectReference,
                                            (arrayAsGenericList as ICollection).Count);
                                    }
                                    else
                                    {
                                        arrayAsGenericList = typeof(NetworkSystem)
                                            .GetMethod(nameof(TransaltorICollection),
                                                BindingFlags.Instance | BindingFlags.NonPublic)
                                            .MakeGenericMethod(info.FieldInfo.FieldType.GenericTypeArguments[0])
                                            .Invoke(this, new[] { arrayToIterate });
                                        objectReference = Activator.CreateInstance(info.FieldInfo.FieldType,
                                            arrayAsGenericList as ICollection);
                                    }

                                    consoleMessage.Invoke($"Set the end of the list{objectReference}");
                                    consoleMessage.Invoke($"Object to change{obj}");
                                    info.FieldInfo.SetValue(obj, objectReference);
                                    consoleMessage.Invoke($"Going Back");
                                }
                            }
                        }
                    }
                }

                return obj;
            }
            catch (Exception e)
            {
                object objectReference;
                objectReference = typeof(System.Collections.ICollection).IsAssignableFrom(type)
                    ? CreateEmptyCollection(type)
                    : Activator.CreateInstance(type);
                objectReference = InspectDataToChange(type, objectReference, data, route, iterator);

                //BUG: cannot trace back the last route
                foreach (MessageData info in GetFieldsFromType(type))
                {
                    if (route[iterator].id == info.ID)
                    {
                        info.FieldInfo.SetValue(obj, objectReference);
                    }
                }

                return objectReference;
                throw;
            }
        }

        private object InspectNullOrEmptyData(Type type, object obj, bool data, List<Route> route,
            int iterator)
        {
            if (obj != null)
            {
                foreach (MessageData info in GetFieldsFromType(type))
                {
                    if (info != null && info.ID == route[iterator].id)
                    {
                        if (route[iterator].collectionSize == -1)
                        {
                            iterator++;
                            if (iterator >= route.Count)
                            {
                                if (data)
                                {
                                    obj = SetValues(info.FieldInfo, obj, null);
                                }
                                else
                                {
                                    object objectReference = info.FieldInfo.GetValue(obj);
                                    objectReference = CreateEmptyCollection(info.FieldInfo.FieldType);
                                    info.FieldInfo.SetValue(obj, objectReference);
                                    return obj;
                                }
                            }
                            else
                            {
                                object objectReference = info.FieldInfo.GetValue(obj);
                                objectReference = InspectDataToChange(info.FieldInfo.FieldType, objectReference, data,
                                    route,
                                    iterator);
                                info.FieldInfo.SetValue(obj, objectReference);
                            }

                            iterator--;
                        }
                        else
                        {
                            object objectReference = info.FieldInfo.GetValue(obj);
                            if (typeof(System.Collections.ICollection).IsAssignableFrom(info.FieldInfo.FieldType))
                            {
                                Type elementType = GetElementType(info.FieldInfo.FieldType);
                                int collectionSize = (objectReference as ICollection).Count;
                                bool isListCountDifferent = route[iterator].collectionSize != collectionSize;

                                object[] arrayToIterate = new object[route[iterator].collectionSize];
                                if (!isListCountDifferent)
                                {
                                    (objectReference as ICollection).CopyTo(arrayToIterate, 0);
                                }
                                else
                                {
                                    for (int i = 0; i < route[iterator].collectionSize; i++)
                                    {
                                        if (i < collectionSize)
                                        {
                                            arrayToIterate[i] = (objectReference as ICollection).Cast<object>()
                                                .ElementAt(i);
                                        }
                                        else
                                        {
                                            arrayToIterate[i] = Activator.CreateInstance(elementType);
                                        }
                                    }
                                }

                                for (int i = 0; i < arrayToIterate.Length; i++)
                                {
                                    consoleMessage.Invoke(
                                        $"{arrayToIterate[i].GetType()}:{info.FieldInfo.FieldType}:Data:{data}:Current{i}:Quantity: {arrayToIterate.Length}");
                                    if (i == route[iterator].collectionPos)
                                    {
                                        var updated = InspectDataToChange(arrayToIterate[i].GetType(),
                                            arrayToIterate[i], data, route, iterator + 1);
                                        arrayToIterate[i] = updated;
                                        iterator--;
                                        consoleMessage.Invoke($"New value is :{arrayToIterate.GetValue(i)}");
                                        break;
                                    }
                                }

                                object arrayAsGenericList;
                                if (info.FieldInfo.FieldType.IsArray)
                                {
                                    arrayAsGenericList = typeof(NetworkSystem)
                                        .GetMethod(nameof(TransaltorArray),
                                            BindingFlags.Instance | BindingFlags.NonPublic)
                                        .MakeGenericMethod(info.FieldInfo.FieldType.GetElementType())
                                        .Invoke(this, new[] { arrayToIterate });
                                    objectReference = Array.CreateInstance(info.FieldInfo.FieldType.GetElementType(),
                                        ((Array)arrayAsGenericList).Length);


                                    Array.Copy((Array)arrayAsGenericList, (Array)objectReference,
                                        (arrayAsGenericList as ICollection).Count);
                                }
                                else
                                {
                                    arrayAsGenericList = typeof(NetworkSystem)
                                        .GetMethod(nameof(TransaltorICollection),
                                            BindingFlags.Instance | BindingFlags.NonPublic)
                                        .MakeGenericMethod(info.FieldInfo.FieldType.GenericTypeArguments[0])
                                        .Invoke(this, new[] { arrayToIterate });
                                    objectReference = Activator.CreateInstance(info.FieldInfo.FieldType,
                                        arrayAsGenericList as ICollection);
                                }

                                consoleMessage.Invoke($"Set the end of the list{objectReference}");
                                consoleMessage.Invoke($"Object to change{obj}");
                                info.FieldInfo.SetValue(obj, objectReference);
                                consoleMessage.Invoke($"Going Back");
                            }
                        }
                    }
                }
            }

            return obj;
        }

        private object SetValues(FieldInfo info, object obj, object data)
        {
            try
            {
                consoleMessage.Invoke($"Data:{data} - Object:{obj}");
                info.SetValue(obj, data);
                consoleMessage.Invoke($"{info.GetValue(obj)}");
                return obj;
            }
            catch (Exception e)
            {
                consoleMessage.Invoke("Cannot value by custom method.");
                consoleMessage.Invoke($"{e}");
                throw;
            }
        }

        //1
        public void ReadValue(FieldInfo info, object obj, int objID, List<Route> route, MessageFlags flags)
        {
            if ((info.FieldType.IsValueType && info.FieldType.IsPrimitive) || info.FieldType == typeof(string) ||
                info.FieldType.IsEnum)
            {
                consoleMessage.Invoke("Preparing Message");
                SendMessage(info, obj, objID, route, flags);
            }
            else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(info.FieldType))
            {
                try
                {
                    int aux = 0;
                    bool isCleared = true;
                    foreach (object item in ((info.GetValue(obj) as System.Collections.ICollection)!))
                    {
                        MessageFlags currentFlag = MessageFlags.CheckSum;
                        if (flags.HasFlag(MessageFlags.Important) || flags.HasFlag(MessageFlags.Ordenable))
                        {
                            currentFlag = flags;
                        }

                        Route route1 = route[route.Count - 1];
                        consoleMessage.Invoke($"{route.Count - 1}");
                        route1.collectionSize = (info.GetValue(obj) as System.Collections.ICollection).Count;
                        route1.collectionPos = aux++;
                        isCleared = false;
                        route[route.Count - 1] = route1;
                        InspectCreateMessage(item.GetType(), item, objID, route, currentFlag);
                    }

                    if (isCleared)
                    {
                        SendEmptyCollectionData(objID, route, flags);
                    }
                }
                catch (Exception e)
                {
                    SendNullData(objID, route, flags);
                    throw;
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

        private void SendEmptyCollectionData(int objID, List<Route> route, MessageFlags flags)
        {
            Route route1 = route[route.Count - 1];
            consoleMessage.Invoke($"{route.Count - 1}");
            route1.collectionSize = 0;
            route1.collectionPos = -1;
            route[route.Count - 1] = route1;
            NullObject clear = new NullObject();
            clear.isNull = false;
            NetNullOrEmpty message = new NetNullOrEmpty(clear, objID, route, flags);
            byte[] messageToSend = message.Serialize();
            //Todo: Send message
            dataToSend.Invoke(messageToSend);
        }

        private void SendMessage(FieldInfo info, object obj, int objId, List<Route> route, MessageFlags value)
        {
            object package = info.GetValue(obj);
            Type packageType = package.GetType();
//Todo: Change
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
                            Type[] parametersToApply =
                                { packageType, objId.GetType(), route.GetType(), value.GetType() };
                            object[] parameters = new[] { package, objId, route, value };
                            object netMessage = Activator.CreateInstance(currentType, parameters);
                            if (netMessage != null)
                            {
                                BaseMessage message = netMessage as BaseMessage;
                                byte[] messageToSend = message.Serialize();
                                //Todo: Send message
                                dataToSend.Invoke(messageToSend);
                            }
                        }
                    }
                }
            }
        }

        public void HandlerMessage(byte[] data)
        {
            MessageType type = NetByteTranslator.GetNetworkType(data);
            int playerID = NetByteTranslator.GetPlayerID(data);
            MessageFlags flags = NetByteTranslator.GetFlags(data);

            bool shouldCheckSum = flags.HasFlag(MessageFlags.CheckSum);
            bool isImportant = flags.HasFlag(MessageFlags.Important);
            bool isOrdenable = flags.HasFlag(MessageFlags.Important);
            ulong getMessageID = 0;
            if (shouldCheckSum)
            {
                if (!BaseMessage<int>.IsMessageCorrectS(data.ToList()))
                {
                    consoleMessage.Invoke($"Message is corrupted");
                    return;
                }
            }

            if (isOrdenable)
            {
                getMessageID = NetByteTranslator.GetMesaggeID(data);
            }

            if (type == MessageType.NullorEmpty)
            {
                NetObjectBasicData messageInfo = NetByteTranslator.GetNetObjectData(data);
                NetNullOrEmpty aux = new NetNullOrEmpty();
                bool messageData = (bool)aux.Deserialize(data);
                ChangeNullOrEmptyNetObjects(messageData, messageInfo.idValues,
                    messageInfo.objectID);
            }
            else if (type == MessageType.TRS)
            {
                NetObjectBasicData messageInfo = NetByteTranslator.GetNetObjectData(data);
                NetTRS aux = new NetTRS();
                TRS messageData = aux.DeseliarizeObj(data);
                ChangeTRS(messageData, messageInfo.idValues,
                    messageInfo.objectID);
            }
            else if (typeOfMessage.TryGetValue(type, out (Type, Type) dataType))
            {
                //Create message
                BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;
                object netMessage = Activator.CreateInstance(dataType.Item2);
                if (netMessage != null)
                {
                    //TOdo: Cheack a way to call deserialize,
                    BaseMessage message = netMessage as BaseMessage;
                    object messageData = message.Deserialize(data);
                    NetObjectBasicData messageInfo = NetByteTranslator.GetNetObjectData(data);
                    //Todo: call to the method that need  to set the value.
                    ChangeExternalNetObjects(messageData, messageInfo.idValues,
                        messageInfo.objectID);
                }
            }
        }

        private object TransaltorICollection<T>(object[] objs)
        {
            List<T> listToTranslate = new List<T>();
            consoleMessage.Invoke($"Type of t:{typeof(T)}");
            foreach (object elementsOfObjets in objs)
            {
                consoleMessage.Invoke($"Elements of Type:{elementsOfObjets.GetType()}");
                listToTranslate.Add((T)elementsOfObjets);
            }

            return listToTranslate;
        }

        private object TransaltorArray<T>(object[] objs)
        {
            T[] arrayToTranslator = new T[objs.Length];
            consoleMessage.Invoke($"Type of t:{typeof(T)}");
            for (int i = 0; i < objs.Length; i++)
            {
                consoleMessage.Invoke($"Elements of Type:{objs.GetType()}");
                arrayToTranslator[i] = ((T)objs[i]);
            }

            return arrayToTranslator;
        }

        private Type GetElementType(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }
            else if (type.IsGenericType)
            {
                return type.GetGenericArguments()[0];
            }

            return typeof(object);
        }

        private object CreateEmptyCollection(Type type)
        {
            if (type.IsArray)
            {
                return Array.CreateInstance(type.GetElementType(), 0);
            }
            else if (typeof(ICollection).IsAssignableFrom(type) && type.IsGenericType)
            {
                Type elementType = GetElementType(type);
                var genericListType = typeof(List<>).MakeGenericType(elementType);
                return Activator.CreateInstance(genericListType);
            }

            return null;
        }
    }
}