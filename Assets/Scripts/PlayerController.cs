using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using Platformer.Gameplay;
// using static Platformer.Core.Simulation;
// using Platformer.Model;
// using Platformer.Core;


namespace Platformer.Mechanics
{
    /// <summary>
    /// This is the main class used to implement control of the player.
    /// It is a superset of the AnimationController class, but is inlined to allow for any kind of customisation.
    /// </summary>
    public class PlayerController : KinematicObject
    {
        [System.Serializable]
        public class PlatformerModel
        {            
            /// A global jump modifier applied to all initial jump velocities.            
            public float jumpModifier = 1.5f;
            
            /// A global jump modifier applied to slow down an active jump when 
            /// the user releases the jump input.            
            public float jumpDeceleration = 0.5f;
            
            /// A global maximum jump count            
            public int jumpMaxAmount = 2;
        }


        /// <summary>
        /// Max horizontal speed of the player.
        /// </summary>
        public float maxSpeed = 7;

        /// <summary>
        /// Initial jump velocity at the start of a jump.
        /// </summary>
        public float jumpTakeOffSpeed = 7;

        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;
        private int doubleJumpAmount;

        public Collider2D collider2d;
                
        public bool controlEnabled = true;

        bool jump;
        Vector2 move;
        SpriteRenderer spriteRenderer;
        internal Animator animator;
        readonly PlatformerModel model = new PlatformerModel();

        public Bounds Bounds => collider2d.bounds;

        void Awake()
        {            
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
        }

        protected override void Update()
        {
            if (controlEnabled)
            {
                move.x = Input.GetAxis("Horizontal");
                
                if (Input.GetButtonDown("Jump"))
                {
                    if (jumpState == JumpState.Grounded)
                    {
                        jumpState = JumpState.PrepareToJump;
                    }                        
                    else if (jumpState == JumpState.InFlight)
                    {                        
                        --doubleJumpAmount;
                        jumpState = JumpState.PrepareToJump;                        
                    }
                }
                else if (Input.GetButtonUp("Jump"))
                {
                    stopJump = true;                   
                }
            }
            else
            {
                move.x = 0;
            }
            UpdateJumpState();
            base.Update();
        }

        void UpdateJumpState()
        {
            jump = false;
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    jump = true;
                    stopJump = false;
                    break;
                case JumpState.Jumping:
                    if (!IsGrounded)
                    {                        
                        jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.InFlight:
                    if (IsGrounded)
                    {                        
                        jumpState = JumpState.Landed;
                    }
                    break;
                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    doubleJumpAmount = model.jumpMaxAmount;                    
                    break;
            }
        }

        protected override void ComputeVelocity()
        {
            if (jump && IsGrounded)
            {
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;                
                jump = false;
            }
            else if (jump && doubleJumpAmount > 0)
            {                
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                jump = false;
            }
            else if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y = velocity.y * model.jumpDeceleration;                    
                }
            }

            if (move.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (move.x < -0.01f)
                spriteRenderer.flipX = true;

            animator.SetBool("isGrounded", IsGrounded);                       
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);

            targetVelocity = move * maxSpeed;
        }

        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }
    }
}