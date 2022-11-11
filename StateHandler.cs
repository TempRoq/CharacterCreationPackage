using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class StateHandler : MonoBehaviour
{
    [SerializeField]
    protected int techCooldown, techTimer;
    [SerializeField]
    protected bool onWall, inFlungState, willWallBounce, willGroundBounce,
                   isGrounded, isBlocking, isControllable,
                   isDashing, negateGravity,
                   techable, willTech, canGatling;
    protected float offHitstunTimer, offLagTimer, offHitstopTimer, offDashTimer;
    public UnityEvent OnFinishHitstunTimer, OnFinishHitstopTimer, OnFinishLagTimer, OnNegateGravity, OnTouchWall, OnLeaveWall, OnLeaveGround;

    public UnityEvent OnLanding;
    protected Rigidbody2D rb2d;
    protected Character pc;

    public Collider2D LowestHitbox;
    [SerializeField]
    public Collider2D floorCheck;
    public LayerMask groundedLayerMask;
    public LayerMask wallLayerMask;

    protected int[] timers = new int[6];
    private enum TimerType
    {
        HITSTOP = 0,
        LAG,
        HITSTUN,   
        DASH,
        TECH_TIMER,
        TECH_COOLDOWN
    }

    public float wallOffsetDistance;

    public bool InLag { get { return timers[(int)TimerType.LAG] > 0; } }
    public bool InHitstun { get { return timers[(int)TimerType.HITSTUN] > 0; } }
    public bool InHitStop { get { return timers[(int)TimerType.HITSTOP] > 0; } }
    public bool InFlungState { get { return inFlungState; } set { inFlungState = value; } }
    public bool IsGrounded { get { return isGrounded && rb2d.velocity.y <= 0.0001f; } }
    public bool IsBlocking { get { return isBlocking; } set { isBlocking = value; } }
    public bool IsControllable { get { return isControllable; } }
    public bool IsDashing { get { return isDashing; } }
    public bool WillWallBounce { get { return willWallBounce; } set { willWallBounce = value; } }
    public bool WillGroundBounce { get { return willGroundBounce; } set { willGroundBounce = value; } }
    public bool WillTech { get { return willTech; } set { willTech = value; } }
    public bool Techable { get { return techable; } set { techable = value; } }
    public bool OnWall { get { return onWall; } }
    public bool CanGatling {  get { return canGatling; } set { canGatling = value; } }


    public bool NegateGravity
    {
        get { return negateGravity; }
        set
        {
            negateGravity = value; if (value == true)
            {
                OnNegateGravity.Invoke();
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        pc = GetComponent<Character>();
    }

    // Update is called once per frame

    private void FixedUpdate()
    {
        UpdatePositionalValues();
        //UpdateTimers();
        CheckTimers();
    }



    #region Give and Cancel Functionss
    public void GiveHitStop(int frames)
    {
        if (frames > 0)
        {
            timers[(int)TimerType.HITSTOP] = frames;
        }
    }

    public void GiveHitStun(int frames)
    {
        if (frames > 0)
        {
            timers[(int)TimerType.LAG] = 0;
            timers[(int)TimerType.HITSTUN] = frames;
        }
    }

    public void GiveLag(int frames)
    {
        if (frames > 0)
        {
            timers[(int)TimerType.LAG] = frames;
        }
    }

    //CANCELLING FUNCTIONS
    public void CancelHitStop()
    {
        timers[(int)TimerType.HITSTOP] = 0;
    }

    public void CancelHitStun()
    {
        timers[(int)TimerType.HITSTUN] = 0;
    }

    public void CancelLag()
    {
        timers[(int)TimerType.LAG] = 0;
    }
    #endregion

    #region Can Functions

    public bool CanAttackDummy()
    {
        return !(InHitstun || InLag || isBlocking || InHitStop) && IsGrounded;
    }
    public bool CanAttack() {
        return !(InHitstun || InLag || isBlocking || InHitStop) && isControllable;
    }

    public bool CanRekka()
    {
        return !(InHitstun || isBlocking) && isControllable;
    }

    public bool CanJump()
    {
        return !(InHitstun || isBlocking ) && isControllable; 
    }


    public bool CanBlock()
    {
        return isGrounded && !( InHitstun || InLag) && isControllable;
    }


    public bool CanMove()
    {
       return !(InHitstun|| isDashing || isBlocking || InHitStop) && isControllable;
    }

    public bool CanTech()
    {
        return techable && willTech;
    }

    #endregion
    private void OnDrawGizmos()
    {
        if (InLag)
        {
            Gizmos.color = Color.red;
            //Gizmos.DrawCube(transform.position, Vector3.one / 3);
        }

        if (InHitstun)
        {
            Gizmos.color = Color.blue;
           // Gizmos.DrawCube(transform.position + new Vector3(0, .3f), Vector3.one / 3);
        }

        else if (isBlocking)
        {
            Gizmos.color = Color.black;
            //Gizmos.DrawCube(transform.position + new Vector3(0, -.3f), Vector3.one / 3);
        }

        if (onWall)
        {
            Gizmos.color = Color.green;
            //Gizmos.DrawCube(transform.position + new Vector3(1, 0f), Vector3.one / 3);
        }


        if (isGrounded)
        {
            Gizmos.color = new Color(1, 0, 0, .5f);
            //Gizmos.DrawSphere(transform.position, 1f);
  
        }
        else
        {
            Gizmos.color = new Color(0, 0, 0, .5f); ;
            //Gizmos.DrawSphere(transform.position, 1f);

        }
        Gizmos.DrawCube(transform.position, Vector3.one * 2);

    }

    public bool WallBehindCharacter(bool facingRight)
    {

        return Physics2D.OverlapBox(LowestHitbox.bounds.center - ( (facingRight ? 1 : -1) * new Vector3(wallOffsetDistance * .5f, 0f)), LowestHitbox.bounds.extents, 0f, wallLayerMask);
    }

    public bool WallBehindCharacter(bool facingRight, out float distance)
    {
        if (Physics2D.OverlapBox(LowestHitbox.bounds.center - ((facingRight ? 1 : -1) * new Vector3(wallOffsetDistance * .5f, 0f)), LowestHitbox.bounds.extents, 0f, wallLayerMask))
        {
            distance = Mathf.Abs(transform.position.x - Physics2D.Raycast(transform.position, (new Vector2(facingRight ? 1 : -1, 0)), wallOffsetDistance, wallLayerMask).point.x);
            return true;
        }
        distance = -1;
        return false;
    }

    public void TryTech()
    {
        if (!willTech && techCooldown == 0)
        {
            willTech = true;
            techTimer = Constants.techWindowFrames;
        }

    }

    public void ResetTechCooldown()
    {
        techCooldown = 0;
    }

    public void CheckTimers()
    {
        bool willBreak = false;
        for (int i = 0; i < timers.Length; i++)
        {        
            if (timers[i] > 0)
            {   
                if (i == (int)TimerType.HITSTOP) //HITSTOP IS THE GATEKEEPER. BEFORE HITSTOP, THOSE RUN REGARDLESS. AFTER HITSTOP, THOSE DO NOT RUN REGARDLESS.
                {
                    willBreak = true;
                }
                timers[i] -= 1;
                if (timers[i] == 0)
                {
                    OffTimer((TimerType)i);
                }
            }
            if (willBreak)
            {
                break;
            }
        }
    }
    private void OffTimer(TimerType t)
    {
        switch (t)
        {
            case TimerType.LAG:
                inFlungState = false;
                OnFinishLagTimer.Invoke();
                //bodyCollider.sharedMaterial = basicMaterial;
                break;

            case TimerType.HITSTOP:
                OnFinishHitstopTimer.Invoke();
                //RestoreHitStop();
                break;

            case TimerType.HITSTUN:
                inFlungState = false;
                OnFinishHitstunTimer.Invoke();
                //bodyCollider.sharedMaterial = basicMaterial;
                break;

            case TimerType.TECH_TIMER:
                willTech = false;
                techCooldown = Constants.techCooldownFrames;
                break;

            case TimerType.DASH:
                isDashing = false;
                break;

            default:
                break;
        }
    }


    /*

    protected void UpdateTimers()
    {
        if (inHitStop && offHitstopTimer >= 0)
        {
            if (offHitstopTimer == 0)
            {
                inHitStop = false;
            }
            else
            {
                offHitstopTimer = Mathf.Clamp(offHitstopTimer - 1, 0, 1000);
                if (inHitStop && offHitstopTimer == 0)
                {
                    inHitStop = false;
                    OnFinishHitstopTimer.Invoke();
                    //RestoreHitStop();
                }
            }
        }
        if (!inHitStop)
        {
            if (inLag && offLagTimer > 0)
            {
                offLagTimer = Mathf.Clamp(offLagTimer - 1, 0, 1000);
                if (inLag && offLagTimer == 0)
                {
                    inFlungState = false;
                    inLag = false;
                    OnFinishLagTimer.Invoke();
                    //bodyCollider.sharedMaterial = basicMaterial;
                }

            }
            if (inHitstun && offHitstunTimer > 0)
            {
                offHitstunTimer = Mathf.Clamp(offHitstunTimer - 1, 0, 1000);
                if (inHitstun && offHitstunTimer == 0)
                {
                    inHitstun = false;
                    inFlungState = false;
                    OnFinishHitstunTimer.Invoke();
                    //bodyCollider.sharedMaterial = basicMaterial;
                }
            }
        }

        if (isDashing && offDashTimer > 0)
        {
            offDashTimer = Mathf.Clamp(offDashTimer - 1, 0, 1000);
            if (isDashing && offDashTimer == 0)
            {
                isDashing = false;
            }

            if (pc.GetCurrAction() != null)
            {
                negateGravity = pc.GetCurrAction().info.negateGravity;
            }
            else
            {
                negateGravity = pc.GetCurrAction();
            }

        }


        if (techCooldown > 0)
        {
            techCooldown -= 1;
        }

        if (techTimer > 0)
        {
            techTimer -= 1;
            if (techTimer == 0)
            {
                willTech = false;
                techCooldown = Constants.techCooldownFrames;
            }
        }

    }
    */

    protected void UpdatePositionalValues()
    {

        bool oldGrounded = isGrounded;


        bool b = Physics2D.OverlapBoxAll(floorCheck.bounds.center, floorCheck.bounds.extents, 0f, groundedLayerMask).Length > 0;



        /*
        if (b)
        {
            if (rb2d.velocity.y > 0)
            {
                b = false;
            }
        }
        */
        isGrounded = b;
        pc.SetAnimatorStateB("Grounded", isGrounded);


        if (!oldGrounded && isGrounded )
        {
            OnLanding.Invoke();
        }

        else if (!isGrounded && oldGrounded)
        {
            OnLeaveGround.Invoke();
        }

        if (!isGrounded)
        {


        }

        bool oldWall = onWall;

        onWall = Physics2D.OverlapBoxAll(LowestHitbox.bounds.center, LowestHitbox.bounds.extents + new Vector3(wallOffsetDistance, 0f, 0f), 0f, wallLayerMask).Length > 0;

        if (!oldWall && onWall)
        {
            OnTouchWall.Invoke();
            //print("callingOnTouchWall");
        }

        if (!onWall && oldWall)
        {
            OnLeaveWall.Invoke();
        }

    }
}
