using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace RojoinNetworkSystem
{
    public struct Route
    {
        public int id;
        public int collectionPos;
        public int collectionSize;

        public Route(int id, int colpos, int colSize)
        {
            this.id = id;
            collectionPos = colpos;
            collectionSize = colSize;
        }
        
    }
    public class NetType : Attribute
    {
        public MessageType msgType;
        public Type Type;

        public NetType(MessageType msgType,Type type)
        {
            Type = type;
            this.msgType = msgType;
        }
    }
    public enum MessageType
    {
        Error = -3,
        HandShake = -2,
        HandShakeOk = -1,
        ServerDir = 0,
        Position = 1,
        Message = 2,
        Ping,
        Confirmation,
        Exit,
        Timer,
        AskForObject,
        TRS,
        Float,
        Int,
        UInt,
        Short,
        UShort,
        Long,
        ULong,
        Byte,
        SByte,
        Char,
        String,
        Bool,
        Double,
        Decimal
    }


    [Flags]
    public enum MessageFlags
    {
        None = 0,
        CheckSum = 1,
        Ordenable = 2,
        Important = 4,
        Resend = 8
    }

    public abstract class BaseMessage
    {
        protected MessageType MsgType;
        protected MessageFlags Flags;
        public static int PlayerID;
        protected byte[] ByteData;
        protected int offsetSize = 0;
        public abstract byte[] Serialize(int playerId);

        public byte[] Serialize()
        {
            return Serialize(PlayerID);
        }

        public void SetByteData(byte[] newByteData) => this.ByteData = newByteData;
        public abstract object Deserialize(byte[] message);
    }

    public abstract class BaseMessage<PayloadType> : BaseMessage
    {
        protected PayloadType Data;


        private static BitOperations[] _operationsArray1 = new[]
        {
            BitOperations.substract, BitOperations.sum, BitOperations.moveLeft, BitOperations.moveRight,
            BitOperations.sum, BitOperations.moveLeft, BitOperations.moveRight, BitOperations.moveRight
        };

        private static BitOperations[] _operationsArray2 = new[]
        {
            BitOperations.moveRight, BitOperations.sum, BitOperations.moveLeft, BitOperations.substract
        };

        protected BaseMessage(PayloadType data, MessageFlags messageFlags = MessageFlags.CheckSum)
        {
            Data = data;
            Flags = messageFlags;
        }

        protected BaseMessage()
        {
            SetOffset();
        }

        public MessageType GetMessageType()
        {
            return MsgType;
        }

        public int GetID()
        {
            return PlayerID;
        }

        public PayloadType CastFromObj(object obj)
        {
            return (PayloadType)obj;
        }

        public PayloadType DeseliarizeObj(byte[] data)
        {
            return CastFromObj(Deserialize(data));
        }

        public PayloadType GetData()
        {
            return Data;
        }

        protected virtual void BasicSerialize(List<byte> outData, MessageType type, int newPlayerID)
        {
            outData.AddRange(BitConverter.GetBytes((int)type));
            outData.AddRange(BitConverter.GetBytes(newPlayerID));
            outData.AddRange(BitConverter.GetBytes((int)Flags));
            offsetSize = sizeof(int) * 3;
        }


        protected virtual void SetOffset()
        {
            offsetSize = sizeof(int) * 3;
        }

        protected virtual void DataCheckSumEncryption(List<byte> outData)
        {
            uint checkSum1 = NetByteTranslator.EncryptBitSizeOperations(outData, _operationsArray1);
            uint checkSum2 = NetByteTranslator.EncryptBitSizeOperations(outData, _operationsArray2);
            outData.AddRange(BitConverter.GetBytes(checkSum1));
            outData.AddRange(BitConverter.GetBytes(checkSum2));
        }

        public virtual bool IsMessageCorrect(List<byte> outData)
        {
            uint checkSum1 = NetByteTranslator.DecryptBitSizeOperations(outData, _operationsArray1);
            uint checkSum2 = NetByteTranslator.DecryptBitSizeOperations(outData, _operationsArray2);
            uint u1 = BitConverter.ToUInt32(outData.ToArray(), outData.Count - 8);
            uint u2 = BitConverter.ToUInt32(outData.ToArray(), outData.Count - 4);
            return checkSum1 == u1 && checkSum2 == u2;
        }

        public static bool IsMessageCorrectS(List<byte> outData)
        {
            uint checkSum1 = NetByteTranslator.DecryptBitSizeOperations(outData, _operationsArray1);
            uint checkSum2 = NetByteTranslator.DecryptBitSizeOperations(outData, _operationsArray2);
            uint u1 = BitConverter.ToUInt32(outData.ToArray(), outData.Count - 8);
            uint u2 = BitConverter.ToUInt32(outData.ToArray(), outData.Count - 4);
            return checkSum1 == u1 && checkSum2 == u2;
        }
    }

    public abstract class OrderableMessage<PayloadType> : BaseMessage<PayloadType>
    {
        protected static ulong messageID = 0;

        protected OrderableMessage(PayloadType data, MessageFlags messageFlags = MessageFlags.CheckSum) : base(data,
            messageFlags)
        {
        }

        protected OrderableMessage() : base()
        {
        }

        protected override void BasicSerialize(List<byte> outData, MessageType type, int newPlayerID)
        {
            outData.AddRange(BitConverter.GetBytes((int)type));
            outData.AddRange(BitConverter.GetBytes(newPlayerID));
            outData.AddRange(BitConverter.GetBytes((int)Flags));
            outData.AddRange(BitConverter.GetBytes(messageID++));
            SetOffset();
        }

        protected override void SetOffset()
        {
            offsetSize = sizeof(int) * 3 + sizeof(ulong);
        }
    }

    public abstract class INetObjectMessage<PayloadType> : OrderableMessage<PayloadType>
    {
        public int objectId;
        public List<Route> valueId = new List<Route>();

        public INetObjectMessage()
        {
        }

        protected INetObjectMessage(PayloadType data, int objId, List<Route> valId,
            MessageFlags messageFlags = MessageFlags.CheckSum) : base(data, messageFlags)
        {
            Data = data;
            objectId = objId;
            valueId = valId;
            this.Flags = messageFlags;
            SetOffset();
        }

        protected override void BasicSerialize(List<byte> outData, MessageType type, int newPlayerID)
        {
            outData.AddRange(BitConverter.GetBytes((int)type));
            outData.AddRange(BitConverter.GetBytes(newPlayerID));
            outData.AddRange(BitConverter.GetBytes((int)Flags));
            outData.AddRange(BitConverter.GetBytes(messageID++));
            outData.AddRange(BitConverter.GetBytes(objectId));
            outData.AddRange(BitConverter.GetBytes(valueId.Count));
            foreach (Route t in valueId)
            {
                outData.AddRange(BitConverter.GetBytes(t.id));
                outData.AddRange(BitConverter.GetBytes(t.collectionPos));
                outData.AddRange(BitConverter.GetBytes(t.collectionSize));
            }

            SetOffset();
        }

        public  NetObjectBasicData GetNetObjectData(byte[] data)
        {
            int objID = BitConverter.ToInt32(data, 20);
            int listOffset = 24;
            int intIndex = BitConverter.ToInt32(data, listOffset);

            List<Route> idValues = new List<Route>();
            for (int i = 0; i < intIndex; i++)
            {
                listOffset += 4;
                int id =BitConverter.ToInt32(data, listOffset);
                listOffset += 4;
               int colPos =(BitConverter.ToInt32(data, listOffset));
                listOffset += 4;
                int colSize = (BitConverter.ToInt32(data, listOffset));
                idValues.Add(new Route(id,colPos,colSize));
            }

            return new NetObjectBasicData(objID, idValues);
        }

        protected override void SetOffset()
        {
            offsetSize = sizeof(int) * 5 + sizeof(ulong);
            offsetSize += valueId.Count * sizeof(int)*3;
            Console.WriteLine($"Reading byte:{offsetSize}");
        }

        protected int GetOffsetByBytes(byte[] data)
        {
            int listOffset = 24;
            int intIndex = BitConverter.ToInt32(data, listOffset);

            for (int i = 0; i < intIndex; i++)
            {
                listOffset += 4*3;
            }

            return listOffset+4;
        }
    }

    public class NetHandShakeOK : OrderableMessage<List<Player>>
    {
        private const MessageFlags DefaultFlags =
            MessageFlags.CheckSum | MessageFlags.Ordenable | MessageFlags.Important;

        public NetHandShakeOK(List<Player> clients, MessageFlags messageFlags = DefaultFlags)
        {
            Data = clients;
            MsgType = MessageType.HandShakeOk;
            Flags = messageFlags;
        }

        public NetHandShakeOK() : base()
        {
            MsgType = MessageType.HandShakeOk;
        }

        public override byte[] Serialize(int newPlayerId)
        {
            List<byte> outData = new List<byte>();

            int listSize = Data.Count;
            BasicSerialize(outData, MsgType, newPlayerId);
            outData.AddRange(BitConverter.GetBytes(Data.Count));

            for (int i = 0; i < listSize; i++)
            {
                outData.AddRange(BitConverter.GetBytes(Data[i].id));
                outData.AddRange(BitConverter.GetBytes(Data[i].nameTag.Length));
                for (int j = 0; j < Data[i].nameTag.Length; j++)
                {
                    outData.Add((byte)Data[i].nameTag[j]);
                }
            }

            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override object Deserialize(byte[] message)
        {
            List<Player> clients = new List<Player>();

            int maxClients = BitConverter.ToInt32(message, offsetSize);
            int baseByte = offsetSize + 4;
            int wordsUpTo = 0;
            for (int i = 0; i < maxClients; i++)
            {
                int currentClientId = BitConverter.ToInt32(message, baseByte + wordsUpTo);
                int clientNameLength = BitConverter.ToInt32(message, baseByte + wordsUpTo + 4);

                StringBuilder clientNameBuilder = new StringBuilder();
                for (int j = 0; j < clientNameLength; j++)
                {
                    char currentChar = (char)message[baseByte + wordsUpTo + 8 + j];
                    clientNameBuilder.Append(currentChar);
                }

                string clientName = clientNameBuilder.ToString();
                wordsUpTo += 8 + clientNameLength;
                // Debug.Log("New name" + clientName);
                clients.Add(new Player(currentClientId, clientName));
            }

            return clients;
        }

        public static object DeserializeStatic(byte[] message)
        {
            NetHandShakeOK aux = new NetHandShakeOK();
            return aux.Deserialize(message);
        }
    }

    public class NetServerDirection : OrderableMessage<(string, int)>
    {
        private const MessageFlags DefaultFlags =
            MessageFlags.CheckSum | MessageFlags.Ordenable | MessageFlags.Important;

        public NetServerDirection((string, int) newData, MessageFlags messageFlags = DefaultFlags)
        {
            Data = newData;
            MsgType = MessageType.ServerDir;
            Flags = messageFlags;
        }

        public NetServerDirection() : base()
        {
            MsgType = MessageType.ServerDir;
        }

        public override byte[] Serialize(int newPlayerId)
        {
            List<byte> outData = new List<byte>();

            int listSize = Data.Item1.Length;
            BasicSerialize(outData, MsgType, newPlayerId);
            outData.AddRange(BitConverter.GetBytes(listSize));

            for (int i = 0; i < Data.Item1.Length; i++)
            {
                outData.Add((byte)Data.Item1[i]);
            }

            outData.AddRange(BitConverter.GetBytes(Data.Item2));
            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override object Deserialize(byte[] message)
        {
            (string, int) outData;
            outData.Item1 = "";
            int max = BitConverter.ToInt32(message, offsetSize);
            for (int i = 0; i < max; i++)
            {
                outData.Item1 += (char)message[offsetSize + 4 + i];
            }

            outData.Item2 = BitConverter.ToInt32(message, offsetSize + 4 + max);
            Data.Item1 = outData.Item1;
            Data.Item2 = outData.Item2;
            return outData;
        }

        public static object DeserializeStatic(byte[] message)
        {
            NetServerDirection aux = new NetServerDirection();
            return aux.Deserialize(message);
        }
    }

    public class NetHandShake : BaseMessage<string>
    {
        public NetHandShake(string tag) : base()
        {
            Data = tag;
            MsgType = MessageType.HandShake;
        }

        public NetHandShake() : base()
        {
            MsgType = MessageType.HandShake;
        }

        public override object Deserialize(byte[] message)
        {
            string outData = "";
            int max = BitConverter.ToInt32(message, offsetSize);
            for (int i = 0; i < max; i++)
            {
                //outData += BitConverter.ToChar(message, 16 + i);
                outData += (char)message[offsetSize + 4 + i];
            }

            Data = outData;
            return outData;
        }

        public override byte[] Serialize(int newPlayerId)
        {
            List<byte> outData = new List<byte>();

            BasicSerialize(outData, MsgType, newPlayerId);

            outData.AddRange(BitConverter.GetBytes(Data.Length));
            for (int i = 0; i < Data.Length; i++)
            {
                outData.Add((byte)Data[i]);
            }

            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public static object DeserializeStatic(byte[] message)
        {
            NetHandShake aux = new NetHandShake();
            return aux.Deserialize(message);
        }
    }

    public class NetExit : BaseMessage<string>
    {
        public NetExit() : base()
        {
            MsgType = MessageType.Exit;
            Flags = MessageFlags.CheckSum;
        }

        public NetExit(string ExitMessage) : base(ExitMessage)
        {
            MsgType = MessageType.Exit;
            Flags = MessageFlags.CheckSum;
            Data = ExitMessage;
        }

        public override object Deserialize(byte[] message)
        {
            string outData = "";
            SetOffset();
            int messageLength = BitConverter.ToInt32(message, offsetSize);
            for (int i = 0; i < messageLength; i++)
            {
                outData += (char)message[offsetSize + 4 + i];
            }

            return outData;
        }

        public static object DeserializeStatic(byte[] message)
        {
            NetConsole aux = new NetConsole();
            return aux.Deserialize(message);
        }

        public override byte[] Serialize(int newPlayerId)
        {
            List<byte> outData = new List<byte>();
            BasicSerialize(outData, MsgType, newPlayerId);
            outData.AddRange(BitConverter.GetBytes(Data.Length));


            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }
    }

    public class NetTRS : INetObjectMessage<TRS>
    {
        public NetTRS(TRS data, int objId, List<Route> valId, MessageFlags messageFlags = MessageFlags.CheckSum) :
            base(data, objId, valId, messageFlags)
        {
            MsgType = MessageType.TRS;
            Flags = messageFlags;
            Data = data;
        }

        public override byte[] Serialize(int newPlayerId)
        {
            List<byte> outData = new List<byte>();
            BasicSerialize(outData, MsgType, newPlayerId);
            outData.AddRange(BitConverter.GetBytes(Data.position.X));
            outData.AddRange(BitConverter.GetBytes(Data.position.Y));
            outData.AddRange(BitConverter.GetBytes(Data.position.Z));
            outData.AddRange(BitConverter.GetBytes(Data.rotation.X));
            outData.AddRange(BitConverter.GetBytes(Data.rotation.Y));
            outData.AddRange(BitConverter.GetBytes(Data.rotation.Z));
            outData.AddRange(BitConverter.GetBytes(Data.scale.X));
            outData.AddRange(BitConverter.GetBytes(Data.scale.Y));
            outData.AddRange(BitConverter.GetBytes(Data.scale.Z));
            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override object Deserialize(byte[] message)
        {
            TRS aux = new TRS();
            Vector3 pos = new Vector3(BitConverter.ToSingle(message, offsetSize),
                BitConverter.ToSingle(message, offsetSize + 4), BitConverter.ToSingle(message, offsetSize + 8));
            Vector3 rot = new Vector3(BitConverter.ToSingle(message, offsetSize + 12),
                BitConverter.ToSingle(message, offsetSize + 16), BitConverter.ToSingle(message, offsetSize + 20));
            Vector3 scale = new Vector3(BitConverter.ToSingle(message, offsetSize + 24),
                BitConverter.ToSingle(message, offsetSize + 28), BitConverter.ToSingle(message, offsetSize + 32));
            return aux;
        }
    }

    public class NetGetObjectID : OrderableMessage<AskForNetObject>
    {
        public NetGetObjectID() : base()
        {
            MsgType = MessageType.AskForObject;
            Flags = MessageFlags.Ordenable | MessageFlags.Important | MessageFlags.CheckSum;
        }

        public NetGetObjectID(AskForNetObject data) : base()
        {
            Data = data;
            MsgType = MessageType.AskForObject;
            Flags = MessageFlags.Ordenable | MessageFlags.Important | MessageFlags.CheckSum;
        }

        //Todo: Change to list
        public override byte[] Serialize(int playerId)
        {
            List<byte> outData = new List<byte>();
            BasicSerialize(outData, MsgType, playerId);
            outData.AddRange(BitConverter.GetBytes(Data.objectType));
            outData.AddRange(BitConverter.GetBytes(Data.intanceID));
            outData.AddRange(BitConverter.GetBytes(Data.owner));
            outData.AddRange(BitConverter.GetBytes(Data.pos.X));
            outData.AddRange(BitConverter.GetBytes(Data.pos.Y));
            outData.AddRange(BitConverter.GetBytes(Data.pos.Z));
            outData.AddRange(BitConverter.GetBytes(Data.rot.X));
            outData.AddRange(BitConverter.GetBytes(Data.rot.Y));
            outData.AddRange(BitConverter.GetBytes(Data.rot.Z));
            outData.AddRange(BitConverter.GetBytes(Data.scale.X));
            outData.AddRange(BitConverter.GetBytes(Data.scale.Y));
            outData.AddRange(BitConverter.GetBytes(Data.scale.Z));
            outData.AddRange(BitConverter.GetBytes(Data.parentId));
            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override object Deserialize(byte[] message)
        {
            AskForNetObject aux = new AskForNetObject();
            aux.objectType = BitConverter.ToInt32(message, offsetSize);
            aux.intanceID = BitConverter.ToInt32(message, offsetSize + 4);
            aux.owner = BitConverter.ToInt32(message, offsetSize + 8);
            int offset = offsetSize + 8;
            aux.pos = new Vector3(BitConverter.ToSingle(message, offset + 4),
                BitConverter.ToSingle(message, offset + 8), BitConverter.ToSingle(message, offset + 12));
            aux.rot = new Vector3(BitConverter.ToSingle(message, offset + 16),
                BitConverter.ToSingle(message, offset + 20), BitConverter.ToSingle(message, offset + 24));
            aux.scale = new Vector3(BitConverter.ToSingle(message, offset + 28),
                BitConverter.ToSingle(message, offset + 32), BitConverter.ToSingle(message, offset + 36));
            aux.parentId = BitConverter.ToInt32(message, offset + 40);
            return aux;
        }
    }

//Send int of position, int of type of object, position and forward
//Todo: Player id(variable de quien pisar)
//object id (el valor unico que le asigno con el factory)
//el id de la variable ( que voy a pisar)
//los bytes a pisar


    public class NetConsole : OrderableMessage<string>
    {
        private string data;

        public NetConsole() : base()
        {
            MsgType = MessageType.Message;
            Flags = MessageFlags.CheckSum | MessageFlags.Ordenable | MessageFlags.Important;
        }

        public NetConsole(string data) : base()
        {
            this.data = data;
            MsgType = MessageType.Message;
            Flags = MessageFlags.CheckSum | MessageFlags.Ordenable | MessageFlags.Important;
        }


        public override object Deserialize(byte[] message)
        {
            string outData = "";
            SetOffset();
            int messageLength = BitConverter.ToInt32(message, offsetSize);
            for (int i = 0; i < messageLength; i++)
            {
                outData += (char)message[offsetSize + 4 + i];
            }

            return outData;
        }

        public static object DeserializeStatic(byte[] message)
        {
            NetConsole aux = new NetConsole();
            return aux.Deserialize(message);
        }

        public override byte[] Serialize(int newPlayerId)
        {
            List<byte> outData = new List<byte>();
            BasicSerialize(outData, MsgType, newPlayerId);
            outData.AddRange(BitConverter.GetBytes(data.Length));
            for (int i = 0; i < data.Length; i++)
            {
                outData.Add((byte)data[i]);
            }

            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }
    }

    public class NetPing : BaseMessage<int>
    {
        public NetPing() : base()
        {
            MsgType = MessageType.Ping;
        }

        public override byte[] Serialize(int newPlayerId)
        {
            List<byte> outData = new List<byte>();
            BasicSerialize(outData, MsgType, newPlayerId);
            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override object Deserialize(byte[] message)
        {
            return BitConverter.ToInt32(message, 4);
        }

        public static object DeserializeStatic(byte[] message)
        {
            NetPing aux = new NetPing();
            return aux.Deserialize(message);
        }
    }

    public class NetTime : OrderableMessage<float>
    {
        public NetTime() : base()
        {
            MsgType = MessageType.Timer;
            Flags = MessageFlags.CheckSum | MessageFlags.Important | MessageFlags.Ordenable;
        }

        public NetTime(float second) : base()
        {
            Data = second;
            MsgType = MessageType.Timer;
            Flags = MessageFlags.CheckSum | MessageFlags.Important | MessageFlags.Ordenable;
        }

        public override byte[] Serialize(int playerId)
        {
            List<byte> outData = new List<byte>();
            BasicSerialize(outData, MsgType, playerId);
            outData.AddRange(BitConverter.GetBytes(Data));
            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override object Deserialize(byte[] message)
        {
            return BitConverter.ToSingle(message, offsetSize);
        }
    }

    public class NetConfirmation : BaseMessage<(MessageType, ulong)>
    {
        public NetConfirmation() : base()
        {
            MsgType = MessageType.Confirmation;
        }

        public NetConfirmation((MessageType, ulong) data) : base(data)
        {
            MsgType = MessageType.Confirmation;
        }

        public override byte[] Serialize(int newPlayerId)
        {
            List<byte> outData = new List<byte>();
            BasicSerialize(outData, MsgType, newPlayerId);
            outData.AddRange(BitConverter.GetBytes((int)Data.Item1));
            outData.AddRange(BitConverter.GetBytes(Data.Item2));
            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override object Deserialize(byte[] message)
        {
            var type = (MessageType)BitConverter.ToInt32(message, offsetSize);
            var messageId = BitConverter.ToUInt64(message, offsetSize + 4);

            return (type, messageId);
        }

        public static object DeserializeStatic(byte[] message)
        {
            NetConfirmation aux = new NetConfirmation();
            return aux.Deserialize(message);
        }
    }

    [NetType(MessageType.Float,typeof(float))]
    public class NetFloat : INetObjectMessage<float>
    {
        public NetFloat() : base()
        {
            MsgType = MessageType.Float;
            SetOffset();
        }

        public NetFloat(float data, int objId, List<Route> valId, MessageFlags messageFlags = MessageFlags.CheckSum) :
            base(data, objId, valId, messageFlags)
        {
            Data = data;
            MsgType = MessageType.Float;
        }

        public override byte[] Serialize(int playerId)
        {
            List<byte> outData = new List<byte>();
            BasicSerialize(outData, MsgType, playerId);
            Console.WriteLine(Data);
            outData.AddRange(BitConverter.GetBytes(Data));
            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override object Deserialize(byte[] message)
        {
            int offsetByBytes = GetOffsetByBytes(message);
            Console.WriteLine(offsetByBytes);
            return BitConverter.ToSingle(message, offsetByBytes);
        }
    }
    [NetType(MessageType.Int,typeof(int))]
    public class NetInt : INetObjectMessage<int>
    {
        public NetInt() : base()
        {
            MsgType = MessageType.Int;
        }

        public NetInt(int data, int objId, List<Route> valId, MessageFlags messageFlags = MessageFlags.CheckSum) :
            base(data, objId, valId, messageFlags)
        {
            MsgType = MessageType.Int;
        }

        public override byte[] Serialize(int playerId)
        {
            List<byte> outData = new List<byte>();
            BasicSerialize(outData, MsgType, playerId);
            outData.AddRange(BitConverter.GetBytes(Data));
            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override object Deserialize(byte[] message)
        {
            return BitConverter.ToInt32(message, GetOffsetByBytes(message));
        }
    }
    [NetType(MessageType.UInt,typeof(uint))]
    public class NetUInt : INetObjectMessage<uint>
    {
        public NetUInt(uint data, int objId, List<Route> valId, MessageFlags messageFlags = MessageFlags.CheckSum) :
            base(data, objId, valId, messageFlags)
        {
            MsgType = MessageType.UInt;
        }

        public NetUInt()
        {
            MsgType = MessageType.UInt;
        }

        public override byte[] Serialize(int playerId)
        {
            List<byte> outData = new List<byte>();
            BasicSerialize(outData, MsgType, playerId);
            outData.AddRange(BitConverter.GetBytes(Data));
            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override object Deserialize(byte[] message)
        {
            return BitConverter.ToUInt32(message, GetOffsetByBytes(message));
        }
    }
    [NetType(MessageType.Bool,typeof(bool))]
    public class NetBool : INetObjectMessage<bool>
    {
        public NetBool(bool data, int objId, List<Route> valId, MessageFlags messageFlags = MessageFlags.CheckSum) :
            base(data, objId, valId, messageFlags)
        {
            MsgType = MessageType.Bool;
        }

        NetBool()
        {
            MsgType = MessageType.Bool;
        }

        public override byte[] Serialize(int playerId)
        {
            List<byte> outData = new List<byte>();
            BasicSerialize(outData, MsgType, playerId);
            outData.AddRange(BitConverter.GetBytes(Data));
            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override object Deserialize(byte[] message)
        {
            return BitConverter.ToBoolean(message, GetOffsetByBytes(message));
        }
    }
    [NetType(MessageType.String,typeof(string))]
    public class NetString : INetObjectMessage<string>
    {
        public NetString(string data, int objId, List<Route> valId, MessageFlags messageFlags = MessageFlags.CheckSum) :
            base(data, objId, valId, messageFlags)
        {
            MsgType = MessageType.String;
        }

        public NetString()
        {
            MsgType = MessageType.String;
        }

        public override byte[] Serialize(int playerId)
        {
            List<byte> outData = new List<byte>();
            BasicSerialize(outData, MsgType, playerId);
            outData.AddRange(BitConverter.GetBytes(Data.Length));
            foreach (char c in Data)
            {
                outData.Add((byte)c);
            }

            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override object Deserialize(byte[] message)
        {
            string aux = "";
            int stringSize = BitConverter.ToInt32(message, GetOffsetByBytes(message));
            for (int i = 0; i < stringSize; i++)
            {
                int offsetByBytes = GetOffsetByBytes(message);
                aux += (char)message[offsetByBytes + 4 + i];
            }

            return aux;
        }
    }
    [NetType(MessageType.Short,typeof(short))]
    public class NetShort : INetObjectMessage<short>
    {
        NetShort()
        {
            MsgType = MessageType.Short;
        }

        public NetShort(short data, int objId, List<Route> valId, MessageFlags messageFlags = MessageFlags.CheckSum) :
            base(data, objId, valId, messageFlags)
        {
            MsgType = MessageType.Short;
        }

        public override byte[] Serialize(int playerId)
        {
            List<byte> outData = new List<byte>();
            BasicSerialize(outData, MsgType, playerId);
            outData.AddRange(BitConverter.GetBytes(Data));
            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override object Deserialize(byte[] message)
        {
            return BitConverter.ToInt16(message, GetOffsetByBytes(message));
        }
    }
    [NetType(MessageType.UShort,typeof(ushort))]
    public class NetUShort : INetObjectMessage<ushort>
    {
        NetUShort()
        {
            MsgType = MessageType.UShort;
        }

        public NetUShort(ushort data, int objId, List<Route> valId, MessageFlags messageFlags = MessageFlags.CheckSum) :
            base(data, objId, valId, messageFlags)
        {
            MsgType = MessageType.UShort;
        }

        public override byte[] Serialize(int playerId)
        {
            List<byte> outData = new List<byte>();
            BasicSerialize(outData, MsgType, playerId);
            outData.AddRange(BitConverter.GetBytes(Data));
            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override object Deserialize(byte[] message)
        {
            return BitConverter.ToUInt16(message, GetOffsetByBytes(message));
        }
    }
    [NetType(MessageType.Long,typeof(long))]
    public class NetLong : INetObjectMessage<long>
    {
        NetLong()
        {
            MsgType = MessageType.Long;
        }

        public NetLong(long data, int objId, List<Route> valId, MessageFlags messageFlags = MessageFlags.CheckSum) :
            base(data, objId, valId, messageFlags)
        {
            MsgType = MessageType.Long;
        }

        public override byte[] Serialize(int playerId)
        {
            List<byte> outData = new List<byte>();
            BasicSerialize(outData, MsgType, playerId);
            outData.AddRange(BitConverter.GetBytes(Data));
            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override object Deserialize(byte[] message)
        {
            return BitConverter.ToInt64(message, GetOffsetByBytes(message));
        }
    }
    [NetType(MessageType.ULong,typeof(ulong))]
    public class NetULong : INetObjectMessage<ulong>
    {
        NetULong()
        {
            MsgType = MessageType.ULong;
        }

        public NetULong(ulong data, int objId, List<Route> valId, MessageFlags messageFlags = MessageFlags.CheckSum) :
            base(data, objId, valId, messageFlags)
        {
            MsgType = MessageType.ULong;
        }

        public override byte[] Serialize(int playerId)
        {
            List<byte> outData = new List<byte>();
            BasicSerialize(outData, MsgType, playerId);
            outData.AddRange(BitConverter.GetBytes(Data));
            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override object Deserialize(byte[] message)
        {
            return BitConverter.ToUInt64(message, GetOffsetByBytes(message));
        }
    }
    [NetType(MessageType.Byte,typeof(byte))]
    public class NetByte : INetObjectMessage<byte>
    {
        NetByte()
        {
            MsgType = MessageType.Byte;
        }

        public NetByte(byte data, int objId, List<Route> valId, MessageFlags messageFlags = MessageFlags.CheckSum) :
            base(data, objId, valId, messageFlags)
        {
            MsgType = MessageType.Byte;
        }

        public override byte[] Serialize(int playerId)
        {
            List<byte> outData = new List<byte>();
            BasicSerialize(outData, MsgType, playerId);
            outData.Add(Data);
            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override object Deserialize(byte[] message)
        {
            return message[offsetSize];
        }
    }
    [NetType(MessageType.SByte,typeof(sbyte))]
    public class NetSByte : INetObjectMessage<sbyte>
    {
        public NetSByte() : base()
        {
            MsgType = MessageType.SByte;
        }

        public NetSByte(sbyte data, int objId, List<Route> valId, MessageFlags messageFlags = MessageFlags.CheckSum) :
            base(data, objId, valId, messageFlags)
        {
            MsgType = MessageType.SByte;
        }

        public override byte[] Serialize(int playerId)
        {
            List<byte> outData = new List<byte>();
            BasicSerialize(outData, MsgType, playerId);
            outData.Add((byte)Data);
            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override object Deserialize(byte[] message)
        {
            return (sbyte)message[GetOffsetByBytes(message)];
        }
    }
    [NetType(MessageType.Char,typeof(char))]
    public class NetChar : INetObjectMessage<char>
    {
        public NetChar() : base()
        {
            MsgType = MessageType.Char;
        }

        public NetChar(char data, int objId, List<Route> valId, MessageFlags messageFlags = MessageFlags.CheckSum) :
            base(data, objId, valId, messageFlags)
        {
            MsgType = MessageType.Char;
        }

        public override byte[] Serialize(int playerId)
        {
            List<byte> outData = new List<byte>();
            BasicSerialize(outData, MsgType, playerId);
            outData.Add((byte)Data);
            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override object Deserialize(byte[] message)
        {
            return (char)message[GetOffsetByBytes(message)];
        }
    }
    [NetType(MessageType.Double,typeof(double))]
    public class NetDouble : INetObjectMessage<double>
    {
        NetDouble()
        {
            MsgType = MessageType.Double;
        }

        public NetDouble(double data, int objId, List<Route> valId, MessageFlags messageFlags = MessageFlags.CheckSum) :
            base(data, objId, valId, messageFlags)
        {
            MsgType = MessageType.Double;
        }

        public override byte[] Serialize(int playerId)
        {
            List<byte> outData = new List<byte>();
            BasicSerialize(outData, MsgType, playerId);
            outData.AddRange(BitConverter.GetBytes(Data));
            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override object Deserialize(byte[] message)
        {
            return BitConverter.ToDouble(message, GetOffsetByBytes(message));
        }
    }
    [NetType(MessageType.Decimal,typeof(decimal))]
    public class NetDecimal : INetObjectMessage<decimal>
    {
        NetDecimal()
        {
            MsgType = MessageType.Decimal;
        }

        public NetDecimal(decimal data, int objId, List<Route> valId, MessageFlags messageFlags = MessageFlags.CheckSum) :
            base(data, objId, valId, messageFlags)
        {
            MsgType = MessageType.Decimal;
        }

        public override byte[] Serialize(int playerId)
        {
            List<byte> outData = new List<byte>();
            BasicSerialize(outData, MsgType, playerId);
            // Convert decimal to byte array
            int[] bits = decimal.GetBits(Data);
            foreach (int bit in bits)
            {
                outData.AddRange(BitConverter.GetBytes(bit));
            }

            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override object Deserialize(byte[] message)
        {
            // Convert byte array back to decimal
            int[] bits = new int[4];
            int offsetByBytes = GetOffsetByBytes(message);
            for (int i = 0; i < 4; i++)
            {
                bits[i] = BitConverter.ToInt32(message, offsetByBytes + i * 4);
            }

            return new decimal(bits);
        }
    }
}