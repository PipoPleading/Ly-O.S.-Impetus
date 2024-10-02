using UnityEngine;
using System.Linq;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class GravityBody : MonoBehaviour
{
    //mostly static, but may need to be customized on a per area basis?
    [SerializeField] public float gravityForce = 800;

    private Rigidbody rb;
    private List<GravityArea> ga;
    public Vector3 gravDirection
    {
        get
        {
            if (ga.Count == 0) return Vector3.zero;
            //need to update to c# version 10 first
            //ga.Sort((area1, area2)) => area1.Priority.CompareTo(area2.Priority);
            return ga.Last().GetGravityDirection(this).normalized;
        }
    }
    private void Start()
    {
        rb = transform.GetComponent<Rigidbody>();
        ga = new List<GravityArea>();
    }

    void FixedUpdate()
    {
        rb.AddForce(gravDirection * (gravityForce * Time.fixedDeltaTime), ForceMode.Acceleration);

        Quaternion upRotation = Quaternion.FromToRotation(transform.up, -gravDirection);
        Quaternion newRotation = Quaternion.Slerp(rb.rotation, upRotation * rb.rotation, Time.fixedDeltaTime * 3f); ;
        rb.MoveRotation(newRotation);
    }

    public void AddGravityArea(GravityArea area)
    {
        ga.Add(area);
    }    
    public void RemoveGravityArea(GravityArea area)
    {
        ga.Remove(area);
    }
}
