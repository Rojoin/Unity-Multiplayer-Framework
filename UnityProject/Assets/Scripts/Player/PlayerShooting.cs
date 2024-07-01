using System.Linq;
using RojoinNetworkSystem;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Class for the PlayerShooting
/// </summary>
public class PlayerShooting : MonoBehaviour, INetObject
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
    [NetValue(1)] public bool isPressingButton;
    private bool singleBulletShoot;
    [Header("Cooldowns Presets")]
    public float specialBeanCooldown;
    private float _specialBeanCooldownTimer = 0.0f;
    private float currentBeanTimer;
    private NetObject _netObject = new NetObject();
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


    public TRS GetTRS()
    {
        return transform.GetTRS();
    }

    public void SetTRS(TRS trs, TRSFlags flags)
    {
        Debug.Log(flags.ToString());
        transform.SetTRS(trs, flags);
    }

    public void SendDeleteMessage()
    {
        NetObjectFactory.Instance.SendDeleteMessage(GetObject());
    }

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
        SendDeleteMessage();
    }

    private void Fire()
    {
        if (currentSingleShootTimer > minShootTimer)
        {
            NetObjectFactory.Instance.NetworkSystem.CallAsRPC(this, nameof(ShootBullet));
        }
    }

    private void Start()
    {
        NetObjectFactory.Instance.NetworkSystem.CallAsRPC(this, nameof(TestParameters), 5);
        NetObjectFactory.Instance.NetworkSystem.CallAsRPC(this, nameof(TestParameters2), true);
        NetObjectFactory.Instance.NetworkSystem.CallAsRPC(this, nameof(TestParameters3), 5);
    }

    [NetRPC(2)] private void TestParameters(float number)
    {
        Debug.Log($"The number is : {number}");
    }  
    [NetRPC(3)] private void TestParameters2(bool value)
    {
        Debug.Log($"The value is : {value}");
    } 
    [NetRPC(4)] private void TestParameters3(int value)
    {
        Debug.Log($"The int value is : {value}");
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


    [NetRPC(1)] private void ShootBullet()
    {
        Debug.Log("Shoot");
        foreach (Transform point in shootingPoints)
        {
            OnBulletShoot.Invoke(point);
        }

        ResetTimers();
    }

    public int GetID()
    {
        return _netObject.id;
    }

    public int GetOwner()
    {
        return _netObject.owner;
    }

    public NetObject GetObject()
    {
        return _netObject;
    }
}