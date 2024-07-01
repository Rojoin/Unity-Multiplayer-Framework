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
        TRS GetTRS();
        void SetTRS(TRS trs,TRSFlags flags);
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
        public (float x, float y, float z) position;
        public (float x, float y, float z, float w) rotation;
        public (float x, float y, float z) scale;
        public bool isActive;
    }

    public class NetObjectBasicData
    {
        public int objectID;
        public List<Route> idValues;

        public NetObjectBasicData(int objId, List<Route> idValues)
        {
            objectID = objId;
            this.idValues = idValues;
        }
    }
}