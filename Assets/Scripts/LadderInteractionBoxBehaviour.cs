using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LadderInteractionBoxBehaviour : MonoBehaviour
{

    public GameObject climbButton;
    public GameObject leaveLaddersButton;

    private void OnTriggerEnter(Collider other) {
        if(other.tag == "Player") {
            climbButton.SetActive(true);
            leaveLaddersButton.SetActive(false);
        }
    }

    private void OnTriggerExit(Collider other) {
        if(other.tag == "Player") {
            climbButton.SetActive(false);
            leaveLaddersButton.SetActive(false);
        }
    }
}
