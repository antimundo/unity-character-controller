using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Configuration
    [Header("Walk")]
    [SerializeField] float walkSpeed;

    [Header("Jump")]
    [SerializeField] float jumpForce;
    [SerializeField] float jumpTime;
    [SerializeField] int extraJumps;
    [SerializeField] float maxVerticalSpeed;
    [SerializeField] float jumpBufferTime;
    [SerializeField] float coyoteTime;

    [Header("Ground check")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float checkRadius;
    [SerializeField] LayerMask whatIsGround;

    [Header("Dash")]
    [SerializeField] float dashForce;
    [SerializeField] float dashDuration;
    [SerializeField] float dashCooldown;
    [SerializeField] bool freezeVerticalSpeedWhileDashing;

    // local
    int extraJumpsNow;
    float jumpTimeCount;
    bool isJumping;
    float jumpBufferCount;
    float coyoteCount;
    bool canDash = true;
    bool isDashing = false;

    // components
    SpriteRenderer sr;
    Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        extraJumpsNow = extraJumps;
    }

    private void Update()
    {
        Walk();
        Jump();
        Dash();

        LimitVerticalSpeed();
    }

    void LimitVerticalSpeed()
    {
        Vector2 currentVelocity = rb.velocity;

        if (rb.velocity.magnitude > maxVerticalSpeed)
            rb.velocity = rb.velocity.normalized * maxVerticalSpeed;

        currentVelocity.y = rb.velocity.y;
        rb.velocity = currentVelocity;
    }

    // Check if object is touching ground
    bool isGrounded() =>
        Physics2D.OverlapCircle(
            groundCheck.position,
            checkRadius,
            whatIsGround);

    void Walk()
    {
        // You can use Input.GetAxis to get a less snappy movement
        float walkInput = Input.GetAxisRaw("Horizontal");

        rb.position += new Vector2(walkInput * walkSpeed * Time.deltaTime, 0);

        // Makes the sprite look towards the movement direction
        if (walkInput != 0) sr.flipX = walkInput < 0;
    }

    void Jump()
    {
        JumpBuffer();
        CoyoteTime();

        if (jumpBufferCount >= 0 && coyoteCount > 0f)
        {
            isJumping = true;
            jumpTimeCount = jumpTime;
            rb.velocity = Vector2.up * jumpForce;
            jumpBufferCount = 0;
        }
        else if (Input.GetButton("Jump") && isJumping)
        {
            if (jumpTimeCount > 0)
            {
                rb.velocity = Vector2.up * jumpForce;
                jumpTimeCount -= Time.deltaTime;
            }
            else isJumping = false;
        }
        else if (Input.GetButtonDown("Jump") && extraJumpsNow > 0)
        {
            isJumping = true;
            jumpTimeCount = jumpTime;
            rb.velocity = Vector2.up * jumpForce;
            extraJumpsNow--;
        }

        if (Input.GetButtonUp("Jump")) isJumping = false;

        if (isGrounded()) extraJumpsNow = extraJumps;
    }

    /// Little margin to perform a jump after leaving the ground
    void CoyoteTime()
    {
        if (isGrounded())
            coyoteCount = coyoteTime;
        else
            coyoteCount -= Time.deltaTime;
    }

    /// Little margin to perform a jump before the ground is touched
    void JumpBuffer()
    {
        if (Input.GetButtonDown("Jump"))
            jumpBufferCount = jumpBufferTime;
        else
            jumpBufferCount -= Time.deltaTime;
    }

    /// Small horizontal jump
    void Dash()
    {
        if (Input.GetButtonDown("Dash") && canDash)
            StartCoroutine(performDash());

        if (isDashing)
            if (sr.flipX)
                rb.position += new Vector2(-dashForce * Time.deltaTime, 0);
            else
                rb.position += new Vector2(dashForce * Time.deltaTime, 0);
            
    }

    /// Specifies when the object needs to start or stop dashing
    IEnumerator performDash()
    {
        isDashing = true;
        canDash = false;
        
        float gravityBefore = rb.gravityScale;

        if (freezeVerticalSpeedWhileDashing)
        {
            rb.gravityScale = 0;
            rb.velocity = new Vector2(rb.velocity.x, 0);
        }

        yield return new WaitForSeconds(dashDuration);
        isDashing = false;
        rb.gravityScale = gravityBefore;

        yield return new WaitForSeconds(dashCooldown - dashDuration);
        canDash = true;
    }

}
