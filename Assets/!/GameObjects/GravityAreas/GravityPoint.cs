using UnityEngine;

public class GravityPoint : GravityArea
{
    [SerializeField] private Transform center;
    public override Vector3 GetGravityDirection(GravityBody gb)
    {
        return (center.position - gb.transform.position).normalized;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, center.position);
        Gizmos.DrawWireSphere(center.position, 5);
    }
}
