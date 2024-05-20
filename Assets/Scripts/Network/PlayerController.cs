using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public int id;

    [SerializeField] public UnityEvent<Vector3> OnMovement = new();
    [SerializeField] public UnityEvent<int> OnHit;

    [SerializeField] private float speed = 20;
    [SerializeField] private Text playerNameText;
    [SerializeField] public string nameTagPlayer = "";
    [SerializeField] private float height = 1;
    [SerializeField] private float radius = 1;
    [SerializeField] private int maxHealth = 6;
    [SerializeField] public int currentHealth;
    private Coroutine movement;
    private BoxCollider box;
    private CharacterController characterController;
    private bool isAlive = true;

    private void OnEnable()
    {
        box = GetComponent<BoxCollider>();
        characterController = GetComponent<CharacterController>();
        currentHealth = maxHealth;
        isAlive = true;
        playerNameText.text = nameTagPlayer;
    }

    private void OnDisable()
    {
        if (movement != null)
        {
            StopCoroutine(movement);
        }

        OnMovement.RemoveAllListeners();
        OnHit.RemoveAllListeners();
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


    private void Rotation(Vector3 moveDir)
    {
        transform.forward = moveDir;
    }

    public void SetPosition(Vector3 moveDir)
    {
        transform.forward = (moveDir - transform.position);
        transform.position = moveDir;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet") && other.GetComponent<Bullet>().ID != id && isAlive)
        {
            other.gameObject.SetActive(false);
            //  Debug.Log("I was hitted");
            currentHealth--;
            if (currentHealth <= 0)
            {
                OnHit.Invoke(id);
                isAlive = false;
            }
        }
    }
}