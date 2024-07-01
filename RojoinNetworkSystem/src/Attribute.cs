using System;
using System.ComponentModel;

namespace RojoinNetworkSystem
{
    public class NetRPC : Attribute
    {
        public MessageFlags messageFlags;
        public int id;
        public NetRPC(int id, MessageFlags flags = MessageFlags.CheckSum)
        {
            this.id = id;
            messageFlags = flags;
        }
        void a()
        {
            //Fijarse de subscribirse al delegado de la funcion
            //Usar typeDecriptor
            //Usar typeDecriptor
            //Mandar lista de:(string type, string valor)
            //parameterType = type
            //parameterType = parameterAsString
            //Crear mensajes tipo Metodo
           //TypeConverter converter = TypeDescriptor.GetConverter(parameterType);
           //converter.ConvertFromInvariantString(parameterAsString);
        }
    }
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

    [Flags]
    public enum TRSFlags
    {
        Default = 0,
        NotPos = 1,
        NotRotation = 2,
        NotScale = 4,
        NotActive = 8
    }
    
    public class NetNotSync : Attribute
    {
        public TRSFlags flags;
        
        public NetNotSync(TRSFlags flagsToSet = TRSFlags.Default)
        {
            flags = flagsToSet;
        }
    }

    public class NetExtensionMethod : Attribute
    {
        public Type extensionType;

        public NetExtensionMethod(Type extensionType)
        {
            this.extensionType = extensionType;
        }
    }

    public class NetExtensionClass : Attribute
    {
    }
}