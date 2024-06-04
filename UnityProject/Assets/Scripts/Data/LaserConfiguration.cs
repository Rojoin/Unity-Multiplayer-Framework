using UnityEngine;
[CreateAssetMenu(fileName = "LaserConfig")]
public class LaserConfiguration : ScriptableObject
{
    public float lifeTime;
    public float damage;
    public float distance;
}