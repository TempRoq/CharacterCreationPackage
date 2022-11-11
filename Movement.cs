using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


[RequireComponent(typeof(Rigidbody2D))]
public class Movement : MonoBehaviour
{
    Character pc;
    StateHandler sh;
    Rigidbody2D rb2d;

    

    public Vector2 lastMove = new Vector2(1, 0);

    [Header("Verticality")]
    public float terminalVelocity;
    public float ffThreshold;
    public float gravMultiplier;
    public int jumpSquatFrames = 3;
    private float gravBase;

    public float jumpSpeed;
    public float shortJumpSpeed;
    public float doubleJumpMult;

    [Header("Horizontality")]
    //Horizontal
    public float maxVelocityGround;
    public float maxVelocityAir;
    public float frictionAir;
    public float frictionAirDash;
    protected float frictionAirBase;
    public float accelerationAir;


    public float dashSpeedAir = 4;
    public float dashDurationAir;
    public float dashSpeedGround;
    public float dashDurationGround;
   


    public Action JumpAction;


    //Should dashing decelerate the player?
    // Start is called before the first frame update
    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        sh = GetComponent<StateHandler>();
        pc = GetComponent<Character>();
        lastMove.x = 1;
        gravBase = rb2d.gravityScale;
        frictionAirBase = frictionAir;
        sh.OnNegateGravity.AddListener(BeginHover);
    }

    public void BeginHover()
    {
        Vector2 hover = rb2d.velocity;
        hover.y = 0;
        rb2d.gravityScale = 0f;
        rb2d.velocity = hover;
    }

    private void FixedUpdate()
    {
        if (sh.IsGrounded && !sh.IsDashing)
        {
            RestoreAirFriction();
        }

        if (!sh.NegateGravity)
        {
            if (Mathf.Abs(rb2d.velocity.y) > ffThreshold)
            {
                rb2d.gravityScale = gravBase * gravMultiplier;
            }

            else
            {
                rb2d.gravityScale = gravBase;
            }
        }

      
    }

    public virtual void OnPressJump()
    {
         pc.PerformAction(JumpAction);
    }




    public bool MoveCharacter(float direc, ref bool facingRight)
    {
        bool moving = true;
        if (direc != 0)
        {
            lastMove.x = direc;
            if (!sh.IsGrounded)
            {

                int oppDirBonus = (int)direc == rb2d.velocity.x / Mathf.Abs(rb2d.velocity.x) ? 1 : 2;

                if (Mathf.Abs(rb2d.velocity.x) < maxVelocityAir) {
                    rb2d.velocity = new Vector2(Mathf.Clamp(rb2d.velocity.x + (direc * accelerationAir * Time.deltaTime * oppDirBonus), -maxVelocityAir, maxVelocityAir), rb2d.velocity.y);
                }
                else
                {
                    if (rb2d.velocity.x > 0 && direc < 0 || rb2d.velocity.x < 0 && direc > 0)
                    {
                        rb2d.velocity = new Vector2(rb2d.velocity.x + (direc * accelerationAir * Time.deltaTime * oppDirBonus), rb2d.velocity.y);
                    }
                }
            }
            else
            {
                rb2d.velocity = new Vector2(direc * maxVelocityGround, rb2d.velocity.y);
            }
            //facingRight = direc > 0;
        }
        else
        {
            if (sh.IsGrounded) { rb2d.velocity = new Vector2(0.0f, rb2d.velocity.y); }
            else if (Mathf.Abs(rb2d.velocity.x) > .5f)
            {
                //print("b4:rb2d.velocity.x = " + rb2d.velocity.x);
                int posNeg = rb2d.velocity.x < 0f ? -1 : 1;

                float newX = rb2d.velocity.x - ((posNeg * frictionAir) * Time.deltaTime);
                if ((newX < 0 && posNeg == 1) || (newX > 0 && posNeg == -1))
                {
                    newX = 0;
                }
                rb2d.velocity = new Vector2(newX, Mathf.Clamp(rb2d.velocity.y, -terminalVelocity, terminalVelocity));
                //print("after:rb2d.velocity.x = " + rb2d.velocity.x);

            }
            moving = false;
        }
        return moving;
    }

    public void SetupDash()
    {
        ChangeAirFriction();
        
    }

        
    public void RestoreAirFriction()
    {
        frictionAir = frictionAirBase;
    }

    public void ChangeAirFriction()
    {
        frictionAir = frictionAirDash;
    }

    public void FastFall()
    {
        if (!sh.IsGrounded && rb2d.velocity.y <= 0 && sh.CanAttack())
        {
            rb2d.velocity = new Vector2(rb2d.velocity.x, -terminalVelocity);
        }
    }
}
