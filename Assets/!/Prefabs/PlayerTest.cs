using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody))]  
public class PlayerTest : MonoBehaviour
{
    public Rigidbody rb;
    [SerializeField]
    public float moveSpeed = 15f;

    Vector3 moveDirection = Vector3.zero;

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        moveDirection = new Vector3 (moveX, 0, moveZ).normalized;
    }

    private void FixedUpdate()
    {
        rb.velocity = new Vector3(-moveDirection.x * moveSpeed, 0, -moveDirection.z * moveSpeed).normalized;
    }
}
