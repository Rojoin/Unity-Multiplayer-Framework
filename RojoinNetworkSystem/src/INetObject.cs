using System;
using System.Collections.Generic;
using System.Numerics;

namespace RojoinNetworkSystem
{
    public interface INetObject
    {
        int GetID();
        int GetOwner();
        NetObject GetObject();
    }

    public class NetObject
    {
        public int id;
        public int owner;
    }

    public class AskForNetObject
    {
        public int objectType;
        public int intanceID;
        public int owner;
        public Vector3 pos;
        public Vector3 rot;
        public Vector3 scale;
        public int parentId;
    }

    public class TRS
    {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
    }

    public class NetObjectBasicData
    {
       public int objectID;
        public List<int> idValues;
       public NetObjectBasicData(int objId, List<int> idValues)
       {
           objectID = objId;
           this.idValues = idValues;
       }
    }

    public class ClassA : INetObject
    {
        [NetValue(1, MessageFlags.Important)] private string name;
        [NetValue(2)] private ClassB b;
        private NetObject net;

        public int GetID()
        {
            return net.id;
        }

        public int GetOwner()
        {
            return net.owner;
        }

        public NetObject GetObject()
        {
            return net;
        }
    }

    public class ClassB : INetObject
    {
        [NetValue(1)] private float aa;
        [NetValue(2)] private float bb;
        [NetValue(3)] private float cc;

        public int GetID()
        {
            throw new System.NotImplementedException();
        }

        public int GetOwner()
        {
            throw new System.NotImplementedException();
        }

        public NetObject GetObject()
        {
            throw new System.NotImplementedException();
        }
    }
}