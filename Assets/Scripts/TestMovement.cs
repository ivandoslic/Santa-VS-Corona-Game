using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: THIS CODE NEEDS SO MUCH FUCKING OPTIMIZATION BECAUSE I SUCK!

public class TestMovement : MonoBehaviour
{
    Vector3 direction;
    Camera cam;
    bool inAir = false;

    [Header("Joystick Settings")]
    public Joystick joystick;
    public float joystickOffsetTolerance;
    [Space(10)]

    [Header("Movement Settings")]
    public CharacterController controller;
    public float movementSpeed;

    public float turnSmoothTime;
    public float turnSmoothVelocity;

    float initialJumpVelocity;
    public float maxJumpHeight = 4.0f;
    public float maxJumpDuration = 0.75f;
    public float groundedGravity = 0.5f;
    public float gravity = 9.8f;

    [Header("Animation Settings")]
    public Animator animator;
    int isRunningHash;
    int isJumpingHash;
    int jumpCountHash;
    int fallingHash;
    int isOnLadderHash;
    int isClimbingLadderHash;
    int isExitingLadderHash;
    int isExitingMidHangHash;
    int isInCombatHash;
    bool isJumpAnimating;
    int jumpCount = 0;
    Dictionary<int, float> initialJumpVelocities = new Dictionary<int, float>();
    Dictionary<int, float> jumpGravities = new Dictionary<int, float>();
    Coroutine currentJumpResetRoutine = null;

    [Header("Advanced Movement")]
    public float climbingSpeed = 2f;
    bool isClimbingLadder = false;
    bool isOnTopOfLadders = false;
    bool isOnBottomOfLadders = false;

    [Header("Fighting Settings")]
    public bool isInCombat = false;

    // Other
    [SerializeField]
    private CapsuleCollider combatCollider;

    private void Awake() {
        isRunningHash = Animator.StringToHash("isRunning");
        isJumpingHash = Animator.StringToHash("isJumping");
        jumpCountHash = Animator.StringToHash("jumpCount");
        fallingHash = Animator.StringToHash("isFalling");
        isOnLadderHash = Animator.StringToHash("isOnLadder");
        isClimbingLadderHash = Animator.StringToHash("isClimbingLadder");
        isExitingLadderHash = Animator.StringToHash("isExitingLadder");
        isExitingMidHangHash = Animator.StringToHash("isExitingMidHang");
        isInCombatHash = Animator.StringToHash("isInCombat");

        setupJumpVariables();
    }

    private void Start() {
        cam = Camera.main;
    }

    void Update()
    {
        bool isFalling = direction.y <= -0.1f;
        float fallMultiplier = 2.0f;

        if(isInCombat){
            controller.enabled = false;
            combatCollider.enabled = true;
            animator.SetBool(isInCombatHash, true);
            return;
        } else {
            animator.SetBool(isInCombatHash, false);
            controller.enabled = true;
            combatCollider.enabled = false;
        }

        if(animator.GetBool(isExitingLadderHash)){
            return;
        }

        if(isClimbingLadder) {
            ladderClimbingBehaviour();
            return;
        }

        if(isFalling) {
            animator.SetBool(fallingHash, true);
            float previousYVelocity = direction.y;
            float newYVelocity = direction.y + (jumpGravities[jumpCount] * fallMultiplier * Time.deltaTime);
            float nextYVelocity = (previousYVelocity + newYVelocity) * .5f;
            direction.y = nextYVelocity;
            controller.Move(direction * Time.deltaTime);
        } else if(!controller.isGrounded && inAir) {
            direction.y += jumpGravities[jumpCount] * Time.deltaTime;
            controller.Move(direction * Time.deltaTime);
        }

        if(controller.isGrounded) {
            animator.SetBool(fallingHash, false);
            if(isJumpAnimating) {
                animator.SetBool(isJumpingHash, false);
                isJumpAnimating = false;
                inAir = false;
                currentJumpResetRoutine = StartCoroutine(jumpResetRoutine());
                if (jumpCount == 3) {
                    jumpCount = 0;
                    animator.SetInteger(jumpCountHash, jumpCount);
                }
                direction.y = -groundedGravity;
            }
            HandleMovement();
        }
        
    }

    void HandleMovement() {
        float horizontalAmount = joystick.Horizontal;
        float verticalAmount = joystick.Vertical;

        if(Mathf.Abs(horizontalAmount) > joystickOffsetTolerance) {
            if(horizontalAmount < 0) {
                horizontalAmount = -1f;
            } else {
                horizontalAmount = 1f;
            }
        }

        if(Mathf.Abs(verticalAmount) > joystickOffsetTolerance) {
            if(verticalAmount < 0) {
                verticalAmount = -1f;
            } else {
                verticalAmount = 1f;
            }
        }

        direction = new Vector3(horizontalAmount, 0, verticalAmount).normalized;

        if(direction.magnitude >= joystickOffsetTolerance) {

            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            animator.SetBool(isRunningHash, true);
            direction.y = -groundedGravity;

            controller.Move(direction * movementSpeed * Time.deltaTime);
        } else {
            animator.SetBool(isRunningHash, false);
        }
    }

