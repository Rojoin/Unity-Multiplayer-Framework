using System.Collections.Generic;

namespace RojoinNetworkSystem
{
    public static class NetObjectServerFactory
    {
        public static int intanceCount = 0;

        public static int getNewIntancesNumber() => intanceCount++;

        public static byte[] CreateNetObjectFactoryMessage(byte[] dataToBroadcast)
        {
            NetGetObjectID messageReceived = new NetGetObjectID();
            AskForNetObject netObjectData = messageReceived.DeseliarizeObj(dataToBroadcast);
            netObjectData.intanceID = getNewIntancesNumber();
            NetGetObjectID newEntity = new NetGetObjectID(netObjectData);
            var data = newEntity.Serialize();
            return data;
        }
    }
}