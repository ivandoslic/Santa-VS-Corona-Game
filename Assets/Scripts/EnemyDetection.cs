using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDetection : MonoBehaviour
{
    [SerializeField] Vector3 inputDirection;
    [SerializeField] private EnemyBehaviour currentTarget;

    private TestMovement player;
    private Joystick joystick;

    public LayerMask layerMask;

    void Start()
    {
        player = GetComponentInParent<TestMovement>();
        if(player != null) {
            joystick = player.joystick;
        }
    }

    void Update()
    {
        var camera = Camera.main;
        var forward = camera.transform.forward;
        var right = camera.transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        inputDirection = forward * joystick.Vertical + right * joystick.Horizontal;
        inputDirection = inputDirection.normalized;

        RaycastHit info;

        if(Physics.SphereCast(transform.position, 3f, inputDirection, out info, layerMask)){
            if(info.collider.GetComponent<EnemyBehaviour>()){
                if(info.collider.GetComponent<EnemyBehaviour>().isAttackable()){
                    currentTarget = info.collider.transform.GetComponent<EnemyBehaviour>();
                }
            }
        }
    }

    public float InputMagnitude(){
        return inputDirection.magnitude;
    }

    public EnemyBehaviour getCurrentTarget(){
        return currentTarget;
    }

    public void setCurrentTarget(EnemyBehaviour target){
        currentTarget = target;
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.black;
        Gizmos.DrawRay(transform.position, inputDirection);
        Gizmos.DrawWireSphere(transform.position, 1);
        if(getCurrentTarget() != null){
            Gizmos.DrawSphere(getCurrentTarget().transform.position, .5f);
        }
    }
}
