using UnityEngine;

public class GravityStandard : GravityArea
{
    public override Vector3 GetGravityDirection(GravityBody _gravityBody)
    {
        return -transform.up;
    }
}
