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

    //Todo: add a way to syncronyze TRS
    //Maybe check if object inheriths from Monobehaviour and take the transform 
    public class NetNotSync : Attribute
    {

    }
}