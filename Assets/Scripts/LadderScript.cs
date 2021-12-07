using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LadderScript : MonoBehaviour
{
    public TestMovement player;
    public GameObject lookTarget;
    public GameObject climbButton;
    public GameObject leaveLaddersButton;

    //TODO: If there is some time add slerp to this rotation to make it work as intended

    public void makePlayerClimb(){
        Vector3 direction = (lookTarget.transform.position - player.transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
        player.transform.rotation = lookRotation;
        player.handleLadderClimb(lookTarget);
        climbButton.SetActive(false);
        leaveLaddersButton.SetActive(true);
    }

    public void makePlayerLeaveLadders(){
        player.leaveLadders();
        leaveLaddersButton.SetActive(false);
    }
}
