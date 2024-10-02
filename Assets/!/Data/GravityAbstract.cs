using UnityEngine;
[RequireComponent(typeof(Collider))]  
public abstract class GravityArea : MonoBehaviour
{
    //priority of what field should be applying physics
    [SerializeField] private int priority;
    //higher level of priority for moving between 'planets'
    [SerializeField] private int gravitygroup;

    //may use lambda in c# 10 to dynamically assign the priority to the player
    void Start()
    { 
        transform.GetComponent<Collider>().isTrigger = true;
    }

    public abstract Vector3 GetGravityDirection(GravityBody gb);

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent(out GravityBody gb))
        {
            gb.AddGravityArea(this);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.TryGetComponent(out GravityBody gb))
        {
            gb.RemoveGravityArea(this);
        }
    }
}
