using System;
using UnityEngine;
using UnityEngine.InputSystem;


public class UserInput : MonoBehaviour
{
    public static UserInput instance;

    public Vector2 moveInput;
    public Vector2 lookInput;

    public bool jumpPressed;
    public bool jumpHeld;
    public bool jumpReleased;

    public bool dashPressed;

    public bool pausePressed;
    public bool pauseHeld;

    public bool aimHeld;
    public bool aimReleased;

    public bool shootPressed;

    private PlayerInput playerInput;

    private InputAction moveAction, lookAction, jumpAction, dashAction, pauseAction, aimAction, shootAction;

    public void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }

        playerInput = GetComponent<PlayerInput>();

        SetInputActions();
    }
    private void Update()
    {
        UpdateInputs();
    }
    private void SetInputActions()
    {
        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
        jumpAction = playerInput.actions["Jump"];
        dashAction = playerInput.actions["Dash"];
        pauseAction = playerInput.actions["Pause"];
        aimAction = playerInput.actions["Aim"];
        shootAction = playerInput.actions["Shoot"];
    }

    private void UpdateInputs()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        lookInput = lookAction.ReadValue<Vector2>();

        jumpPressed = jumpAction.WasPressedThisFrame();
        jumpHeld = jumpAction.IsPressed();
        jumpReleased = jumpAction.WasReleasedThisFrame();

        dashPressed = dashAction.WasPressedThisFrame(); 

        pausePressed = pauseAction.WasPressedThisFrame();
        pauseHeld = pauseAction.IsPressed();

        aimHeld = aimAction.IsPressed();
        aimReleased = aimAction.WasReleasedThisFrame();

        shootPressed = shootAction.IsPressed();

    }
}
