using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.TextCore.Text;
using UnityEngine.Windows;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(GravityBody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Camera))]
public class KinematicCharacterController : MonoBehaviour
{

    //[Header("Controls")]
    [SerializeField]
    //InputActionReference move, look, spring;
    //axis from player input
    private Vector2 moveDir;
    private Vector2 lookDir;
    //PlayerInputActions input;
    bool jumpInput;
    bool dashInput;

    [Header("Movement")]

    [Tooltip("The height of the collider when crouching.")]
    [SerializeField] private float m_crouchHeight = 1f;

    [Tooltip("Whether or not to apply gravity to the controller.")]
    [SerializeField] private bool m_useGravity = true;

    [Tooltip("The max speed above which falling speed will be capped.")]
    [SerializeField] private float m_maxFallSpeed = 20;

    [Tooltip("The default maximum movement speed (can be overridden by character motors).")]
    [field: SerializeField] public float maxSpeed { get; private set; } = 5;



    [Header("Collision")]

    [Tooltip("Which layers the controller should take into account when checking for collisions.")]
    [SerializeField] private LayerMask m_collisionMask;

    [Tooltip(
        "Buffer distance inside the collider from which to start collision checks. Should be very small (but not too small)."
    )]
    [SerializeField] private float m_skinWidth = 0.015f;

    [Tooltip("The maximum angle at which the controller will treat the surface like a slope.")]
    [SerializeField][Range(1, 89)] private float m_maxSlopeAngle = 55;

    [Tooltip("The minimum angle at which the controller will treat a surface like a flat ceiling, stopping vertical movement.")]
    [SerializeField] private float m_minCeilingAngle = 165;

    [Tooltip("The maximum height for a wall to be considered a step that the controller will snap up onto.")]
    [SerializeField] private float m_maxStepHeight = 0.2f;

    [Tooltip("The minimum depth for steps that the controller can climb.")]
    [SerializeField] private float m_minStepDepth = 0.1f;


    [Header("Jump")]

    [Tooltip("The height the controller can jump. Determines gravity along with jumpDistance.")]
    [SerializeField] private float m_jumpHeight = 2;

    [Tooltip("The distance the controller can jump when moving at max speed. Determines gravity along with jumpHeight.")]
    [SerializeField] private float m_jumpDistance = 4;

    [Tooltip("How long (in seconds) after you leave the ground can you still jump.")]
    [SerializeField] private float m_coyoteTime = 0.2f;


    [Header("Debug")]
    [SerializeField] private bool SHOW_DEBUG = false;


    private Vector3 moveAmount;
    private Vector3 calculatedVelocity;
    private Vector3 velocity;
    private Vector3 groundSpeed;

    public bool isGrounded { get; private set; }
    private bool wasGrounded;
    public bool LandedThisFrame { get; private set; }
    public bool IsBumpingHead { get; private set; }

    public bool IsSliding { get; private set; }
    public bool isOnSlope { get; private set; }
    public float SlopeAngle { get; private set; }
    private Vector3 _slopeNormal;

    public bool isClimbingStep { get; private set; }

    private List<RaycastHit> hitPoints;
    private Vector3 _groundPoint;

    public bool ShouldCrouch { get; set; }
    public bool isCrouching { get; private set; }
    private float height;

    public bool isRunning { get; set; }
    public bool isSpriting { get; set; }

    public float Gravity { get; private set; }
    //lowkey need to elaborate on
    private Vector3 gravityVector;
    public bool Coyote { get; private set; }
    private bool jumping, dashing;
    private float jumpForce;

    private Rigidbody rb;
    private GravityBody gb;
    private CapsuleCollider col;
    private Camera cam;

    //temp var holding the collider's boundaries
    private Bounds bounds;
    private Vector3 sphereOffsetBottom, sphereOffsetTop;
    //for debug gizmos
    private Color[] _colors = { Color.red, new Color(1, 0.5f, 0), Color.yellow, Color.green, Color.cyan, Color.blue, Color.magenta };

