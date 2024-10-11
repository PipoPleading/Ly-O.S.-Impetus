using UnityEngine;
//using static Unity.Cinemachine.InputAxisControllerBase<T>;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private Transform _groundCheck;
    [SerializeField] public Transform _cam;

    private float _groundCheckRadius = 0.3f;
    public float _speed = 8;
    public float _jumpForce = 500f;

    private Rigidbody _rigidbody;
    private Vector3 overallDirection;

    // from player inputs
    [SerializeField] public bool jump = false;
    [SerializeField] public bool sprint = false;
    private Vector2 moveDir;
    private Vector2 lookDir;
    public float _turnSpeed = 1500f;

    PlayerInputActions input;

    private GravityBody _gravityBody;

    private void Awake()
    {
        #region input
        input = new PlayerInputActions();
        input.Player.Move.performed += ctx =>
        {
            moveDir = ctx.ReadValue<Vector2>();
        };
        input.Player.Move.canceled += ctx => {
            moveDir = Vector2.zero;
        };
        input.Player.Look.performed += ctx => {
            lookDir = ctx.ReadValue<Vector2>();
        };
        input.Player.Look.canceled += ctx => {
            lookDir = Vector2.zero;
        };
        input.Player.Jump.performed += ctx => {
            jump = true;
        };
        input.Player.Jump.canceled += ctx => {
            jump = false;
        };
/*        input.Player.Reset.performed += ctx => {
            transform.position = spawnPos.position;
        };*/

        //may need to update in the future, could cause bugs depending on implementation
        input.Player.Sprint.performed += ctx => {
            sprint = true;
        };
        input.Player.Sprint.canceled += ctx => {
            sprint = false;
        };
/*        input.Player.Crouch.performed += ctx => {
            controller.ShouldCrouch = true;
        };
        input.Player.Crouch.canceled += ctx => {
            controller.ShouldCrouch = false;
        };
*/
        #endregion input

    }

    void Start()
    {
        _rigidbody = transform.GetComponent<Rigidbody>();
        _gravityBody = transform.GetComponent<GravityBody>();

        //cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        //need to use new input system with GetAxisRaw, probably a turnary?
        overallDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;
        //going to update with collide & slide
        //jump = Physics.CheckSphere(_groundCheck.position, _groundCheckRadius, _groundMask);

        if (Input.GetKeyDown(KeyCode.Space) && jump)
        {
            _rigidbody.AddForce(-_gravityBody.gravDirection * _jumpForce, ForceMode.Impulse);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Vector3 test = new Vector3(transform.position.x, transform.position.y - 1, transform.position.z);
        Gizmos.DrawSphere(test, _groundCheckRadius);
    }

    void FixedUpdate()
    {
        bool isMoving = overallDirection.magnitude > 0.1f;

        if (isMoving)
        {
            Vector3 direction = transform.forward * overallDirection.z;
            _rigidbody.MovePosition(_rigidbody.position + direction * (_speed * Time.fixedDeltaTime));

            Quaternion rightDirection = Quaternion.Euler(0f, overallDirection.x * (_turnSpeed * Time.fixedDeltaTime), 0f);
            Quaternion newRotation = Quaternion.Slerp(_rigidbody.rotation, _rigidbody.rotation * rightDirection, Time.fixedDeltaTime * 3f); ;
            _rigidbody.MoveRotation(newRotation);
        }
    }

    //controls
    #region enableDisable
    void OnEnable()
    {
        input.Enable();
    }

    void OnDisable()
    {
        input.Disable();
    }
    #endregion enableDisable
}
