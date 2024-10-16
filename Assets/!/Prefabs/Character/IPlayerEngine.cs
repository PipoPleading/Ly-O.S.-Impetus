using UnityEngine;

public interface IPlayerEngine
{
    Vector3 Accelerate(Vector3 wishDir, Vector3 currentVel, KinematicCharacterController character);
}