    //accelScaling variables
    [Header("Scaling for acceleration, vis a vi Pizza Tower")]
    [SerializeField]
    private float walkSpeed = 5f;
    [SerializeField]
    private float runSpeed = 2f;
    [SerializeField]
    private float sprintSpeed = 2f;

    //final movement vector
    private Vector2 dir;

    private Vector3 overallDirection;

    public Text text;


    void Awake()
    {
        //component initializations so i have to do less stuff per scene
        //input = new PlayerInputActions();

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        gb = GetComponent<GravityBody>();
        gb.gravityForce = 800;

        col = GetComponent<CapsuleCollider>();
        col.center = new Vector3(0, col.height / 2, 0);
        height = col.height;

        float halfDist = m_jumpDistance / 2;
        Gravity = (-2 * m_jumpHeight * maxSpeed * maxSpeed) / (halfDist * halfDist);
        Debug.Log("Gravity: " + Gravity);
        jumpForce = (2 * m_jumpHeight * maxSpeed) / halfDist;

        sphereOffsetBottom = new Vector3(0, col.radius, 0);
        sphereOffsetTop = new Vector3(0, col.height - col.radius, 0);

        hitPoints = new List<RaycastHit>();

        cam = GetComponent<Camera>();

        //player input reading, may put on a timer later for load screens and such idk
/*        input.Player.Jump.performed += ctx => {
            jumpInput = true;
        };
        input.Player.Jump.canceled += ctx => {
            jumpInput = false;
        };
        input.Player.Look.performed += ctx => {
            lookDir = ctx.ReadValue<Vector2>();
        };
        input.Player.Look.canceled += ctx => {
            lookDir = Vector2.zero;
        };*/

    }

    private void Start()
    {
        //rb = transform.GetComponent<Rigidbody>();

        //cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        /*#if UNITY_EDITOR
                float halfDist = m_jumpDistance / 2;
                Gravity = (-2 * m_jumpHeight * maxSpeed * maxSpeed) / (halfDist * halfDist);
                jumpForce = (2 * m_jumpHeight * maxSpeed) / halfDist;

                sphereOffsetBottom = new Vector3(0, col.radius, 0);
                sphereOffsetTop = new Vector3(0, col.height - col.radius, 0);
        #endif*/

        //moveDir = move.action.ReadValue<Vector2>();
        InputUsage();

        overallDirection = new Vector3(moveDir.x, 0f, moveDir.y).normalized;
    }

    void OnDrawGizmos()
    {
        if (SHOW_DEBUG)
        {
            Debug.DrawRay(transform.position, calculatedVelocity, Color.green, Time.deltaTime);

            if (isGrounded || IsSliding)
            {
                Gizmos.DrawWireSphere(_groundPoint, 0.05f);
            }

            if (hitPoints == null) { return; }

            int i = 0;
            foreach (RaycastHit hit in hitPoints)
            {
                Color color = _colors[i % (_colors.Length - 1)];
                Gizmos.DrawWireSphere(hit.point, 0.1f);
                Debug.DrawRay(hit.point, hit.normal, color, Time.deltaTime);
                i++;
            }
        }
    }

    void setDebugText()
    {
        text.text = $"FPS: {(1 / Time.deltaTime).ToString("F0")}\n" +
                    $"deltaTime: {Time.deltaTime}\n" +
        $"Timescale: {Time.timeScale}\n\n" +

                    $"Gravity: {Gravity}\n" +
                    $"Speed: {velocity.magnitude.ToString("f2")}\n" +
                    //$"Acceleration: {controller.Acceleration().ToString("F4")}\n" +
                    $"Velocity: {velocity.ToString("F6")}\n" +
                    $"Position: {transform.position.ToString("F4")}\n" +
                    $"LookDir: {cam.transform.eulerAngles.ToString("F4")}\n" +
                    $"MoveDir: {dir.ToString("F4")}\n" +
        $"Input: {moveDir}\n\n" +

                    $"Grounded: {isGrounded}\n" +
                    $"On Slope: {isOnSlope}\n" +
                    $"Slope Angle: {SlopeAngle}\n" +
                    $"Sliding: {IsSliding}\n" +
                    $"Climbing Step: {isClimbingStep}\n\n" +

                    $"Crouching: {isCrouching}\n" +
        //$"Sprinting: {controller.motor.isSprinting}\n" +
                    $"Try Jump: {jumpInput}\n" +
                    $"Coyote: {Coyote}\n"
        ;
    }

