using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Class for the PlayerShooting
/// </summary>
public class PlayerShooting : MonoBehaviour
{
    [Header("Channels")]
    [SerializeField] private VoidChannelSO OnFireChannel;

    [Header("Configurations")]
    [SerializeField] private Transform rayPosition;
    [SerializeField] private Transform[] shootingPoints;
    [SerializeField] private Transform cannon;

    [Header("UnityEvents")]
    public UnityEvent<Transform> OnBulletShoot;
    public UnityEvent<int> OnNewScore;
    public UnityEvent OnLaserShoot;
    public UnityEvent OnPrepareLaser;

    [Header("Variables")]
    private bool isPressingButton;
    private bool singleBulletShoot;
    [Header("Cooldowns Presets")]
    public float specialBeanCooldown;
    private float _specialBeanCooldownTimer = 0.0f;
    private float currentBeanTimer;
    private bool canFireSpecialBeam;
    public float SpecialBeanCooldownTimer
    {
        get => _specialBeanCooldownTimer;
        set
        {
            _specialBeanCooldownTimer = value;
            //  fillUIChannel.RaiseEvent(this as IFillable);
        }
    }
    private bool isChargingSpecialBeam = false;
    private float specialBeamTimer = 1.2f;
    private float minHoldShootTimer = 0.2f;
    private float currentHoldShootTimer;
    private float minShootTimer = 0.5f;
    private float currentSingleShootTimer;


    private void Awake()
    {
        shootingPoints = cannon.transform.Cast<Transform>().ToArray();
    }

    private void OnEnable()
    {
        //Onfire.AddListernet()
        OnFireChannel.Subscribe(Fire);
    }

    private void OnDisable()
    {
        OnFireChannel.Unsubscribe(Fire);
        OnBulletShoot.RemoveAllListeners();
    }

    private void Fire()
    {
        if (currentSingleShootTimer > minShootTimer)
        {
            ShootBullet();
        }
    }

    private void Start()
    {
    }

    private void Update()
    {
        AttackLogic();
    }

    /// <summary>
    /// Player AttackLogic
    /// </summary>
    private void AttackLogic()
    {
        currentSingleShootTimer += Time.deltaTime;
    }

    /// <summary>
    /// Resets variables for shooting 
    /// </summary>
    private void ResetTimers()
    {
        singleBulletShoot = false;
        currentSingleShootTimer = 0.0f;
        currentBeanTimer = 0.0f;
        currentHoldShootTimer = minHoldShootTimer;
        isChargingSpecialBeam = false;
    }


    private void ShootBullet()
    {
        foreach (Transform point in shootingPoints)
        {
            OnBulletShoot.Invoke(point);
        }

        ResetTimers();
    }
}