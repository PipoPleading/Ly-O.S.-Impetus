using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.TextCore.Text;
using UnityEngine.Windows;
using Unity.VisualScripting;
using Unity.Cinemachine;
using System;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(GravityBody))]
[RequireComponent(typeof(CapsuleCollider))]
//[RequireComponent(typeof(Camera))]
public class KinematicCharacterController : MonoBehaviour
{

    //[Header("Controls")]
    [SerializeField]
    //InputActionReference move, look, spring;
    //axis from player input
    private Vector2 moveDir;
    private Vector2 lookDir;

    [Header("Grappling")]
    public bool freeze;

    public float grappleMax;
    public float grappleDelay;
    public float overShootY;

    public LayerMask grappleable;

    private Vector3 grapplePoint;

    public Transform armTip;
    public LineRenderer lr;

    public float grapplingCd;
    public float grapplingCdTimer;
    public bool grappling;
    public bool activeGrapple;

    //PlayerInputActions input;
    [Header("Inputs")]
    public bool jumpInput;
    public bool dashInput;
    public bool aimInput;
    public bool shootInput;
    public bool aimRelease;

    [Header("Camera")]
    public CameraStyle currentStyle;
    
    public enum CameraStyle
    {
        Normal,
        Aim,
        Other
    }

    [Header("States")]
    public PlayerState playerState;
    public enum PlayerState
    {
        idle,
        move,
        jump,
        bunnyhop,
        spin,
        fall,
        dash,
        superdash,
        //grappling guys
        swing,
        pull,
    }

    [Header("Dash")]
    //dash stuff
    public float desiredMoveSp;
    public float lastDesiredMoveSp;
    public bool keepMomentum;

    public bool superDash;
    public float superDashScalar = 1.10f;

    public float dashForce;
    public float dashVerticalForce;
    public float dashDuration;

    public float dashCd;
    public float dashCdTimer;
    public Vector3 forceToApply;

    //camera switching
    public CinemachineCamera normalCam;
    public CinemachineCamera aimCam;
    public CinemachineCamera otherCam;

    //the most important guy for some reason
    Vector3 direction;

    [Header("Movement")]
    [SerializeField] private float m_crouchHeight = 1f;

    [SerializeField] private bool m_useGravity = true;

    [SerializeField] public float m_maxFallSpeed = 20;

    [Header("Collision")]

    [SerializeField] private LayerMask m_collisionMask;

    [SerializeField] private float m_skinWidth = 0.015f;

    [SerializeField][Range(1, 89)] private float m_maxSlopeAngle = 55;

    [SerializeField] private float m_minCeilingAngle = 165;

    [SerializeField] private float m_maxStepHeight = 0.2f;

    [SerializeField] private float m_minStepDepth = 0.1f;

    [SerializeField] private float speedScalar = 5;


    [Header("Jump")]

    [SerializeField] private float m_jumpHeight = 4;

    [SerializeField] private float m_jumpDistance = 4;

    [SerializeField] private float m_coyoteTime = 0.15f;


    [Header("Debug")]
    [SerializeField] private bool SHOW_DEBUG = false;

    public float playerTimeScale = 1f;

    private Vector3 moveAmount;
    private Vector3 calculatedVelocity;
    private Vector3 velocity;
    private Vector3 groundSpeed;
    private Vector3 dashSpeed;

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

    [SerializeField] public float Gravity;
    [SerializeField] public float storedGravity;

    public Vector3 gravityVector;
    public bool coyote { get; private set; }
    public bool jumping, dashing;
    private float jumpForce;

    private Rigidbody rb;
    private GravityBody gb;
    private CapsuleCollider col;
    [SerializeField] Transform cam;


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

    //used to be overallDirection
    public Transform orientation;
    public Transform player;
    public Transform playerObj;
    public Transform aimTransform;

    public float rotationSpeed;
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
        Gravity = (-2 * m_jumpHeight * speedScalar * speedScalar) / (halfDist * halfDist);
        storedGravity = Gravity;
        Debug.Log("Gravity: " + Gravity);
        jumpForce = (2 * m_jumpHeight * speedScalar) / halfDist;