    ///  Moves the attached rigidbody in the desired direction, taking into account gravity, collisions, and slopes, using
    ///  the "collide and slide" algorithm. Returns the current calculatedVelocity. (Pick either this or Move())
    public Vector3 Move(Vector2 moveDir, bool shouldJump, bool shouldDash)
    {
        bounds = col.bounds;
        bounds.Expand(-2 * m_skinWidth);

        isCrouching = UpdateCrouchState(ShouldCrouch);


        //seperating from motorinstance & iplayerengine for optimization
        groundSpeed = AccelScaling(new Vector3(moveDir.x, 0, moveDir.y));

        moveAmount = groundSpeed * Time.deltaTime;

        IsBumpingHead = CeilingCheck(transform.position);
        isGrounded = GroundCheck(transform.position);
        LandedThisFrame = isGrounded && !wasGrounded;



        // coyote time
        if (wasGrounded && !isGrounded)
        {
            StartCoroutine(CoyoteTime());
        }

        // scale movement to slope angle
        if (isGrounded && isOnSlope && !IsBumpingHead)
        {
            moveAmount = ProjectAndScale(moveAmount, _slopeNormal);
        }

        hitPoints.Clear();

        // --- collision   
        moveAmount = CollideAndSlide(moveAmount, transform.position);

        // --- gravity
        if (m_useGravity)
        {
            //need to introduce gravity body code here
            jumping = false;
            //jump code
            if (shouldJump && (isGrounded || Coyote))
            {
                if (LandedThisFrame)
                {
                    //bunnyhop anim for state machine
                }

                if (shouldDash)
                {

                }
                gravityVector.y = jumpForce * Time.deltaTime;
                jumping = true;
                Coyote = false;
            }

            if ((isGrounded || wasGrounded) && !jumping)
            {
                moveAmount += SnapToGround(transform.position + moveAmount);
            }

            if ((isGrounded && !jumping) || (!isGrounded && IsBumpingHead))
            {
                gravityVector = new Vector3(0, Gravity, 0) * Time.deltaTime * Time.deltaTime;
            }
            else if (gravityVector.y > -m_maxFallSpeed)
            {
                gravityVector.y += Gravity * Time.deltaTime * Time.deltaTime;
            }

            moveAmount += CollideAndSlide(gravityVector, transform.position + moveAmount, true);
        }

        // ACTUALLY MOVE THE RIGIDBODY
        rb.MovePosition(transform.position + moveAmount);

        wasGrounded = isGrounded;
        calculatedVelocity = moveAmount / Time.deltaTime;
        return calculatedVelocity;
    }

    IEnumerator CoyoteTime()
    {
        Coyote = true;
        yield return new WaitForSeconds(m_coyoteTime);
        Coyote = false;
    }

