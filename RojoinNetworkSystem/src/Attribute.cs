using System;

namespace RojoinNetworkSystem
{
    public class NetValue : Attribute
    {
        public MessageFlags messageFlags;
        public int id;

        public NetValue(int id, MessageFlags flags = MessageFlags.CheckSum)
        {
            this.id = id;
            messageFlags = flags;
        }
    }

    public class NetNotSync : Attribute
    {

    }
}