using System;
using System.Runtime;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace RojoinNetworkSystem
{
    public enum MessageType
    {
        Error = -3,
        HandShake = -2,
        HandShakeOk = -1,
        Position = 1,
        String = 2,
        Ping,
        Confirmation,
        PositionAndRotation,
        AskForObject,
        Exit,
        Damage,
        Timer
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

        public abstract PayloadType Deserialize(byte[] message);


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

        public override List<Player> Deserialize(byte[] message)
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

        public static List<Player> DeserializeStatic(byte[] message)
        {
            NetHandShakeOK aux = new NetHandShakeOK();
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

        public override string Deserialize(byte[] message)
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

        public static string DeserializeStatic(byte[] message)
        {
            NetHandShake aux = new();
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

        public override string Deserialize(byte[] message)
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

        public static string DeserializeStatic(byte[] message)
        {
            NetConsole aux = new();
            return aux.Deserialize(message);
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
    }

    public class NetDamage : OrderableMessage<int>
    {
        public NetDamage() : base()
        {
            MsgType = MessageType.Damage;
            Flags = MessageFlags.Ordenable | MessageFlags.Important | MessageFlags.CheckSum;
        }

        public NetDamage(int id) : base()
        {
            MsgType = MessageType.Damage;
            Flags = MessageFlags.Ordenable | MessageFlags.Important | MessageFlags.CheckSum;
            Data = id;
        }

        public override byte[] Serialize(int newPlayerId)
        {
            List<byte> outData = new List<byte>();
            BasicSerialize(outData, MsgType, newPlayerId);
            outData.AddRange(BitConverter.GetBytes(Data));
            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override int Deserialize(byte[] message)
        {
            return BitConverter.ToInt32(message, offsetSize);
        }

        public static int DeserializeStatic(byte[] message)
        {
            NetDamage aux = new();
            return aux.Deserialize(message);
        }
    }

    public class NetPlayerPos : OrderableMessage<(Vector3, int)>
    {
        public NetPlayerPos(Vector3 data, int id) : base()
        {
            this.Data.Item1 = data;
            this.Data.Item2 = id;
            MsgType = MessageType.Position;
            Flags = MessageFlags.Ordenable | MessageFlags.Important | MessageFlags.CheckSum;
            SetOffset();
        }

        public NetPlayerPos() : base()
        {
        }

        public override (Vector3, int) Deserialize(byte[] message)
        {
            (Vector3, int) outData;

            outData.Item1.X = BitConverter.ToSingle(message, offsetSize);
            outData.Item1.Y = BitConverter.ToSingle(message, offsetSize + 4);
            outData.Item1.Z = BitConverter.ToSingle(message, offsetSize + 8);
            outData.Item2 = BitConverter.ToInt32(message, offsetSize + 12);
            return outData;
        }

        public override byte[] Serialize(int newPlayerId)
        {
            List<byte> outData = new List<byte>();

            BasicSerialize(outData, MsgType, newPlayerId);
            outData.AddRange(BitConverter.GetBytes(Data.Item1.X));
            outData.AddRange(BitConverter.GetBytes(Data.Item1.Y));
            outData.AddRange(BitConverter.GetBytes(Data.Item1.Z));
            outData.AddRange(BitConverter.GetBytes(Data.Item2));

            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public static (Vector3, int) DeserializeStatic(byte[] message)
        {
            NetPlayerPos aux = new();
            return aux.Deserialize(message);
        }
    }

    public class NetSpawnObject : OrderableMessage<(int type, Vector3 pos, Vector3 forw)>
    {
        public NetSpawnObject() : base()
        {
            MsgType = MessageType.AskForObject;
            Flags = MessageFlags.Ordenable | MessageFlags.Important | MessageFlags.CheckSum;
        }

        public NetSpawnObject(int id, Vector3 pos, Vector3 forward) : base()
        {
            MsgType = MessageType.AskForObject;
            Flags = MessageFlags.Ordenable | MessageFlags.Important | MessageFlags.CheckSum;
            Data.type = id;
            Data.pos = pos;
            Data.forw = forward;
        }

        public override byte[] Serialize(int playerId)
        {
            List<byte> outData = new List<byte>();

            BasicSerialize(outData, MsgType, playerId);
            outData.AddRange(BitConverter.GetBytes(Data.Item1));
            outData.AddRange(BitConverter.GetBytes(Data.Item2.X));
            outData.AddRange(BitConverter.GetBytes(Data.Item2.Y));
            outData.AddRange(BitConverter.GetBytes(Data.Item2.Z));
            outData.AddRange(BitConverter.GetBytes(Data.Item3.X));
            outData.AddRange(BitConverter.GetBytes(Data.Item3.Y));
            outData.AddRange(BitConverter.GetBytes(Data.Item3.Z));

            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override (int, Vector3, Vector3) Deserialize(byte[] message)
        {
            (int index, Vector3 pos, Vector3 forw) outData;

            outData.index = BitConverter.ToInt32(message, offsetSize);
            outData.pos.X = BitConverter.ToSingle(message, offsetSize + 4);
            outData.pos.Y = BitConverter.ToSingle(message, offsetSize + 8);
            outData.pos.Z = BitConverter.ToSingle(message, offsetSize + 12);
            outData.forw.X = BitConverter.ToSingle(message, offsetSize + 16);
            outData.forw.Y = BitConverter.ToSingle(message, offsetSize + 20);
            outData.forw.Z = BitConverter.ToSingle(message, offsetSize + 24);
            return outData;
        }

        public static (int, Vector3, Vector3) DeserializeStatic(byte[] message)
        {
            NetSpawnObject aux = new();
            return aux.Deserialize(message);
        }
    }

//Send int of position, int of type of object, position and forward
    public class NetPositionAndRotation : OrderableMessage<(int id, int type, Vector3 pos, Vector3 forw)>
    {
        public NetPositionAndRotation() : base()
        {
            MsgType = MessageType.PositionAndRotation;
        }

        public NetPositionAndRotation(int id, int type, Vector3 pos, Vector3 forward) : base()
        {
            MsgType = MessageType.PositionAndRotation;
            Flags = MessageFlags.Ordenable | MessageFlags.Important | MessageFlags.CheckSum;
            Data.id = id;
            Data.type = type;
            Data.pos = pos;
            Data.forw = forward;
        }

        public override byte[] Serialize(int playerId)
        {
            List<byte> outData = new List<byte>();

            BasicSerialize(outData, MsgType, playerId);
            outData.AddRange(BitConverter.GetBytes(Data.Item1));
            outData.AddRange(BitConverter.GetBytes(Data.Item2));
            outData.AddRange(BitConverter.GetBytes(Data.Item3.X));
            outData.AddRange(BitConverter.GetBytes(Data.Item3.Y));
            outData.AddRange(BitConverter.GetBytes(Data.Item3.Z));
            outData.AddRange(BitConverter.GetBytes(Data.Item4.X));
            outData.AddRange(BitConverter.GetBytes(Data.Item4.Y));
            outData.AddRange(BitConverter.GetBytes(Data.Item4.Z));
            DataCheckSumEncryption(outData);
            return outData.ToArray();
        }

        public override (int, int, Vector3, Vector3) Deserialize(byte[] message)
        {
            (int id, int index, Vector3 pos, Vector3 forw) outData;

            int size = 0;
            outData.id = BitConverter.ToInt32(message, offsetSize);
            outData.index = BitConverter.ToInt32(message, offsetSize + 4);
            outData.pos.X = BitConverter.ToSingle(message, offsetSize + 8);
            outData.pos.Y = BitConverter.ToSingle(message, offsetSize + 12);
            outData.pos.Z = BitConverter.ToSingle(message, offsetSize + 16);
            outData.forw.X = BitConverter.ToSingle(message, offsetSize + 20);
            outData.forw.Y = BitConverter.ToSingle(message, offsetSize + 24);
            outData.forw.Z = BitConverter.ToSingle(message, offsetSize + 28);
            return outData;
        }

        public static (int, int, Vector3, Vector3) DeserializeStatic(byte[] message)
        {
            NetPositionAndRotation aux = new();
            return aux.Deserialize(message);
        }
    }

    public class NetConsole : OrderableMessage<string>
    {
        private string data;

        public NetConsole() : base()
        {
            MsgType = MessageType.String;
            Flags = MessageFlags.CheckSum | MessageFlags.Ordenable | MessageFlags.Important;
        }

        public NetConsole(string data) : base()
        {
            this.data = data;
            MsgType = MessageType.String;
            Flags = MessageFlags.CheckSum | MessageFlags.Ordenable | MessageFlags.Important;
        }


        public override string Deserialize(byte[] message)
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

        public static string DeserializeStatic(byte[] message)
        {
            NetConsole aux = new();
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

        public override int Deserialize(byte[] message)
        {
            return BitConverter.ToInt32(message, 4);
        }

        public static int DeserializeStatic(byte[] message)
        {
            NetPing aux = new();
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

        public override float Deserialize(byte[] message)
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

        public override (MessageType, ulong) Deserialize(byte[] message)
        {
            var type = (MessageType)BitConverter.ToInt32(message, offsetSize);
            var messageId = BitConverter.ToUInt64(message, offsetSize + 4);

            return (type, messageId);
        }

        public static (MessageType, ulong) DeserializeStatic(byte[] message)
        {
            NetConfirmation aux = new();
            return aux.Deserialize(message);
        }
    }
}