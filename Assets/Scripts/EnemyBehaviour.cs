using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AI;
using DG.Tweening;

public class EnemyBehaviour : MonoBehaviour
{
    private CharacterController characterController;
    private Animator animator;
    [SerializeField] private PlayerCombatSystem playerCombat;
    [SerializeField] private EnemyDetection enemyDetection;

    [Header("Stats")]
    public int health = 3;
    private float mvSpeed = 1;
    private Vector3 moveDirection;

    public float lookRadius = 10f;
    bool isAttackableBool = true;

    Transform target;
    NavMeshAgent agent;

    [Header("States")]
    [SerializeField] private bool isPreparingAttack;
    [SerializeField] private bool isMoving;
    [SerializeField] private bool isRetreating;
    [SerializeField] private bool isLockedTarget;
    [SerializeField] private bool isStunned;
    [SerializeField] private bool isWaiting = true;

    // Coroutines:

    private Coroutine PrepareAttackCoroutine;
    private Coroutine RetreatCoroutine;
    private Coroutine DamageCoroutine;
    private Coroutine MovementCoroutine;

    // Animator variables
    int inputMagnitudeHash;
    int strafeDirectionHash;

    // Events
    public UnityEvent<EnemyBehaviour> OnRetreat;

    // Start is called before the first frame update
    void Start()
    {
        target = PlayerManager.instance.player.transform;
        //agent = GetComponent<NavMeshAgent>();

        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();

        playerCombat = FindObjectOfType<PlayerCombatSystem>();
        enemyDetection = playerCombat.GetComponentInChildren<EnemyDetection>();

        inputMagnitudeHash = Animator.StringToHash("InputMagnitude");
        strafeDirectionHash = Animator.StringToHash("StrafeDirection");

        playerCombat.OnHit.AddListener((x) => OnPlayerHit(x));

        MovementCoroutine = StartCoroutine(EnemyMovement());
    }

    IEnumerator EnemyMovement(){
        yield return new WaitUntil(() => isWaiting == true);

        int randomChance = Random.Range(0, 2);

        if(randomChance == 1){
            int randomDir = Random.Range(0, 2);
            moveDirection = randomDir == 1 ? Vector3.right : Vector3.left;
            isMoving = true;
        } else {
            StopMoving();
        }

        yield return new WaitForSeconds(1);

        MovementCoroutine = StartCoroutine(EnemyMovement());
    }

    // Update is called once per frame
    void Update(){
        transform.LookAt(new Vector3(playerCombat.transform.position.x, transform.position.y, playerCombat.transform.position.z));

        MoveEnemy(moveDirection);
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, lookRadius);
    }

    public bool isAttackable(){
        return isAttackableBool;
    }

    public void StopMoving(){
        isMoving = false;
        moveDirection = Vector3.zero;
        if(characterController.enabled)
            characterController.Move(moveDirection);
    }

    void OnPlayerHit(EnemyBehaviour target){
        if(target == this){
            StopEnemyCoroutines();
            DamageCoroutine = StartCoroutine(HitCoroutine());

            // enemyDetection.SetCurrentTarget(null);
            isLockedTarget = false;
            // OnDamage.Invoke(this);

            health--;

            if(health <= 0){
                // Death();
                return;
            }

            animator.SetTrigger("Hit");
            transform.DOMove(transform.position - (transform.forward / 2), .3f).SetDelay(.1f);

            StopMoving();
        }

        IEnumerator HitCoroutine(){
            isStunned = true;
            yield return new WaitForSeconds(.5f);
            isStunned = false;
        }
    }

    void StopEnemyCoroutines(){
        // PrepareAttack(false);

        if (isRetreating)
        {
            if (RetreatCoroutine != null)
                StopCoroutine(RetreatCoroutine);
        }

        if (PrepareAttackCoroutine != null)
            StopCoroutine(PrepareAttackCoroutine);

        if(DamageCoroutine != null)
            StopCoroutine(DamageCoroutine);

        if (MovementCoroutine != null)
            StopCoroutine(MovementCoroutine);
    }

    void MoveEnemy(Vector3 direction){
        mvSpeed = 1;

        if (direction == Vector3.forward) {
            mvSpeed = 5;
        }

        if (direction == -Vector3.forward) {
            mvSpeed = 2;
        }

        animator.SetFloat(inputMagnitudeHash, (characterController.velocity.normalized.magnitude * direction.z) / (5 / mvSpeed), .2f, Time.deltaTime);
        animator.SetBool("Strafe", (direction == Vector3.right || direction == Vector3.left));
        animator.SetFloat(strafeDirectionHash, direction.normalized.x, .2f, Time.deltaTime);

        if(!isMoving)
            return;

        Vector3 dir = (playerCombat.transform.position - transform.position).normalized;
        Vector3 pDir = Quaternion.AngleAxis(90, Vector3.up) * dir;
        Vector3 moveDir = Vector3.zero;

        Vector3 finalDirection = Vector3.zero;

        if(direction == Vector3.forward)
            finalDirection = dir;
        if(direction == Vector3.right || direction == Vector3.left)
            finalDirection = (pDir * direction.normalized.x);
        if(direction == -Vector3.forward)
            finalDirection = -transform.forward;

        if(direction == Vector3.right || direction == Vector3.left)
            mvSpeed /= 1.5f;

        moveDir += finalDirection * mvSpeed * Time.deltaTime;

        characterController.Move(moveDir);

        // if (!isPreparingAttack);
        //     return;

        if (Vector3.Distance(transform.position, playerCombat.transform.position) < 2) {
            StopMoving();
            if (!playerCombat.isCountering && !playerCombat.isAttackingEnemy)
                Attack();
            else
                PrepareAttack(false);
        }
    }

    public void SetAttack() {
        isWaiting = false;

        PrepareAttackCoroutine = StartCoroutine(PrepAttack());

        IEnumerator PrepAttack() {
            PrepareAttack(true);
            yield return new WaitForSeconds(.2f);
            moveDirection = Vector3.forward;
            isMoving = true;
        }
    }

    void PrepareAttack(bool active) {
        isPreparingAttack = active;

        if (active) {
            // show particles to indicate to player that he can counter attack
        } else {
            StopMoving();
            // clear and stop particles for counter attack
        }
    }

    private void Attack() {
        transform.DOMove(transform.position + (transform.forward / 1), .5f);
        animator.SetTrigger("AirPunch");
    }

    public void HitEvent() {
        if(!playerCombat.isCountering && !playerCombat.isAttackingEnemy)
            playerCombat.DamageEvent();

        PrepareAttack(false);
    }

    public void SetRetreat() {
        StopEnemyCoroutines();

        RetreatCoroutine = StartCoroutine(PrepRetreat());

        IEnumerator PrepRetreat() {
            yield return new WaitForSeconds(1.4f);
            OnRetreat.Invoke(this);
            isRetreating = true;
            moveDirection = -Vector3.forward;
            isMoving = true;
            yield return new WaitUntil(() => Vector3.Distance(transform.position, playerCombat.transform.position) > 4);
            isRetreating = false;
            StopMoving();

            isWaiting = true;
            MovementCoroutine = StartCoroutine(EnemyMovement());
        }
    }

    #region Public Booleans

    public bool IsAttackable()
    {
        return health > 0;
    }

    public bool IsPreparingAttack()
    {
        return isPreparingAttack;
    }

    public bool IsRetreating()
    {
        return isRetreating;
    }

    public bool IsLockedTarget()
    {
        return isLockedTarget;
    }

    public bool IsStunned()
    {
        return isStunned;
    }

    #endregion
}
