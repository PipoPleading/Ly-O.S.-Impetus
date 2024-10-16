using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Simple movement with instantaneous starting/stopping.
/// </summary>
[RequireComponent(typeof(KinematicCharacterController))]
public class CharacterMotor_Instant : MonoBehaviour, IPlayerEngine
{

    [SerializeField] private float walkSpeed = 5;
    [SerializeField] private float sprintSpeedMult = 2.0f;

    public Vector3 Accelerate(Vector3 wishDir, Vector3 currentVel, KinematicCharacterController character)
    {
        Vector3 v = wishDir * walkSpeed;
        if (character.IsSprinting) { return v * sprintSpeedMult; }
        return v;
    }
}