    private Vector3 CollideAndSlide(Vector3 dir, Vector3 pos, bool gravityPass = false)
    {
        Vector3 accumulator = Vector3.zero;
        Vector3 planeNormal1 = new Vector3();

        bool climbingStep = false;

        //3 is the number of passes used per fixedupdate/game frame
        for (int i = 0; i < 3; i++)
        {
            if (Mathf.Approximately(dir.magnitude, 0)) { break; }

            float dist = dir.magnitude + m_skinWidth;
            Vector3 direction = dir.normalized;
            if (Physics.CapsuleCast(
                pos + sphereOffsetBottom,
                pos + sphereOffsetTop,
                bounds.extents.x,
                direction,
                out RaycastHit hit,
                dist,
                m_collisionMask
            ))
            {
                //collision
                hitPoints.Add(hit);

                float surfaceAngle = Vector3.Angle(Vector3.up, hit.normal);
                Vector3 snapToSurface = direction * (hit.distance - m_skinWidth);

                if (snapToSurface.magnitude <= m_skinWidth) { snapToSurface = Vector3.zero; }
                if (gravityPass && surfaceAngle <= m_maxSlopeAngle)
                {
                    accumulator += snapToSurface;
                    break;
                }

                Vector3 leftover = dir - snapToSurface;

                if (i == 0)
                {
                    planeNormal1 = hit.normal;
                    // treat steep slope as flat wall when grounded
                    if (surfaceAngle > m_maxSlopeAngle && isGrounded && !gravityPass)
                    {
                        #region stair detection
                        float stepOffset = hit.point.y - _groundPoint.y;
                        Vector3 stepDirection = hit.point - pos;
                        stepDirection = new Vector3(stepDirection.x, 0, stepDirection.z).normalized;

                        if (stepOffset < m_maxStepHeight && stepOffset > m_skinWidth)
                        {
                            float stepDist = col.radius - stepOffset - m_skinWidth;
                            if (Physics.CapsuleCast(
                                pos + sphereOffsetBottom + snapToSurface + new Vector3(0, stepDist, 0),
                                pos + sphereOffsetTop + snapToSurface + new Vector3(0, stepDist, 0),
                                bounds.extents.x,
                                stepDirection,
                                out RaycastHit stepCheck,
                                m_minStepDepth + 2 * m_skinWidth,
                                m_collisionMask
                            ))
                            {
                                print(stepCheck.distance);
                                float stepWallAngle = Vector3.Angle(stepCheck.normal, Vector3.up);
                                if ((stepCheck.distance - m_skinWidth) > m_minStepDepth || stepWallAngle <= m_maxSlopeAngle)
                                {
                                    climbingStep = true;
                                }
                            }
                            else
                            {
                                climbingStep = true;
                            }

                            if (climbingStep)
                            {
                                snapToSurface.y += stepDist;
                                snapToSurface += stepDirection * 2 * m_skinWidth;
                                snapToSurface += SnapToGround(pos + snapToSurface + leftover);
                            }
                        }
                        #endregion

                        planeNormal1 = new Vector3(planeNormal1.x, 0, planeNormal1.z).normalized;
                        leftover = new Vector3(leftover.x, 0, leftover.z);
                    }
                    leftover = Vector3.ProjectOnPlane(leftover, planeNormal1);
                    dir = leftover;
                }
                else if (i == 1)
                {
                    Vector3 crease = Vector3.Cross(planeNormal1, hit.normal).normalized;
                    if (SHOW_DEBUG) Debug.DrawRay(hit.point, crease, Color.cyan, Time.deltaTime);
                    float dis = Vector3.Dot(leftover, crease);
                    dir = crease * dis;
                }

                if (i < 2)
                {
                    accumulator += snapToSurface;
                    pos += snapToSurface;
                }
            }
            else
            {  // no collision
                accumulator += dir;
                break;
            }
        }
        return accumulator;
    }

    private Vector3 ProjectAndScale(Vector3 vector, Vector3 planeNormal)
    {
        float mag = vector.magnitude;
        vector = Vector3.ProjectOnPlane(vector, planeNormal).normalized;
        vector *= mag;
        return vector;
    }

    private Vector3 SnapToGround(Vector3 pos)
    {
        float dist = m_maxStepHeight + m_skinWidth;
        //casting
        if (Physics.CapsuleCast(
            pos + sphereOffsetBottom,
            pos + sphereOffsetTop,
            bounds.extents.x,
            Vector3.down,
            out RaycastHit hit,
            dist,
            m_collisionMask)
        )
        //on hit
        {
            float surfaceAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (hit.distance - m_skinWidth < m_maxStepHeight && surfaceAngle <= m_maxSlopeAngle)
            {
                isGrounded = true;
                return new Vector3(0, -(hit.distance - m_skinWidth), 0);
            }
        }
        //else
        return Vector3.zero;
    }

