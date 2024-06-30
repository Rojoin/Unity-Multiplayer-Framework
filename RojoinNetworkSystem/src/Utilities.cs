using System;
using System.Collections.Generic;

namespace RojoinNetworkSystem
{
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
            if (data != null && data.Length > 0)
            {
                int dataOut = BitConverter.ToInt32(data, 0);
                return (MessageType)dataOut;
            }

            return MessageType.Error;
        }

        internal static NetObjectBasicData GetNetObjectData(byte[] data)
        {
            int objID = BitConverter.ToInt32(data, 20);
            int listOffset = 24;
            int intIndex = BitConverter.ToInt32(data, listOffset);

            List<Route> idValues = new List<Route>();
            for (int i = 0; i < intIndex; i++)
            {
                listOffset += 4;
                int id = BitConverter.ToInt32(data, listOffset);
                listOffset += 4;
                int colPos = (BitConverter.ToInt32(data, listOffset));
                listOffset += 4;
                int colSize = (BitConverter.ToInt32(data, listOffset));
                idValues.Add(new Route(id, colPos, colSize));
            }

            return new NetObjectBasicData(objID, idValues);
        }

        public static int GetPlayerID(byte[] data)
        {
            if (data != null && data.Length > 4)
            {
                int dataOut = BitConverter.ToInt32(data, 4);
                return dataOut;
            }

            return -15;
        }

        public static MessageFlags GetFlags(byte[] data)
        {
            if (data != null && data.Length > 8)
            {
                MessageFlags dataOut = (MessageFlags)BitConverter.ToInt32(data, 8);
                return dataOut;
            }

            return MessageFlags.None;
        }

        public static ulong GetMesaggeID(byte[] data)
        {
            if (data != null && data.Length > 12)
            {
                ulong dataOut = BitConverter.ToUInt64(data, 12);
                return dataOut;
            }

            return 0;
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

        private static uint SelectOperations(BitOperations[] operationsToDo, uint checkSum, byte currentByte)
        {
            int index = currentByte % operationsToDo.Length;
            switch (operationsToDo[index])
            {
                case BitOperations.sum:
                    checkSum = BitSum(checkSum, currentByte);
                    break;
                case BitOperations.substract:
                    checkSum = BitSus(checkSum, currentByte);
                    break;
                case BitOperations.moveLeft:
                    checkSum = BitLeft(checkSum, currentByte);
                    break;
                case BitOperations.moveRight:
                    checkSum = BitRight(checkSum, currentByte);
                    break;
                default:
                    checkSum = checkSum;
                    break;
            }

            return checkSum;
        }

        private static uint BitSum(uint checkSum, byte currentByte)
        {
            return checkSum + currentByte;
        }

        private static uint BitSus(uint checkSum, byte currentByte)
        {
            return checkSum - currentByte;
        }

        private static uint BitLeft(uint checkSum, byte currentByte)
        {
            return checkSum <<= 3;
        }

        private static uint BitRight(uint checkSum, byte currentByte)
        {
            return checkSum >>= 3;
        }
    }

    public static class Utilities
    {
        public static int Sorter(MessageCache cache1, MessageCache cache2)
        {
            return cache1.messageId > cache2.messageId ? (int)cache1.messageId : (int)cache2.messageId;
        }
    }
}