        sphereOffsetBottom = new Vector3(0, col.radius, 0);
        sphereOffsetTop = new Vector3(0, col.height - col.radius, 0);

        hitPoints = new List<RaycastHit>();

    }

    private void Start()
    {
        //rb = transform.GetComponent<Rigidbody>();

        //cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
                    $"coyote: {coyote}\n"
        ;
    }

    private void StateHandler()
    {
        if (shootInput)
        {
            StartGrapple();
        }

        //this is the state machine
        //handles whether or not the player is dashing moment to moment
        groundSpeed = !dashing ? AccelScaling(new Vector3(dir.x, 0, dir.y)) : AccelScaling(new Vector3(dir.x + forceToApply.x, 0, dir.y + forceToApply.z));

        //jump logic?

        //on jump, -> fall state
        if (isGrounded || coyote)
        {
            //walking
            float scalar = 1;

            //turnary for superdash scalar
            scalar = superDash ? superDashScalar : 1;

            moveAmount = (groundSpeed * scalar) * Time.deltaTime;

            if(groundSpeed == Vector3.zero)
            {
                playerState = PlayerState.idle;
            }
            else
            {
                playerState = PlayerState.move;
            }
        }

        //mini state machine?? need to consolodate

/*        // isDashing
        if (jumpInput && (isGrounded || coyote))
        {
            gravityVector.y = (jumpForce * superDashScalar) * Time.deltaTime;
        }
        else
        {
            moveAmount = dashSpeed * Time.deltaTime;
            //can make it += to get void fiend dash, might make that an upgrade cause it feels cool & swag
            //could be cool for vertical wavedashing <- insane
            gravityVector.y -= 2 * Time.deltaTime;
        }*/

        //collisions pt.1
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

        //resetting raycasts
        hitPoints.Clear();

        // collisions pt.2
        moveAmount = CollideAndSlide(moveAmount, transform.position);

        //falling and shit (should remove bool way later)
        if (m_useGravity)
        {
            if (dashInput && !dashing && !grappling)
            {
                Dash();
            }

            jumping = false;
            superDash = false;
            if (jumpInput && (isGrounded || coyote))
            {
                Jump();
            }
            //superDash = false;


            /*#if UNITY_EDITOR
            float halfDist = m_jumpDistance / 2;
            Gravity = (-2 * m_jumpHeight * maxSpeed * maxSpeed) / (halfDist * halfDist);
            jumpForce = (2 * m_jumpHeight * maxSpeed) / halfDist;

            sphereOffsetBottom = new Vector3(0, col.radius, 0);
            sphereOffsetTop = new Vector3(0, col.height - col.radius, 0);
            #endif*/


            //anything and everything jump related should be after this point

            if ((isGrounded || wasGrounded) && !jumping && !activeGrapple)
            {
                moveAmount += SnapToGround(transform.position + moveAmount);
            }

            // gravityVector Affectors
            if ((isGrounded && !jumping) || (!isGrounded && IsBumpingHead))
            {
                //need to put old gravity code here
                // THE DEVIL
                gravityVector = new Vector3(0, Gravity, 0) * Time.deltaTime * Time.deltaTime;
            }
            else if (gravityVector.y > -m_maxFallSpeed)
            {
                //accelerates in the direction of the gravity vector
                gravityVector.y += Gravity * Time.deltaTime * Time.deltaTime;
            }
            /*//debug
            if (UnityEngine.Input.GetKeyDown(KeyCode.M))
            {
                gravityVector.z += 0.1f;
            }*/

            //projecting with move amount to cull redundancies 
            moveAmount += CollideAndSlide(gravityVector, transform.position + moveAmount, true);
        }

    }

    ///  Moves the attached rigidbody in the desired direction, taking into account gravity, collisions, and slopes, using
    ///  the "collide and slide" algorithm. Returns the current calculatedVelocity. (Pick either this or Move())
    public void Move()
    {
        bounds = col.bounds;
        bounds.Expand(-2 * m_skinWidth);

        isCrouching = UpdateCrouchState(ShouldCrouch);

        StateHandler();

        // ACTUALLY MOVE THE RIGIDBODY
        rb.MovePosition(transform.position + moveAmount);

        //checks 'last frame'
        wasGrounded = isGrounded;

        //set every fixed update
        calculatedVelocity = moveAmount / Time.deltaTime;
    }

    private void Dash()
    {
        if (dashCdTimer > 0) return;
        else dashCdTimer = dashCd;

        dashing = true;

        //playerState = PlayerState.dash;
        //this is used in the movement vector
        forceToApply = orientation.forward * dashForce + orientation.up * dashVerticalForce;

        if(!isGrounded && !jumping)
        {
            gravityVector.y -= 2 * Time.deltaTime;
            Gravity = 0;
        }

        Debug.Log("dash");
        Debug.Log(forceToApply.magnitude);

        Invoke(nameof(ResetDash), dashDuration);
    }

    private void Jump()
    {
        if(dashing)
        {
            //super dash, pog
            gravityVector.y = (jumpForce * superDashScalar) * Time.deltaTime;
            playerState = PlayerState.superdash;
        }
        else
        {
            gravityVector.y = jumpForce * Time.deltaTime;
            jumping = true;
            coyote = false;
            if (LandedThisFrame)
            {
                playerState = PlayerState.bunnyhop;
            }
            else
            {
                playerState = PlayerState.jump;
            }
        }

    }

    private void ResetDash()
    {
        Gravity = storedGravity;
        dashing = false;
    }

    IEnumerator CoyoteTime()
    {
        coyote = true;
        yield return new WaitForSeconds(m_coyoteTime);
        coyote = false;
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

    // might need to polish later
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
    void Update()
    {
        InputUsage();

        //feels bad in the normal state machine/fixed update
        if (currentStyle == CameraStyle.Normal)
        {
            Vector3 inputDir = orientation.forward * lookDir.y + orientation.right * lookDir.x;

            if (moveDir != Vector2.zero)
            {
                playerObj.forward = Vector3.Slerp(playerObj.forward, inputDir.normalized, Time.deltaTime * rotationSpeed);
            }
        }
        else if (currentStyle == CameraStyle.Aim)
        {
            //Vector3 aimLook = aimTransform.position - new Vector3(direction.x, aimTransform.position.y, direction.z); ;
            //orientation.forward = direction;

            if (direction != Vector3.zero) playerObj.forward = direction;
        }
        //overallDirection = new Vector3(moveDir.x, 0f, moveDir.y).normalized;
    }

    private void Timers()
    {
        if (grapplingCdTimer > 0) grapplingCdTimer -= Time.deltaTime;

        if (dashCdTimer > 0) dashCdTimer -= Time.deltaTime;

    }

    private void Aim()
    {
        if (aimInput) SwitchCameraStyle(CameraStyle.Aim);
        else SwitchCameraStyle(CameraStyle.Normal);

        if (!isGrounded && aimInput)
        {
            BulletTime();
        }
        else
        {
            StandardTime();
        }
    }

    private void FixedUpdate()
    {
        //omnidirectional gravity
        /*
        bool isMoving = overallDirection.magnitude > 0.1f;

        direction = transform.forward * overallDirection.z;
        rb.MovePosition(rb.position + direction * (_speed * Time.fixedDeltaTime));

        Quaternion rightDirection = Quaternion.Euler(0f, overallDirection.x * (_turnSpeed * Time.fixedDeltaTime), 0f);
        Quaternion newRotation = Quaternion.Slerp(rb.rotation, rb.rotation * rightDirection, Time.fixedDeltaTime * 3f); ;
        rb.MoveRotation(newRotation);*/

        //collide and slide
        /*
                print("Cam forward:" + cam.transform.forward);
                print("Cam right:" + cam.transform.right);*/
        //direction = (cam.transform.forward * moveDir.y + cam.transform.right * moveDir.x);
        //potentially could cause problems with gravity rotation

        Timers();

        Aim();

        //physics 
        //direction = (orientation.forward * moveDir.y + orientation.right * moveDir.x);
        direction = (cam.forward * moveDir.y + cam.right * moveDir.x);
        direction.y = 0;
        direction.Normalize();

        //gets input into collide & slide every fixedupdate

        //rotate orientation
        //Vector3 viewDir = player.position - new Vector3(transform.position.x, player.position.y, transform.position.z);
        if (direction != Vector3.zero) orientation.forward = direction;

        //rotate player object (ly-os <3)


        //for movement only
        dir = new Vector2(direction.x, direction.z);

        Move();

        velocity = calculatedVelocity;


    }

    private void BulletTime()
    {
        //lerps between standard and bullet time
        playerTimeScale = Mathf.Lerp(0.2f, 1, 0);
        Time.timeScale = playerTimeScale;
    }

    private void StandardTime()
    {
        playerTimeScale = Mathf.Lerp(0.2f, 1, 1);
        Time.timeScale = playerTimeScale;
    }

    private void StartGrapple()
    {
        if (grapplingCdTimer > 0) return;

        grappling = true;

        m_useGravity = false;

        RaycastHit hit;
        if (Physics.Raycast(cam.position, cam.forward, out hit, grappleMax, grappleable))
        {
            grapplePoint = hit.point;

            Invoke(nameof(ExecuteGrapple), grappleDelay);
        }
        else
        {
            grapplePoint = cam.position + cam.forward * grappleMax;

            Invoke(nameof(StopGrapple), grappleDelay);

        }

        lr.enabled = true;
        lr.SetPosition(1, grapplePoint);
    }

    private void LateUpdate()
    {
        if(grappling)
            lr.SetPosition(0, armTip.position);
    }

    private void ExecuteGrapple()
    {
        freeze = false;

        Vector3 lowest = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);

        float grappleRelativeY = grapplePoint.y - lowest.y;
        float arcHeight = grappleRelativeY + overShootY;

        if (grappleRelativeY < 0) arcHeight = overShootY;

        MoveToPosition(grapplePoint,arcHeight);

        Invoke(nameof(StopGrapple), 1f);

    }

    private Vector3 velocityToSet;
    public void MoveToPosition(Vector3 targetPos, float trajectoryHeight)
    {
        activeGrapple = true;

        // forceToApply gets set for move and dash, you would need
        forceToApply = CalculateJumpVelocity(transform.position, targetPos, trajectoryHeight);
        Invoke(nameof(SetVelocity), 0.1f);
    }

    private void SetVelocity()
    {
        velocity = velocityToSet;
    }

    private void StopGrapple()
    {
        grappling = false;

        grapplingCdTimer = grapplingCd;

        lr.enabled = false; 

        m_useGravity = true;    
    }

    private void InputUsage()
    {
        moveDir = UserInput.instance.moveInput;
        lookDir = UserInput.instance.lookInput;
        jumpInput = UserInput.instance.jumpPressed;
        dashInput = UserInput.instance.dashPressed;
        aimInput = UserInput.instance.aimHeld;
        shootInput = UserInput.instance.shootPressed;
        aimRelease = UserInput.instance.aimReleased;
    }

    public Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
    {
        float displacementY = endPoint.y - startPoint.y;

        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0 , endPoint.z - startPoint.z);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * Gravity *  trajectoryHeight);
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / Gravity)
            + Mathf.Sqrt(2 * (displacementY - trajectoryHeight) / Gravity));

        return velocityXZ + velocityY;
    }

    private void SwitchCameraStyle(CameraStyle newStyle)
    {
        normalCam.Priority.Value = 0;
        aimCam.Priority.Value = 0;
        otherCam.Priority.Value = 0;
        
        if (currentStyle == CameraStyle.Normal) normalCam.Priority.Value = 1;
        if (currentStyle == CameraStyle.Aim) aimCam.Priority.Value = 1;
        if (currentStyle == CameraStyle.Other) otherCam.Priority.Value = 1;

        currentStyle = newStyle;
    }

}