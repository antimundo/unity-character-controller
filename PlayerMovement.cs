using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float movementSpeed;
    [SerializeField] float maxVerticalSpeed;
    // Move particle
    [SerializeField] ParticleSystem walkParticle;

    [Header("Jump")]
    [SerializeField] float jumpForce;
    // Jump time
    [SerializeField] float jumpTime;
    float jumpTimeCounter;
    bool isJumping;
    // Extra jumps
    [SerializeField] int extraJumps;
    int extraJumpsNow;
    // Jump Buffer
    [SerializeField] float jumpBufferLength;
    float jumpBufferCount;
    // Coyote Time
    [SerializeField] float coyoteTime;
    float coyoteCounter;
    // Jump particle
    [SerializeField] ParticleSystem jumpParticle;

    [Header("Ground check")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float checkRadius;
    [SerializeField] LayerMask whatIsGround;
    bool isGrounded;

    [Header("Dash")]
    [SerializeField] float dashForce;
    [SerializeField] float dashDuration;
    [SerializeField] float dashCooldown;
    [SerializeField] bool resetSpeedY;
    float gravityBefore;

    // dash coroutine
    bool canDash = true;
    bool isDashing = false;
    readonly Coroutine dashCoroutine;
    [SerializeField] ParticleSystem dashParticle;

    // components
    [SerializeField] SpriteRenderer sr;
    Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        extraJumpsNow = extraJumps;
        gravityBefore = rb.gravityScale;
    }

    private void Update()
    {
        JumpBuffer();
        CoyoteTime();
        Jump();
        Dash();
        LimitVerticalSpeed();
    }
    
    void FixedUpdate()
    {
        Walk();
        PerformDash();
    }

    void LimitVerticalSpeed()
    {
        Vector3 currentVelocity = rb.velocity;

        if (rb.velocity.magnitude > maxVerticalSpeed)
            rb.velocity = rb.velocity.normalized * maxVerticalSpeed;

        currentVelocity.y = rb.velocity.y;
        rb.velocity = currentVelocity;
    }

    void Walk()
    {
        // Check if object is touching ground
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            checkRadius,
            whatIsGround);

        // Use Input.GetAxis to get a less snappy movement
        float moveInput = Input.GetAxisRaw("Horizontal");

        // Applies the force
        rb.velocity = new Vector2(moveInput * movementSpeed, rb.velocity.y);

        FlipSprite(moveInput);
    }

    /// <summary>
    /// Flips the sprite to face the moving direction
    /// </summary>
    void FlipSprite(float moveInput)
    {
        if (moveInput > 0)
            sr.flipX = false;
        else if (moveInput < 0)
            sr.flipX = true;
        
        // flip dash particle
        if (moveInput > 0)
            dashParticle.transform.eulerAngles = new Vector3(0,0,0);
        else if (moveInput < 0)
            dashParticle.transform.eulerAngles = new Vector3(0,180,0);
    }

    void Jump()
    {
        if (jumpBufferCount >= 0 && coyoteCounter > 0f)
        {
            isJumping = true;
            jumpTimeCounter = jumpTime;  
            rb.velocity = Vector2.up * jumpForce;
            jumpBufferCount = 0;
        }
        else if (Input.GetButton("Jump") && isJumping)
        {
            if (jumpTimeCounter > 0)
            {
                rb.velocity = Vector2.up * jumpForce;
                jumpTimeCounter -= Time.deltaTime;
            }
            else isJumping = false;
        }
        else if (Input.GetButtonDown("Jump") && extraJumpsNow > 0)
        {
            isJumping = true;
            jumpTimeCounter = jumpTime;
            rb.velocity = Vector2.up * jumpForce;
            extraJumpsNow--;
            jumpParticle.Play();
        }

        if (Input.GetButtonUp("Jump")) isJumping = false;

        if (isGrounded) extraJumpsNow = extraJumps;
    }

    /// <summary>
    /// Little margin to perform a jump after leaving the ground
    /// </summary>
    void CoyoteTime()
    {
        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;
    }

    /// <summary>
    /// Little margin to perform a jump before the ground is touched
    /// </summary>
    void JumpBuffer()
    {
        if (Input.GetButtonDown("Jump"))
            jumpBufferCount = jumpBufferLength;
        else
            jumpBufferCount -= Time.deltaTime;
    }

    /// <summary>
    /// Small horizontal jump
    /// </summary>
    void Dash()
    {
        if (Input.GetButtonDown("Dash") && canDash) {
            if (dashCoroutine != null)
                StopCoroutine(dashCoroutine);

            StartCoroutine(CheckIfDashing());

            // Visual effects
            dashParticle.Play();
            CinemachineShake.Instance.ShakeCamera(5f, 0.2f);
        }
    }

    /// <summary>
    /// Specifies when the object needs to start or stop dashing
    /// </summary>
    IEnumerator CheckIfDashing()
    {
        isDashing = true;
        canDash = false;
        
        yield return new WaitForSeconds(dashDuration);
        isDashing = false;
        rb.gravityScale = gravityBefore;

        yield return new WaitForSeconds(dashCooldown - dashDuration);
        canDash = true;
    }

    /// <summary>
    /// Applies the forces of the Dash if you are dashing
    /// </summary>
    void PerformDash()
    {
        if (isDashing)
        {
            if (sr.flipX)
                rb.AddForce(Vector2.left * dashForce);
            else
                rb.AddForce(Vector2.right * dashForce);
            if (resetSpeedY)
            {
                rb.gravityScale = 0;
                rb.velocity = new Vector2(rb.velocity.x , 0);
            }
        }
    }
}