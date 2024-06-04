using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Class for the PlayerMovement
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Channels")]
    [SerializeField] private VoidChannelSO OnRollChannel;
    [SerializeField] private BoolChannelSO OnFocusChannel;
    [SerializeField] private Vector2ChannelSO OnMoveChannel;
    [Header("UnityEvents")]
    public UnityEvent OnBarrelRoll;
    [Header("GameObjects")]
    //[SerializeField] private PlayerSettings player;
    [SerializeField] private Transform playerModel;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform aimTarget;


    [Header("Values")]
    public float rollTime;
    [SerializeField] public float xySpeed;
    [SerializeField] private float lookSpeed;
    [SerializeField] private float leanLimit;
    [SerializeField] private bool isFocusActivate = false;
    private bool canRoll = false;
    private Vector2 movevementValue;

    [Header("ClampValues")]
    private Vector2 minPositionBeforeClamp;
    private Vector2 maxPositionBeforeClamp;
    private static readonly int IsRolling = Animator.StringToHash("IsRolling");
    private static readonly int MovementX = Animator.StringToHash("MovementX");

    private void OnEnable()
    {
        OnFocusChannel.Subscribe(OnFocusMode);
        OnRollChannel.Subscribe(OnRoll);
        OnMoveChannel.Subscribe(OnMove);
    }

    private void OnDisable()
    {
        OnFocusChannel.Unsubscribe(OnFocusMode);
        OnRollChannel.Unsubscribe(OnRoll);
        OnMoveChannel.Unsubscribe(OnMove);
    }
    
    private void Start()
    {

        canRoll = true;
        isFocusActivate = false;
       // xySpeed = player.xySpeed;
       // lookSpeed = player.lookSpeed;
       // maxPositionBeforeClamp = player.maxPositionBeforeClamp;
       // minPositionBeforeClamp = player.minPositionBeforeClamp;
    }

    private void Update()
    {
        Movement();
    }

    /// <summary>
    /// Logic to make the playerMove
    /// If is Focus is Activated the player will not move but rotate
    /// </summary>
    private void Movement()
    {
        if (!isFocusActivate)
        {
            LocalMove(movevementValue.x, movevementValue.y);
        }

        RotationLook(movevementValue.x, movevementValue.y, lookSpeed);
        HorizontalLean(playerModel, -movevementValue.x, leanLimit, .1f);
        ClampPosition();
    }

    /// <summary>
    /// Changes movevementValue to Input
    /// </summary>
    /// <param name="value">Input</param>
    public void OnMove(Vector2 value) => movevementValue = value;

    /// <summary>
    /// Logic for the RollMovement
    /// Actiavtes the animation
    /// </summary>
    public void OnRoll()
    {
        if (!gameObject.activeSelf)
            return;
        StartCoroutine(OnRolling());
    }

    public IEnumerator OnRolling()
    {
        if (!canRoll)
        {
            yield break;
        }

        canRoll = false;
        animator.SetFloat(MovementX, movevementValue.x);
        animator.SetTrigger(IsRolling);
        OnBarrelRoll.Invoke();
        yield return new WaitForSeconds(rollTime);
        canRoll = true;
    }

    /// <summary>
    /// Activates focusMode
    /// </summary>
    public void OnFocusMode(bool value) => isFocusActivate = value;

    /// <summary>
    /// Do Local Move according to the parameters
    /// Can be modifidied with xySpeed and depends on deltaTime
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    private void LocalMove(float x, float y)
    {
        transform.localPosition += new Vector3(x, y, 0) * (xySpeed * Time.deltaTime);
    }

    /// <summary>
    /// Clamps the position of the plauer to not go offlimits
    /// </summary>
    private void ClampPosition()
    {
        var pos = transform.localPosition;
        pos.y = Mathf.Clamp(pos.y, -minPositionBeforeClamp.y, maxPositionBeforeClamp.y);
        pos.x = Mathf.Clamp(pos.x, -minPositionBeforeClamp.x, maxPositionBeforeClamp.x);
        transform.localPosition = new Vector3(pos.x, pos.y, pos.z);
    }

    /// <summary>
    /// Changes the rotation of the model
    /// </summary>
    /// <param name="horizontal">Horizontal Value</param>
    /// <param name="vertical">Vertical Value</param>
    /// <param name="speed">Speed of the rotation</param>
    private void RotationLook(float horizontal, float vertical, float speed)
    {
        aimTarget.parent.position = Vector3.zero;
        aimTarget.localPosition = new Vector3(horizontal, vertical, 1);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(aimTarget.position),
            Mathf.Deg2Rad * speed);
    }

    /// <summary>
    /// Leans the player horizontally
    /// </summary>
    /// <param name="target">Target to lean</param>
    /// <param name="axis">Axis to lean</param>
    /// <param name="leanLimit">Limit of the lean</param>
    /// <param name="lerpTime">Time until lean is complete</param>
    private void HorizontalLean(Transform target, float axis, float leanLimit, float lerpTime)
    {
        Vector3 targetEulerAngels = target.localEulerAngles;
        target.localEulerAngles = new Vector3(targetEulerAngels.x, targetEulerAngels.y,
            Mathf.LerpAngle(targetEulerAngels.z, -axis * leanLimit, lerpTime));
    }
}