    private bool GroundCheck(Vector3 pos)
    {
        IsSliding = false;
        bool grounded = false;

        float dist = 2 * m_skinWidth;
        Vector3 origin = pos + sphereOffsetBottom;
        RaycastHit[] hits = Physics.SphereCastAll(origin, bounds.extents.x, Vector3.down, dist, m_collisionMask);
        if (hits.Length > 0)
        {
            foreach (RaycastHit hit in hits)
            {
                if (Mathf.Approximately(hit.distance, 0)) { continue; }

                float angle = Vector3.Angle(Vector3.up, hit.normal);
                SlopeAngle = angle;
                _slopeNormal = hit.normal;
                _groundPoint = hit.point;
                if (angle <= m_maxSlopeAngle)
                {
                    IsSliding = false;
                    isOnSlope = angle > 0.1f;
                    grounded = true;
                    break;
                }
                else { IsSliding = true; }
            }
        }
        return grounded;
    }

    private bool CeilingCheck(Vector3 pos)
    {
        float dist = 2 * m_skinWidth;
        Vector3 origin = pos + sphereOffsetTop;

        RaycastHit hit;
        if (Physics.SphereCast(origin, bounds.extents.x, Vector3.up, out hit, dist, m_collisionMask))
        {
            float angle = Vector3.Angle(Vector3.up, hit.normal);
            float hitAngle = Vector3.Angle(moveAmount.normalized, hit.normal);
            if (angle >= m_minCeilingAngle || hitAngle >= m_minCeilingAngle)
            {
                return true;
            }
        }
        return false;
    }

    private bool UpdateCrouchState(bool shouldCrouch)
    {
        if (shouldCrouch && !isCrouching)
        {
            col.height = m_crouchHeight;
            col.center = new Vector3(0, col.height / 2, 0);
            return true;
        }
        else if (isCrouching && !shouldCrouch)
        {
            if (CanUncrouch())
            {
                col.height = height;
                col.center = new Vector3(0, col.height / 2, 0);
                return false;
            }
        }
        return isCrouching;
    }

    private bool CanUncrouch()
    {
        float dist = height - m_crouchHeight + m_skinWidth;
        Vector3 origin = bounds.center + new Vector3(0, col.height / 2 - col.radius, 0);
        return !Physics.SphereCast(origin, bounds.extents.x, Vector3.up, out RaycastHit hit, dist, m_collisionMask);
    }

    private Vector3 AccelScaling(Vector3 wishDir)
    {
        //snaps to 0 the frame a movement key is no longer held, need to fix with container variable?
        Vector3 v = wishDir * walkSpeed;
        if (isRunning) { v = v * runSpeed; }
        if (isSpriting) {  v = v * sprintSpeed; }
        return v;
    }


    void OnEnable()
    {
        //input.Enable();
    }

    void OnDisable()
    {
        //input.Disable();
    }

    private void FixedUpdate()
    {
        //omnidirectional gravity
        Vector3 direction;/*
        bool isMoving = overallDirection.magnitude > 0.1f;

        direction = transform.forward * overallDirection.z;
        rb.MovePosition(rb.position + direction * (_speed * Time.fixedDeltaTime));

        Quaternion rightDirection = Quaternion.Euler(0f, overallDirection.x * (_turnSpeed * Time.fixedDeltaTime), 0f);
        Quaternion newRotation = Quaternion.Slerp(rb.rotation, rb.rotation * rightDirection, Time.fixedDeltaTime * 3f); ;
        rb.MoveRotation(newRotation);*/

        //collide and slide


        direction = (cam.transform.forward * moveDir.y + cam.transform.right * moveDir.x);
        direction.y = 0;
        direction.Normalize();
        dir = new Vector2(direction.x, direction.z);

        velocity = Move(dir, jumpInput, dashInput);
    }


    private void InputUsage()
    {
        moveDir = UserInput.instance.moveInput;
        jumpInput = UserInput.instance.jumpHeld;
        dashInput = UserInput.instance.dashPressed;
    }
}