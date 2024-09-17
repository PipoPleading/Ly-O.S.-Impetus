using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody))]
public class PlayerTest : MonoBehaviour
{
    //required components
    public Rigidbody rb;

    //player stats
    [SerializeField]
    public float moveSpeed = 15f;
    [SerializeField]
    public float jumpForce = 10f;

    bool canJump = true;
    bool slowDown = false;

    //player controls
    public PlayerInputActions playerControls;
    private InputAction jump;
    private InputAction movement;
    private InputAction fire;
    Vector3 moveDirection = Vector3.zero;

    //gravity components
    public float rayCastLength;
    public float rotationSpeed;
    private float tempRotationSpeed;
    public float gravity;
    private float tempGravity;

    public Transform currentPlanet;
    //may need in the future post model creation, may need to rework hierarchy *shrug*
    //public Transform playerVisual;

    RaycastHit[] hits;
    Vector3 gravityDir;
    Vector3 normalVector;
    Vector3 input;

    public bool isTouchingPlanetSurface = false;
    private Transform playerCamera;
    private Transform playerCameraArm;

    //animator & assets go here




    private void Awake()
    {
        //controls
        playerControls = new PlayerInputActions();
        playerControls.Player.Jump.performed += Jump;

        //initializing gravity
    }
    private void OnEnable()
    {
        movement = playerControls.Player.Move;

        movement.Enable();
    }
    void Jump(InputAction.CallbackContext context)
    {
        if (!canJump) return;

        rb.velocity *= 0;
        //need to modify later for dynamic jump
        rb.AddForce(normalVector * jumpForce, ForceMode.Impulse);
        //might mess with?
        gravity = tempGravity / 2f;
        Invoke(nameof(RestoreGravity), 1f);
        canJump = false;
        rotationSpeed = tempRotationSpeed / 2f;

    }

    //child function of jump
    void RestoreGravity()
    {
        gravity = tempGravity;
        canJump = true;
        slowDown = false;
    }

    public void EnterNewGravityField()
    {
        gravity = tempGravity / 4f;
        GravitySlowDown(slowDown);
        rotationSpeed = tempRotationSpeed / 10f;
        slowDown = true;
        canJump = false;
        Invoke(nameof(RestoreGravity), 0.5f);
    }

    void Movement()
    {
        moveDirection = new Vector3(movement.ReadValue<Vector2>().x, 0, movement.ReadValue<Vector2>().y);

        Vector3 cameraRotation = new Vector3(0, playerCamera.localEulerAngles.y + playerCameraArm.localEulerAngles.y, 0);
        Vector3 direction = Quaternion.Euler(cameraRotation) * input;
        Vector3 movement_dir = (transform.forward * direction.x + transform.right * direction.x);
        Vector3 currentNormalVel = Vector3.Project(rb.velocity, normalVector.normalized);
        rb.velocity = currentNormalVel + (movement_dir * moveSpeed);

        if (movement_dir != Vector3.zero)
        {
            //animation stuff

        }
        else
        {
            //animation stuff
        }

        GravitySlowDown(slowDown);

    }

    void GravitySlowDown(bool slowDown)
    {
        if (slowDown) rb.velocity *= 0.5f;
    }

    void ApplyGravity()
    {
        if (currentPlanet == null) return;

        hits = Physics.RaycastAll(transform.position, -transform.up, rayCastLength);

        if(hits.Length == 0)
        {
            hits = Physics.RaycastAll(transform.position, transform.forward, rayCastLength);
        }

        if(hits.Length == 0)
        {
            hits = Physics.RaycastAll(transform.position, -transform.forward, rayCastLength);
        }

        if(hits.Length == 0)
        {
            hits = Physics.RaycastAll(transform.position, transform.right, rayCastLength);
        }
        
        if(hits.Length == 0)
        {
            hits = Physics.RaycastAll(transform.position, -transform.right, rayCastLength);
        }

        if (hits.Length == 0)
        {
            gravityDir = currentPlanet.position - transform.position;
            hits = Physics.RaycastAll(transform.position, gravityDir, rayCastLength);
        }

        GetGravityNormal();
        rb.AddForce(normalVector.normalized * gravity, ForceMode.Acceleration);
        hits = new RaycastHit[0];
    }


    void GetGravityNormal()
    {
        if(currentPlanet == null) return;
        normalVector = (transform.position - currentPlanet.position).normalized;
        for(int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform == currentPlanet)
            {
                normalVector = hits[i].normal;
                break;
            }
        }

        return;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.transform == currentPlanet) isTouchingPlanetSurface = true;
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.transform == currentPlanet) isTouchingPlanetSurface = false;
    }

    void ApplyPlanetRotation()
    {
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, normalVector) * transform.rotation;
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        if (isTouchingPlanetSurface && canJump) rotationSpeed = tempRotationSpeed;
    }

    private void OnDisable()
    {
        movement.Disable();
    }

    void Update()
    {
        /* float moveX = Input.GetAxis("Horizontal");
         float moveZ = Input.GetAxis("Vertical");*/
        moveDirection = movement.ReadValue<Vector3>();
    }

   

    private void FixedUpdate()
    {
        Movement();
        ApplyGravity();
        ApplyPlanetRotation();

        //may move to seperate function
        rb.velocity = new Vector3(-moveDirection.x * moveSpeed, 0, -moveDirection.z * moveSpeed);
    }
}
