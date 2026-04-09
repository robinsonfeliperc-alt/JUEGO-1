using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 7f;

    [Header("Salto")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private int maxJumps = 2;

    [Header("Detección de suelo")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Feel / Física")]
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    // ── Referencias ───────────────────────────────────────────────────────
    private Rigidbody2D rb;
    private Animator animator;
    private bool hasAnimator;

    // ── Estado ────────────────────────────────────────────────────────────
    private Vector2 moveInput;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool isGrounded;
    private int jumpsRemaining;
    private bool isFacingRight = true;

    // ── Hashes de Animator (los 3 parámetros del state machine) ──────────
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int AnimJump = Animator.StringToHash("Jump");

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hasAnimator = TryGetComponent(out animator);
    }

    private void Update()
    {
        CheckGround();
        HandleJump();
        FlipSprite();
        UpdateAnimator();   // sincroniza Speed, IsGrounded y dispara Jump
        jumpPressed = false;
    }

    private void FixedUpdate()
    {
        Move();
        ApplyBetterJumpPhysics();
    }

    // ── Callbacks del New Input System ────────────────────────────────────
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        jumpHeld = value.isPressed;
        if (value.isPressed) jumpPressed = true;
    }

    // ── Movimiento horizontal ─────────────────────────────────────────────
    private void Move()
    {
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
    }

    // ── Detección de suelo ────────────────────────────────────────────────
    private void CheckGround()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (!wasGrounded && isGrounded)
            jumpsRemaining = maxJumps;
    }

    // ── Salto ─────────────────────────────────────────────────────────────
    private void HandleJump()
    {
        if (jumpPressed && jumpsRemaining > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpsRemaining--;
            // El Trigger "Jump" se setea en UpdateAnimator() junto al resto
        }

        if (!isGrounded && jumpsRemaining == maxJumps)
            jumpsRemaining = maxJumps - 1;
    }

    // ── Física de salto mejorada ──────────────────────────────────────────
    private void ApplyBetterJumpPhysics()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y
                                 * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !jumpHeld)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y
                                 * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    // ── Voltear sprite ────────────────────────────────────────────────────
    private void FlipSprite()
    {
        if ((moveInput.x > 0 && !isFacingRight) ||
            (moveInput.x < 0 && isFacingRight))
        {
            isFacingRight = !isFacingRight;
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }

    // ── Animator — punto único de sincronización ──────────────────────────
    private void UpdateAnimator()
    {
        if (!hasAnimator) return;

        // Float Speed → conduce las transiciones Idle ↔ Run
        animator.SetFloat(AnimSpeed, Mathf.Abs(moveInput.x));

        // Bool IsGrounded → cierra las transiciones Jump → Idle / Run
        animator.SetBool(AnimIsGrounded, isGrounded);

        // Trigger Jump → abre las transiciones * → Jump (solo el frame que se salta)
        if (jumpPressed && jumpsRemaining < maxJumps) // jumpsRemaining ya fue decrementado
            animator.SetTrigger(AnimJump);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}