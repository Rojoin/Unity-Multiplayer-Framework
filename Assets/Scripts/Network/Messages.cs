using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum MessageType
{
    HandShake = -2,
    HandShakeOk = -1,
    Position = 1,
    String = 2,
    Ping,
    Confirmation,
    Exit
}


[Flags]
public enum MessageFlags
{
    None = 0,
    CheckSum = 1,
    Ordenable = 2,
    Important = 4
}


public abstract class BaseMessage<PayloadType>
{
    protected MessageType Type;
    protected PayloadType Data;
    protected MessageFlags Flags;
    public static int PlayerID;

    protected int offsetSize = 0;

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
        return Type;
    }

    public int GetID()
    {
        return PlayerID;
    }

    public abstract byte[] Serialize(int playerId = -999);
   
    public abstract PayloadType Deserialize(byte[] message);

    public PayloadType GetData()
    {
        return Data;
    }

    protected virtual void BasicSerialize(List<byte> outData, MessageType type, int newPlayerID)
    {
        int idToSend = PlayerID;
        if (newPlayerID != -999)
        {
            idToSend = newPlayerID;
        }
        outData.AddRange(BitConverter.GetBytes((int)type));
        outData.AddRange(BitConverter.GetBytes(idToSend));
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
//Todo: Add message when spawning entity
//Todo: Add message for timer.
//Todo: Add message for winning.
public abstract class OrderableMessage<PayloadType> : BaseMessage<PayloadType>
{
    protected static ulong messageID = 0;

    protected override void BasicSerialize(List<byte> outData, MessageType type, int newPlayerID)
    {
        int idToSend = PlayerID;
        if (newPlayerID != -999)
        {
            idToSend = newPlayerID;
        }
        
        outData.AddRange(BitConverter.GetBytes((int)type));
        outData.AddRange(BitConverter.GetBytes(idToSend));
        outData.AddRange(BitConverter.GetBytes((int)Flags));
        outData.AddRange(BitConverter.GetBytes(messageID++));
        Debug.Log(messageID);
        SetOffset();
    }
 
    protected override void SetOffset()
    {
        offsetSize = sizeof(int) * 3 + sizeof(ulong);
    }
}

public class NetHandShakeOK : BaseMessage<List<Player>>
{
    private const MessageFlags DefaultFlags = MessageFlags.CheckSum | MessageFlags.Important;

    public NetHandShakeOK(List<Player> clients, MessageFlags messageFlags = DefaultFlags) : base(clients, messageFlags)
    {
        Data = clients;
        Type = MessageType.HandShakeOk;
        Flags = messageFlags;
    }

    public NetHandShakeOK() : base()
    {
        Type = MessageType.HandShakeOk;
    }

    public override byte[] Serialize(int newPlayerId = -999)
    {
        List<byte> outData = new List<byte>();

        int listSize = Data.Count;
        BasicSerialize(outData, Type,newPlayerId);
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
            Debug.Log("New name" + clientName);
            clients.Add(new Player(currentClientId, clientName));
        }

        return clients;
    }
}

public class NetHandShake : BaseMessage<string>
{
    public NetHandShake(string tag) : base()
    {
        Data = tag;
        Type = MessageType.HandShake;
    }

    public NetHandShake() : base()
    {
        Type = MessageType.HandShake;
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

    public override byte[] Serialize(int newPlayerId = -999)
    {
        List<byte> outData = new List<byte>();

        BasicSerialize(outData, Type,newPlayerId);

        outData.AddRange(BitConverter.GetBytes(Data.Length));
        for (int i = 0; i < Data.Length; i++)
        {
            outData.Add((byte)Data[i]);
        }

        DataCheckSumEncryption(outData);
        return outData.ToArray();
    }
}

public class NetExit : BaseMessage<int>
{
    public NetExit() : base()
    {
        Type = MessageType.Exit;
    }

    public override byte[] Serialize(int newPlayerId = -999)
    {
        List<byte> outData = new List<byte>();
        BasicSerialize(outData, Type,newPlayerId);
        DataCheckSumEncryption(outData);
        return outData.ToArray();
    }

    public override int Deserialize(byte[] message)
    {
        PlayerID = BitConverter.ToInt32(message, 4);
        return PlayerID;
    }
}

public class NetPosition : OrderableMessage<(Vector3, int)>
{
    //TODO: Add id to object that needs to be created
    public NetPosition(Vector3 data, int id) : base()
    {
        this.Data.Item1 = data;
        this.Data.Item2 = id;
        Type = MessageType.Position;
        SetOffset();
    }

    public NetPosition() : base()
    {
    }

    public override (Vector3, int) Deserialize(byte[] message)
    {
        (Vector3, int) outData;

        outData.Item1.x = BitConverter.ToSingle(message, offsetSize);
        outData.Item1.y = BitConverter.ToSingle(message, offsetSize + 4);
        outData.Item1.z = BitConverter.ToSingle(message, offsetSize + 8);
        outData.Item2 = BitConverter.ToInt32(message, offsetSize + 12);
        return outData;
    }

    public override byte[] Serialize(int newPlayerId = -999)
    {
        List<byte> outData = new List<byte>();

        BasicSerialize(outData, Type,newPlayerId);
        outData.AddRange(BitConverter.GetBytes(Data.Item1.x));
        outData.AddRange(BitConverter.GetBytes(Data.Item1.y));
        outData.AddRange(BitConverter.GetBytes(Data.Item1.z));
        outData.AddRange(BitConverter.GetBytes(Data.Item2));

        DataCheckSumEncryption(outData);
        return outData.ToArray();
    }
}

public class NetConsole : OrderableMessage<string>
{
    private string data;

    public NetConsole() : base()
    {
        Type = MessageType.String;
        Flags = MessageFlags.CheckSum | MessageFlags.Ordenable | MessageFlags.Important;
    }

    public NetConsole(string data) : base()
    {
        this.data = data;
        Type = MessageType.String;
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


    public override byte[] Serialize(int newPlayerId = -999)
    {
        List<byte> outData = new List<byte>();
        BasicSerialize(outData, Type,newPlayerId);
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
        Type = MessageType.Ping;
    }

    public override byte[] Serialize(int newPlayerId = -999)
    {
        List<byte> outData = new List<byte>();
        BasicSerialize(outData, Type,newPlayerId);
        DataCheckSumEncryption(outData);
        return outData.ToArray();
    }

    public override int Deserialize(byte[] message)
    {
        return BitConverter.ToInt32(message, 4);
    }
}

public class NetConfirmation : BaseMessage<(MessageType, ulong)>
{
    public NetConfirmation() : base()
    {
        Type = MessageType.Confirmation;
    }

    public NetConfirmation((MessageType, ulong) data) : base(data)
    {
        Type = MessageType.Confirmation;
    }
    
    public override byte[] Serialize(int newPlayerId = -999)
    {
        List<byte> outData = new List<byte>();
        BasicSerialize(outData, Type,newPlayerId);
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
}