using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sample controller for animation preview only.
/// This script is intended for demo/testing purposes,
/// not for production gameplay use.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class AnimeController_Sample : MonoBehaviour
{
    public float animSpeed = 1.5f;
    public float forwardSpeed = 7.0f;
    public float backwardSpeed = 2.0f;
    public float rotateSpeed = 2.0f;
    public float jumpPower = 3.0f;

    private CapsuleCollider col;
    private Rigidbody rb;
    private Animator anim;
    private AnimatorStateInfo currentBaseState;
    private GameObject cameraObject;

    private Vector3 velocity;
    private float orgColHight;
    private Vector3 orgVectColCenter;

    // ===== Input buffer (New Input System best practice) =====
    private float inputH;
    private float inputV;
    private bool jumpPressed;

    // ===== Animator States =====
    static readonly int idle_cState = Animator.StringToHash("Base Layer.Idle_C");
    static readonly int idle_aState = Animator.StringToHash("Base Layer.Idle_A");
    static readonly int locoState   = Animator.StringToHash("Base Layer.Locomotion");
    static readonly int jumpState   = Animator.StringToHash("Base Layer.Jump");
    static readonly int cute1State  = Animator.StringToHash("Base Layer.Cute1");

    void Start()
    {
        anim = GetComponent<Animator>();
        col  = GetComponent<CapsuleCollider>();
        rb   = GetComponent<Rigidbody>();

        cameraObject = GameObject.FindWithTag("MainCamera");

        orgColHight = col.height;
        orgVectColCenter = col.center;
    }

    // ===== Read input in Update() =====
    void Update()
    {
        if (Keyboard.current == null)
            return;

        inputH = 0f;
        inputV = 0f;

        // Move (WASD / Arrow Keys)
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            inputV += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            inputV -= 1f;

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            inputH += 1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            inputH -= 1f;

        // Jump / Cute (Space)
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpPressed = true;
    }

    void FixedUpdate()
    {
        // Animator parameters
        anim.SetFloat("Speed", inputV);
        anim.SetFloat("Direction", inputH);
        anim.speed = animSpeed;

        currentBaseState = anim.GetCurrentAnimatorStateInfo(0);
        rb.useGravity = true;

        // Movement
        velocity = new Vector3(0, 0, inputV);
        velocity = transform.TransformDirection(velocity);

        if (inputV > 0.1f)
            velocity *= forwardSpeed;
        else if (inputV < -0.1f)
            velocity *= backwardSpeed;

        // Jump while running
        if (jumpPressed && currentBaseState.nameHash == locoState && !anim.IsInTransition(0))
        {
            rb.AddForce(Vector3.up * jumpPower, ForceMode.VelocityChange);
            anim.SetBool("Jump", true);
            jumpPressed = false;
        }

        transform.localPosition += velocity * Time.fixedDeltaTime;
        transform.Rotate(0, inputH * rotateSpeed, 0);

        // Jump State
        if (currentBaseState.nameHash == jumpState)
        {
            cameraObject?.SendMessage(
                "setCameraPositionJumpView",
                SendMessageOptions.DontRequireReceiver
            );

            if (!anim.IsInTransition(0))
            {
                anim.SetBool("Jump", false);
            }
        }
        // Idle States
        else if (currentBaseState.nameHash == idle_cState ||
                 currentBaseState.nameHash == idle_aState)
        {
            if (jumpPressed)
            {
                anim.SetBool("Cute1", true);
                jumpPressed = false;
            }
        }
        // Cute State
        else if (currentBaseState.nameHash == cute1State)
        {
            if (!anim.IsInTransition(0))
                anim.SetBool("Cute1", false);
        }
    }

    // ===== Sample Control UI (Demo Only) =====
    void OnGUI()
    {
        const int width = 260;
        const int height = 140;
        int x = Screen.width - width - 10;
        int y = 10;

        GUI.Box(new Rect(x, y, width, height), "Controls (Sample)");

        GUI.Label(new Rect(x + 15, y + 30, width, 20),
            "W / A / S / D or Arrow Keys : Move");

        GUI.Label(new Rect(x + 15, y + 50, width, 20),
            "Space : Jump / Cute Action");

        GUI.Label(new Rect(x + 15, y + 70, width, 20),
            "Left Ctrl : Front Camera");

        GUI.Label(new Rect(x + 15, y + 90, width, 20),
            "Left Alt : Look-at Camera");
    }
}
