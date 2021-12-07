using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

[System.Serializable]
public class HitEventClass : UnityEvent<EnemyBehaviour>
{
}

public class PlayerCombatSystem : MonoBehaviour
{
    private EnemyDetection enemyDetection;
    private EnemyManager enemyManager;
    private Animator animator;

    [Header("Target")]
    private EnemyBehaviour lockedTarget;

    [Header("Combat Settings")]
    [SerializeField] private float attackCooldown;

    [Header("States")]
    public bool isAttackingEnemy = false;
    public bool isCountering = false;

    // Coroutines
    private Coroutine attackCoroutine;
    private Coroutine damageCoroutine;

    [Space]

    public UnityEvent<EnemyBehaviour> OnTrajectory;
    public HitEventClass OnHit;
    public UnityEvent<EnemyBehaviour> OnCounterAttack;

    int animationCount = 0;
    string[] attacks;

    private void Start() {
        if(OnHit == null)
            OnHit = new HitEventClass();

        animator = GetComponent<Animator>();
        enemyDetection = GetComponentInChildren<EnemyDetection>();
        enemyManager = FindObjectOfType<EnemyManager>();
    }

    void AttackCheck() {
        if(isAttackingEnemy){
            return;
        }

        if(enemyDetection.getCurrentTarget() == null){
            if(enemyManager.AliveEnemyCount() == 0){
                Attack(null, 0);
                return;
            } else {
                lockedTarget = enemyManager.RandomEnemy();
            }
        }

        if(enemyDetection.InputMagnitude() > .2f)
            lockedTarget = enemyDetection.getCurrentTarget();

        if(lockedTarget == null){
            lockedTarget = enemyManager.RandomEnemy();
        }

        Attack(lockedTarget, TargetDistance(lockedTarget));
    }

    public void Attack(EnemyBehaviour target, float distance) {
        attacks = new string[] { "AirKick", "AirKick2", "AirPunch", "AirKick3" };

        if(target == null) {
            AttackType("GroundPunch", .2f, null, 0);
            return;
        }

        if (distance < 15) {
            animationCount = (int)Mathf.Repeat((float) animationCount + 1, (float) attacks.Length);
            string attackString = attacks[animationCount];
            AttackType(attackString, attackCooldown, target, .65f);
        } else {
            lockedTarget = null;
            AttackType("GroundPunch", .2f, null, 0);
        }

        // todo: add hit impulse
    }

    void AttackType(string attackTrigger, float cooldown, EnemyBehaviour target, float movementDuration) {
        animator.SetTrigger(attackTrigger);

        if(attackCoroutine != null)
            StopCoroutine(attackCoroutine);
        attackCoroutine = StartCoroutine(AttackCoroutine(cooldown)); // todo: if last hit set duration to 1.5f

        // check if is last enemy and start final blow coroutine

        if(target == null) {
            return;
        }

        target.StopMoving();
        MoveTowardsTarget(target, movementDuration);

        IEnumerator AttackCoroutine(float duration)
        {
            isAttackingEnemy = true;
            yield return new WaitForSeconds(duration);
            isAttackingEnemy = false;
            yield return new WaitForSeconds(.2f);
        }
    }

    void MoveTowardsTarget(EnemyBehaviour target, float duration){
        transform.DOLookAt(target.transform.position, .2f);
        transform.DOMove(TargetOffset(target.transform), duration);
    }

    public Vector3 TargetOffset(Transform target){
        Vector3 position;
        position = target.position;
        return Vector3.MoveTowards(position, transform.position, .95f);
    }

    float TargetDistance(EnemyBehaviour target){
        return Vector3.Distance(transform.position, target.transform.position);
    }

    public void HitEvent(){
        if(lockedTarget == null || enemyManager.AliveEnemyCount() == 0)
            return;
        
        OnHit.Invoke(lockedTarget);

        // todo: add punch particles
    }

    public void DamageEvent() {
        animator.SetTrigger("Hit");

        if(damageCoroutine != null)
            StopCoroutine(damageCoroutine);
        damageCoroutine = StartCoroutine(DamageCoroutine());

        IEnumerator DamageCoroutine() {
            yield return new WaitForSeconds(.5f);
        }
    }

    public void OnAttack(){
        AttackCheck();
    }
}
