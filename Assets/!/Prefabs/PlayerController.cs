using System;
using UnityEngine;
//using static Unity.Cinemachine.InputAxisControllerBase<T>;
//using static Unity.Cinemachine.InputAxisControllerBase<T>;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private Transform _groundCheck;
    [SerializeField] public Camera cam;

    private float _groundCheckRadius = 0.3f;
    public float _speed = 8;
    public float _jumpForce = 500f;

    private Rigidbody rb;
    private Vector3 overallDirection;
    Vector3 velocity;
    Vector2 Dir;

    // from player inputs
    [SerializeField] public bool canJump = false;
    [SerializeField] public bool sprint = false;
    private Vector2 moveDir;
    private Vector2 lookDir;

    Vector2 dir;
    public float _turnSpeed = 1500f;

    PlayerInputActions input;

    [SerializeField]
    InputActionReference move, look, spring;
    //kinematic base class containing collide and slide stuff
    [SerializeField]
    KinematicCharacterController controller;

    private GravityBody _gravityBody;

    private void Awake()
    {
        controller = GetComponent<KinematicCharacterController>();
    }

    void Start()
    {
        rb = transform.GetComponent<Rigidbody>();
        _gravityBody = transform.GetComponent<GravityBody>();

        //cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        moveDir = move.action.ReadValue<Vector2>();
        overallDirection = new Vector3(moveDir.x, 0f, moveDir.y).normalized;
        //need to use new input system with GetAxisRaw, probably a turnary?
        //overallDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Vector3 test = new Vector3(transform.position.x, transform.position.y - 1, transform.position.z);
        Gizmos.DrawSphere(test, _groundCheckRadius);
    }

    void FixedUpdate()
    {
        //omnidirectional gravity
        Vector3 direction;
        bool isMoving = overallDirection.magnitude > 0.1f;
 
/*        direction = transform.forward * overallDirection.z;
        rb.MovePosition(rb.position + direction * (_speed * Time.fixedDeltaTime));

        Quaternion rightDirection = Quaternion.Euler(0f, overallDirection.x * (_turnSpeed * Time.fixedDeltaTime), 0f);
        Quaternion newRotation = Quaternion.Slerp(rb.rotation, rb.rotation * rightDirection, Time.fixedDeltaTime * 3f); ;
        rb.MoveRotation(newRotation);*/

        //collide and slide

        direction = (cam.transform.forward * moveDir.y + cam.transform.right * moveDir.x);
        direction.y = 0;
        direction.Normalize();
        dir = new Vector2(direction.x, direction.z);
        // dir = new Vector2(0, -1).normalized;
        velocity = controller.Move(dir, canJump);


    }

    //controls
    #region enableDisable
    void OnEnable()
    {
        spring.action.performed += JumpInput;

    }


    void OnDisable()
    {
        spring.action.performed -= JumpInput;


    }
    #endregion enableDisable

    private void JumpInput(InputAction.CallbackContext context)
    {
        canJump = true;
    }
}
