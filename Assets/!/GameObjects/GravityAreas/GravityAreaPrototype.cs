using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityAreaPrototype : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        PlayerTest playerController = other.GetComponent<PlayerTest>();
        if(playerController != null )
        {
            if (playerController.currentPlanet == transform) return;
            playerController.currentPlanet = transform;
            playerController.EnterNewGravityField();
        }
    }
}
