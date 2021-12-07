using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdditionalTriggerBehaviour : MonoBehaviour
{
    public TestMovement player;

    private void OnTriggerEnter(Collider other) {
        if(other.tag == "LadderEdge") {
            player.reachedTopOfLadders();
        }

        if(other.name == "GroundPlate") {
            player.reachedBottomOfLadders();
        }
    }

    private void OnTriggerExit(Collider other) {
        if(other.tag == "LadderEdge") {
            player.leftTopOfLadders();
        }

        if(other.name == "GroundPlate") {
            player.leftBottomOfLadders();
        }
    }
}
