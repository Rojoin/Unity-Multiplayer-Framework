using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New PlayerSettings",menuName = "New Player")]
public class PlayerSettings : ScriptableObject
{
    [Header("Shooting Presets")]
    public float specialBeanCooldown;
    public float minHoldShootTimer = 0.2f;
    public float minShootTimer = 0.05f;
    [Header("Movement Presets")]
    public float xySpeed;
    public float lookSpeed;
    public float cartSpeed;
    [SerializeField]
    float leanLimit;
    [Header("ClampValues")]
    public Vector2 minPositionBeforeClamp = new Vector2(8, 3.5f);
    public Vector2 maxPositionBeforeClamp = new Vector2(8, 8);
}
