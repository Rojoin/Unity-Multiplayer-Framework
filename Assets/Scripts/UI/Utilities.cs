using System;
using System.Collections.Generic;
using UnityEngine;

public static class Utilities
{
    public static void SetCanvasActive(this CanvasGroup canvas, bool state = true)
    {
        canvas.alpha = state ? 1.0f : 0.0f;
        canvas.blocksRaycasts = state;
        canvas.interactable = state;
    }
}

public enum BitOperations
{
    sum,
    substract,
    moveLeft,
    moveRight
}

public static class NetByteTranslator
{
    public static MessageType GetNetworkType(byte[] data)
    {
        int dataOut = BitConverter.ToInt32(data, 0);
        return (MessageType)dataOut;
    }

    public static int GetPlayerID(byte[] data)
    {
        int dataOut = BitConverter.ToInt32(data, 4);
        return dataOut;
    }

    public static MessageFlags GetFlags(byte[] data)
    {
        MessageFlags dataOut = (MessageFlags)BitConverter.ToInt32(data, 8);
        return dataOut;
    }

    public static ulong GetMesaggeID(byte[] data)
    {
        ulong dataOut = BitConverter.ToUInt64(data, 12);
        return dataOut;
    }

    public static ulong GetObjectID(byte[] data)
    {
        ulong dataOut = BitConverter.ToUInt64(data, 20);
        return dataOut;
    }

    public static uint EncryptBitSizeOperations(List<byte> outData, BitOperations[] operationsToDo)
    {
        uint checkSum = 0;
        for (int i = 0; i < outData.Count; i++)
        {
            byte singleByte = outData[i];
            checkSum = SelectOperations(operationsToDo, checkSum, singleByte);
        }

        return checkSum;
    }

    public static uint DecryptBitSizeOperations(List<byte> outData, BitOperations[] operationsToDo)
    {
        uint checkSum = 0;
        for (int i = 0; i < outData.Count - 8; i++)
        {
            byte singleByte = outData[i];
            checkSum = SelectOperations(operationsToDo, checkSum, singleByte);
        }

        return checkSum;
    }

    private static uint SelectOperations(BitOperations[] operationsToDo, uint chekSum, byte currentByte)
    {
        int index = currentByte % operationsToDo.Length;
        chekSum = operationsToDo[index] switch
        {
            BitOperations.sum => BitSum(chekSum, currentByte),
            BitOperations.substract => BitSus(chekSum, currentByte),
            BitOperations.moveLeft => BitLeft(chekSum, currentByte),
            BitOperations.moveRight => BitRight(chekSum, currentByte),
            _ => chekSum
        };

        return chekSum;
    }

    private static uint BitSum(uint chekSum, byte currentByte)
    {
        return chekSum + currentByte;
    }

    private static uint BitSus(uint chekSum, byte currentByte)
    {
        return chekSum - currentByte;
    }

    private static uint BitLeft(uint chekSum, byte currentByte)
    {
        return chekSum <<= 3;
    }

    private static uint BitRight(uint chekSum, byte currentByte)
    {
        return chekSum >>= 3;
    }
}