using UnityEngine;

public static class NetByteTranslatorOverride
{
    public static Vector3 ToUnityVector3(this System.Numerics.Vector3 vector3)
    {
        return new Vector3(vector3.X,vector3.Y,vector3.Z);
    } 
    public static System.Numerics.Vector3 ToSystemVector3(this Vector3 vector3)
    {
        return new System.Numerics.Vector3(vector3.x,vector3.y,vector3.z);
    }
}