    public void HandleJump(){
        if(controller.isGrounded) {
            if(jumpCount < 3 && currentJumpResetRoutine != null) {
                StopCoroutine(currentJumpResetRoutine);
            }

            inAir = true;
            isJumpAnimating = true;
            jumpCount += 1;
            animator.SetInteger(jumpCountHash, jumpCount);

            direction.y = initialJumpVelocities[jumpCount] * .75f;
            animator.SetBool(isJumpingHash, true);
            
            controller.Move(direction * Time.deltaTime);
        }
    }

    IEnumerator jumpResetRoutine() {
        yield return new WaitForSeconds(1f);
        jumpCount = 0;
    }

    void setupJumpVariables() {
        float timeToApex = maxJumpDuration / 2;
        // gravity = (-2 * maxJumpHeight / Mathf.Pow(timeToApex, 2));
        gravity = -11.8f;
        initialJumpVelocity = (2 * maxJumpHeight + 0.5f) / timeToApex;

        // float secondJumpGravity = (-2 * (maxJumpHeight + 2)) / Mathf.Pow((timeToApex * 1.25f), 2);
        float secondJumpGravity = gravity * 2.35f;
        float secondJumpInitialVelocity = (2 * (maxJumpHeight + 2)) / (timeToApex * 1.25f);

        // float thirdJumpGravity = (-2 * (maxJumpHeight + 4)) / Mathf.Pow((timeToApex * 1.5f), 2);
        float thirdJumpGravity = gravity * 2.82f;
        float thirdJumpInitialVelocity = (2 * (maxJumpHeight + 3)) / (timeToApex * 1.5f);

        initialJumpVelocities.Add(0, initialJumpVelocity);
        initialJumpVelocities.Add(1, initialJumpVelocity);
        initialJumpVelocities.Add(2, secondJumpInitialVelocity);
        initialJumpVelocities.Add(3, thirdJumpInitialVelocity);

        jumpGravities.Add(0, gravity);
        jumpGravities.Add(1, gravity);
        jumpGravities.Add(2, secondJumpGravity);
        jumpGravities.Add(3, thirdJumpGravity);
    }

    // On initializing the ladder climbing player should recognize the ladder object and match his rotation with the ladders rotation
    // That rotation is only on the y axis so it can't be that hard. Right?
    public void handleLadderClimb(GameObject ladders) {
        isClimbingLadder = true;
        controller.enabled = false;
        animator.SetBool(isOnLadderHash, true);
    }

    void handleLadderExit() {
        animator.SetBool(isExitingLadderHash, true);
        animator.SetBool(isClimbingLadderHash, false);
        animator.SetBool(isOnLadderHash, false);
        isClimbingLadder = false;
        isOnTopOfLadders = false;
        isOnBottomOfLadders = false;
        transform.Translate(transform.right * 0.6f + transform.up * 0.7f);
        controller.enabled = true;
    }

    public void onLadderExited() {
        animator.SetBool(isExitingLadderHash, false);
        animator.SetBool(isExitingMidHangHash, false);
        isClimbingLadder = false;
    }

    public void ladderClimbingBehaviour() {
        float verticalAmount = joystick.Vertical;
        
        if(Mathf.Abs(verticalAmount) > joystickOffsetTolerance) {
            if(verticalAmount > 0 && !isOnTopOfLadders) {
                transform.Translate(transform.up * climbingSpeed * Time.deltaTime);
                animator.SetBool(isClimbingLadderHash, true);
            } else if(!isOnBottomOfLadders) {
                transform.Translate(-transform.up * climbingSpeed * Time.deltaTime);
                animator.SetBool(isClimbingLadderHash, true);
            }

            if(isOnBottomOfLadders) {
                animator.SetBool(isClimbingLadderHash, false);
            }

            if(isOnTopOfLadders) {
                handleLadderExit();
            }

        } else {
            animator.SetBool(isClimbingLadderHash, false);
        }
    }

    public void leaveLadders() {
        animator.SetBool(isExitingMidHangHash, true);
        animator.SetBool(isClimbingLadderHash, false);
        animator.SetBool(isOnLadderHash, false);
        isClimbingLadder = false;
        isOnTopOfLadders = false;
        isOnBottomOfLadders = false;
        controller.enabled = true;
    }

    public void reachedTopOfLadders() {
        if(isClimbingLadder) {
            isOnTopOfLadders = true;
        }
    }

    public void leftTopOfLadders() {
        if(isClimbingLadder) {
            isOnTopOfLadders = false;
        }
    }

    public void reachedBottomOfLadders() {
        if(isClimbingLadder) {
            isOnBottomOfLadders = true;
        }
    }

    public void leftBottomOfLadders() {
        if(isClimbingLadder) {
            isOnBottomOfLadders = false;
        }
    }
}
