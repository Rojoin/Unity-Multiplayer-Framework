using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using System.ComponentModel;

public enum MessageType
{
    HandShake = -1,
    Console = 0,
    Position = 1,
    String = 2
}

public interface IMessage<T>
{
    public MessageType GetMessageType();
    public byte[] Serialize();
    public T Deserialize(byte[] message);
}

public class NetHandShake : IMessage<(long, int)>
{
    (long, int) data;
    public (long, int) Deserialize(byte[] message)
    {
        (long, int) outData;

        outData.Item1 = BitConverter.ToInt64(message, 4);
        outData.Item2 = BitConverter.ToInt32(message, 12);

        return outData;
    }

    public MessageType GetMessageType()
    {
        return MessageType.HandShake;
    }

    public byte[] Serialize()
    {
        List<byte> outData = new List<byte>();

        outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));

        outData.AddRange(BitConverter.GetBytes(data.Item1));
        outData.AddRange(BitConverter.GetBytes(data.Item2));


        return outData.ToArray();
    }
}

public class NetVector3 : IMessage<UnityEngine.Vector3>
{
    private static ulong lastMsgID = 0;
    private Vector3 data;

    public NetVector3(Vector3 data)
    {
        this.data = data;
    }

    public Vector3 Deserialize(byte[] message)
    {
        Vector3 outData;

        outData.x = BitConverter.ToSingle(message, 8);
        outData.y = BitConverter.ToSingle(message, 12);
        outData.z = BitConverter.ToSingle(message, 16);

        return outData;
    }

    public MessageType GetMessageType()
    {
        return MessageType.Position;
    }

    public byte[] Serialize()
    {
        List<byte> outData = new List<byte>();

        outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));
        outData.AddRange(BitConverter.GetBytes(lastMsgID++));
        outData.AddRange(BitConverter.GetBytes(data.x));
        outData.AddRange(BitConverter.GetBytes(data.y));
        outData.AddRange(BitConverter.GetBytes(data.z));

        return outData.ToArray();
    }

    //Dictionary<Client,Dictionary<msgType,int>>
}
public class NetConsole : IMessage<string>
{
    private static ulong lastMsgID = 0;
    private string data;

    public NetConsole()
    {
    }

    public NetConsole(string data)
    {
        this.data = data;
    }


    public string Deserialize(byte[] message)
    {
        string outData = "";
        int messageLenght = BitConverter.ToInt32(message, 8);
        Debug.Log(messageLenght);
        for (int i = 0; i < messageLenght; i++)
        {
            outData += BitConverter.ToChar(message, 12 + i);
        }

        return outData;

    }


    public MessageType GetMessageType()
    {
        return MessageType.String;
    }

    public byte[] Serialize()
    {
        List<byte> outData = new List<byte>();
        outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));
        outData.AddRange(BitConverter.GetBytes(lastMsgID++));
        outData.AddRange(BitConverter.GetBytes(data.Length));
        for (int i = 0; i < data.Length; i++)
        {
            outData.Add((byte)data[i]);
        }

        return outData.ToArray();
    }
}
public class NetByteTranslator
{

    public static MessageType getNetworkType(byte[] data)
    {
        (long, int) dataOut;
        dataOut.Item1 = BitConverter.ToInt32(data, 0);
        return (MessageType)dataOut.Item1;
    }
}
