using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody))]  
public class PlayerTest : MonoBehaviour
{
    public Rigidbody rb;
    [SerializeField]
    public float moveSpeed = 15f;
    public PlayerInputActions playerControls;
    public InputAction playerJump;

    Vector3 moveDirection = Vector3.zero;

    private InputAction playerMove;
    private InputAction playerFire;


    private void Awake()
    {
        playerControls = new PlayerInputActions();
    }
    private void OnEnable()
    {
        playerMove = playerControls.Player.Move;

        playerMove.Enable();
    }

    private void OnDisable()
    {
        playerMove.Disable();
    }

    void Update()
    {
        /* float moveX = Input.GetAxis("Horizontal");
         float moveZ = Input.GetAxis("Vertical");*/

        moveDirection = new Vector3(playerMove.ReadValue<Vector2>().x, 0, playerMove.ReadValue<Vector2>().y) ;
    }

    private void FixedUpdate()
    {
        rb.velocity = new Vector3(-moveDirection.x * moveSpeed, 0, -moveDirection.z * moveSpeed);
    }
}
