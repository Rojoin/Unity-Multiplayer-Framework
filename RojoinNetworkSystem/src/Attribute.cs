using System;

namespace RojoinNetworkSystem
{
    public class Attribute : System.Attribute
    {
        public MessageFlags messageFlags;
        public int id;

        public Attribute(int id, MessageFlags flags = MessageFlags.None)
        {
            this.id = id;
            messageFlags = flags;
        }
    }

    public class NetNotSync : System.Attribute
    {
    }
}