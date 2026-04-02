using UnityEngine;

/// <summary>
/// Visual/animation driver for the player. Keeps gameplay logic out of PlayerMove.
/// </summary>
[RequireComponent(typeof(PlayerMove))]
[DisallowMultipleComponent]
public class PlayerVisual : MonoBehaviour
{
    [SerializeField] private PlayerMove player;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    private static readonly int IsJumpingId = Animator.StringToHash("IsJumping");
    private static readonly int IsFallingId = Animator.StringToHash("IsFalling");

    private void Awake()
    {
        if (player == null)
            player = GetComponent<PlayerMove>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void LateUpdate()
    {
        if (player == null)
            return;

        UpdateFlip();
        UpdateAnimator();
    }

    private void UpdateFlip()
    {
        if (spriteRenderer == null)
            return;

        Vector2 moveDir = player.EdgeMoveDirection;
        if (moveDir.sqrMagnitude < 0.0001f)
            return;

        float dot = Vector3.Dot(transform.right, (Vector3)moveDir);
        spriteRenderer.flipX = dot < 0f;
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        animator.SetBool(IsJumpingId, player.IsJumping);
        animator.SetBool(IsFallingId, player.IsFalling);
    }
}

