using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleport : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The destination object with a Teleport script attached.")]
    public Teleport destinationTeleporter;

    [Tooltip("The effect to create when teleporting.")]
    public GameObject teleportEffect;

    private bool teleporterAvailable = true;


    private void OnTriggerEnter(Collider other)
    { 
        if ((other.tag == "Player") && (teleporterAvailable==true) && (destinationTeleporter != null))
        {
  
            if (teleportEffect != null)
            {
              Instantiate(teleportEffect, transform.position, transform.rotation, null);
            }

            destinationTeleporter.teleporterAvailable = false;
            
            CharacterController characterController = other.gameObject.GetComponent<CharacterController>();
            if (characterController != null)
            {
              characterController.enabled = false;
            }


            float heightOffset = transform.position.y - other.transform.position.y;


            other.transform.position = destinationTeleporter.transform.position - new Vector3(0, heightOffset, 0);


            if (characterController != null)
            {
              characterController.enabled = true;
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
      if (other.tag == "Player")
      {

        teleporterAvailable = true;
      }
    }
}
