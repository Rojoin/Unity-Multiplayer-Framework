using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class PlayerController : MonoBehaviour
{
    public int id;

    [SerializeField] public UnityEvent<Vector3> OnMovement = new();
    [SerializeField] private float speed = 20;
    [SerializeField] private float height = 1;
    [SerializeField] private float radius = 1;
    private Coroutine movement;
    private BoxCollider box;
    private CharacterController characterController;

    private void OnEnable()
    {
        box = GetComponent<BoxCollider>();
        characterController = GetComponent<CharacterController>();
    }

    private void OnDisable()
    {
        if (movement != null)
        {
            StopCoroutine(movement);
        }

        OnMovement.RemoveAllListeners();
    }

    public void Move(Vector2 dir)
    {
        if (movement != null)
        {
            StopCoroutine(movement);
        }

        movement = StartCoroutine(Movement(dir));
    }

    /// <summary>
    /// Movement Corroutine
    /// </summary>
    /// <param name="dir"></param>
    /// <returns></returns>
    private IEnumerator Movement(Vector2 dir)
    {
        while (dir != Vector2.zero)
        {
            Vector3 moveDir = new Vector3(dir.x, 0, dir.y);
            float time = Time.deltaTime;
            
            Rotation(moveDir);
            characterController.Move(moveDir * (time * speed));
            // transform.position += moveDir * (time * speed);
            OnMovement.Invoke(transform.position);


            yield return null;
        }
    }

    private bool CanMove(Vector2 dir, float time, ref Vector3 moveDir)
    {
        moveDir = new Vector3(dir.x, 0, dir.y);
        bool canMove = !IsColliding(moveDir);

        if (!canMove)
        {
            Vector3 moveDirX = new Vector3(dir.x, 0, 0);
            canMove = !IsColliding(moveDirX);
            if (canMove)
            {
                moveDir = moveDirX;
            }
            else
            {
                Vector3 moveDirY = new Vector3(0, 0, dir.y);
                canMove = !IsColliding(moveDirY);
                if (canMove)
                {
                    moveDir = moveDirY;
                }
            }
        }

        return canMove;
    }

//Todo:Change To rigidbody or raycast
    private bool IsColliding(Vector3 moveDir)
    {
        var position = transform.position;

        return Physics.BoxCast(box.bounds.center, box.size,
            moveDir);
    }

    private void Rotation(Vector3 moveDir)
    {
        transform.forward = moveDir;
    }

    public void SetPosition(Vector3 moveDir)
    {
        transform.forward = (moveDir - transform.position);
        transform.position = moveDir;
    }
}