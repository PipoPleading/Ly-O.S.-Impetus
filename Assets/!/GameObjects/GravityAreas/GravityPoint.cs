using UnityEngine;

public class GravityPoint : GravityArea
{
    [SerializeField] private Vector3 center;
    public override Vector3 GetGravityDirection(GravityBody gb)
    {
        return (center - gb.transform.position).normalized;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, center);
        Gizmos.DrawWireSphere(center, 5);
    }
}
