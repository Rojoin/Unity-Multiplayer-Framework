using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// Class for the PlayerShooting
/// </summary>

public class PlayerShooting : MonoBehaviour
{
    [Header("Channels")]

    //[SerializeField] private BoolChannelSO OnFireChannel;

    [Header("Configurations")]
    
    [SerializeField] private Transform rayPosition;
    [SerializeField] private Transform[] shootingPoints;
    [SerializeField] private Transform cannon;

    [Header("UnityEvents")]
    public UnityEvent OnBulletShoot;
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
    private float minShootTimer = 0.05f;
    private float currentSingleShootTimer;


    private void Awake()
    {
        shootingPoints = cannon.transform.Cast<Transform>().ToArray();
    }

    private void OnEnable()
    {
        //Onfire.AddListernet()
       // OnFireChannel.Subscribe(OnFire);
        OnFire(false);
    }

    private void OnDisable()
    {
      //  OnFireChannel.Unsubscribe(OnFire);
        OnFire(false);
    }

    private void Start()
    {
        //specialBeanCooldown = player.specialBeanCooldown;
        //minShootTimer = player.minShootTimer;
        //minHoldShootTimer = player.minHoldShootTimer;
    }

    private void Update()
    {
       // if (LevelController.levelStatus != LevelController.LevelState.playing) return;
        AttackLogic();
    }
    /// <summary>
    /// Player AttackLogic
    /// </summary>
    private void AttackLogic()
    {
        SpecialBeanCooldownTimers();
        currentHoldShootTimer += Time.deltaTime;
        currentSingleShootTimer += Time.deltaTime;
        if (!isPressingButton)
        {
            CheckIfCanFireLaser();
            ResetTimers();
        }
        else
        {
            if (!isChargingSpecialBeam)
            {
                currentBeanTimer += Time.deltaTime;
                if (currentSingleShootTimer > minShootTimer && !singleBulletShoot)
                {
                    singleBulletShoot = true;
                    ShootBullet();
                }
                else if (currentHoldShootTimer > minHoldShootTimer && singleBulletShoot && currentSingleShootTimer > minHoldShootTimer)
                {
                    ShootBullet();
                    currentHoldShootTimer -= minHoldShootTimer;
                }
            }

            if (currentBeanTimer > specialBeamTimer && canFireSpecialBeam)
            {
               
                if (!isChargingSpecialBeam)
                  OnPrepareLaser.Invoke();

                isChargingSpecialBeam = true;
            }
        }
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
    /// <summary>
    /// Checks if player can fireLaser
    /// If true ShootsLaser
    /// </summary>
    private void CheckIfCanFireLaser()
    {
        if (!(currentBeanTimer > specialBeamTimer) || !canFireSpecialBeam) return;
        ShootRay();
        canFireSpecialBeam = false;
        SpecialBeanCooldownTimer = 0.0f;
    }
    /// <summary>
    /// Logic of the timers for the SpecialBean
    /// </summary>
    private void SpecialBeanCooldownTimers()
    {
        if (!canFireSpecialBeam) SpecialBeanCooldownTimer += Time.deltaTime;
        if (!(SpecialBeanCooldownTimer > specialBeanCooldown)) return;
        canFireSpecialBeam = true;
    }
    public void OnFire(bool value) => isPressingButton = value;
    /// <summary>
    /// Logic to Instantiate bullets
    /// The method shoots one bullet for each ShootingPoint
    /// </summary>
    private void ShootBullet()
    {
        OnBulletShoot.Invoke();
        foreach (Transform shootingPos in shootingPoints)
        {
           /// askForBulletChannel.RaiseEvent(shootingPos, LayerMask.LayerToName(gameObject.layer), bulletConfiguration, transform.rotation);
        }
    }
    /// <summary>
    /// Instantiate the Shoot Ray Logic
    /// </summary>
    public void ShootRay()
    {
        OnLaserShoot.Invoke();
     //   askForLaserChannel.RaiseEvent(rayPosition,LayerMask.LayerToName(gameObject.layer),laserConfiguration,transform,transform.rotation);
    }
  
    /// <summary>
    /// Gets the SpecialBeanCooldown max Cooldown
    /// </summary>
    /// <returns></returns>
    public float GetCurrentFillValue()
    {
        return SpecialBeanCooldownTimer;
    }
    /// <summary>
    /// Gets the SpecialBean current cooldown
    /// </summary>
    /// <returns></returns>
    public float GetMaxFillValue()
    {
        return specialBeanCooldown;
    }
}

