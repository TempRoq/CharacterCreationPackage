using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
[RequireComponent(typeof(HitboxReceiver))]
[RequireComponent(typeof(ActionHandler))]
[RequireComponent(typeof(Movement))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(StateHandler))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CharacterControl))]
public class Character : MonoBehaviour
{
    public float CurrMeter { get { return ct.currMeter; } }


    protected StateHandler sh;
    public StateHandler CharacterStateHandler { get { return sh; } }
    protected HitboxReceiver hbr;
    protected ActionHandler ac;
    protected Movement m;
    protected Rigidbody2D rb2d;
    protected SpriteRenderer sr;
    protected AudioSource ads;
    protected CharacterControl cc;
    public CharacterTeam ct;
    [HideInInspector]
    public Animator ani;
    [Header("Trackables")]

    public int currentStance = 0;
    public Stance[] stances;

    [Header("Stats")]
    public int currentHealth;
    public int maxHealth;
    public bool facingRight;
    public float landingLag;
    public int bounceLag;

    [Header("Colliders")]
    [SerializeField]
    protected Collider2D bodyCollider;
    [SerializeField]
    protected PushBox pushBox;

    [Header("Actions and Combos")]
    public Action DashAction;
    public bool movementCancel = true;
    public bool specialToSpecialCancel = true;
    public bool reverseBeat = true;


    [Header("Miscellaneous")]
    public bool disappearOnDeath;

    [Header("Gravity")]
    public float attackGravity;
    public float HitstunGravScale;
    public float HitstunGravThresh;



    [Header("PRORATION")]
    [Range(0, 20)]
    public float percentageLostPer;
    [Range(0, 5)]
    public int movesPerProrationStep;
    [Range(0, 100)]
    public int minDamagePercentage;


    [Header("Meter Costs")]
    public int movementCancelCost = 100;
    public int specialToSpecialCost = 100;
    public int reverseBeatCost = 100;

    [Header("Extras")]
    public int startingSpawnInstanceNum = 3;
    protected float incHealthTimerMax = 1.5f;
    protected float incHealthTimer = 0f;
    public GameObject[] Spawnables;
   


    //Stuff not in inspector
    private List<List<GameObject>> spawnableInstances;
    protected int flinchAnim = 0;
    protected Vector3 originalScale;

    [HideInInspector]
    public Vector2 StickInput { get { return stickInput; } }
    [HideInInspector]
    public UnityEvent OnChangeHealth, OnChangeMeter;
    //MOTION INPUTS!
    protected List<MyEnums.Motions> motionInputs = new List<MyEnums.Motions>();
    protected List<MyEnums.StickDirection> directionBuffer = new List<MyEnums.StickDirection>();
    protected static readonly int directionBufferMaxLength = 9;
    protected static readonly int motionInputBufferFrames = 7;
    protected int motionInputBufferFrameCount = 0;
    public int comboHit = 0;



    public float VelocityGroundMax { get { return m.maxVelocityGround; } }
    public float VelocityAirMax { get { return m.maxVelocityAir; } }
    public float VelocityJumpFull { get { return m.jumpSpeed; } }
    public float VelocityJumpShort { get { return m.shortJumpSpeed; } }
    public float DoubleJumpMultiplier { get { return m.doubleJumpMult; } }
    public float DashSpeedGround { get { return m.dashSpeedGround; } }
    public float DashSpeedAir { get { return m.dashSpeedAir; } }
    public bool InStartup { get { return ac.InStartup; } }
    public bool MidAction { get { return ac.performingAction; } }
    public bool InEndLag { get { return sh.InLag && !ac.performingAction; } }

    public bool[] startedButtons;
    public bool[] downButtons;
    [HideInInspector]
    protected Vector2 stickInput;

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    //Variables not for inspector
    protected Vector2 velocityBeforeHitstop;
    /// <summary>
    /// 
    /// Start, update, and FIxedUpdate
    /// 
    /// 
    /// </summary>

    protected virtual void Start()
    {
        sh = GetComponent<StateHandler>();
        hbr = GetComponent<HitboxReceiver>();
        ac = GetComponent<ActionHandler>();
        m = GetComponent<Movement>();
        rb2d = GetComponent<Rigidbody2D>();
        ani = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        ads = GetComponent<AudioSource>();
        pushBox = GetComponentInChildren<PushBox>();
        cc = GetComponent<CharacterControl>();


        hbr.OnHit.AddListener(TakeHit);

        sh.OnFinishHitstopTimer.AddListener(RestoreHitStop);
        sh.OnLanding.AddListener(CheckLanding);
        sh.OnTouchWall.AddListener(CheckWallTouch);

        originalScale = transform.localScale;



        ac.OnFinishAction.AddListener(RestoreGravity);
        ac.OnFinishAction.AddListener(SetFinishActionTrigger);
        sh.OnFinishHitstunTimer.AddListener(SetHitstunTriggerF);
        sh.OnFinishHitstunTimer.AddListener(ActivatePushBoxHitstun);
        sh.OnLeaveGround.AddListener(HidePushBox);
        sh.OnLanding.AddListener(ActivatePushBox);
        ChangeStance(currentStance);
        ac.OnStartAction.AddListener(DoAction);
        sh.OnLeaveGround.AddListener(ac.TrackAirUses);
        sh.OnLanding.AddListener(ac.StopTrackAirUses);
        //  sh.OnLanding.AddListener(ApplyLandingLag);

        spawnableInstances = new List<List<GameObject>>();
        for (int i = 0; i < Spawnables.Length; i++)
        {
            List<GameObject> gList = new List<GameObject>();
            for (int j = 0; j < startingSpawnInstanceNum; j++)
            {
                GameObject g = Instantiate(Spawnables[i]);
                g.GetComponent<ActionEmitter>().targetsToHit = ac.targetsToHit;
                g.GetComponent<Projectile>().DeactivateProjectile();
                gList.Add(g);
            }
            spawnableInstances.Add(gList);
        }

        OnChangeHealth.AddListener(UpdateHealthBar);
        OnChangeMeter.AddListener(UpdateSuperBar);


        directionBuffer.Add(MyEnums.StickDirection.NEUTRAL);
    }

    private void Awake()
    {
        startedButtons = new bool[9];
        downButtons = new bool[9];
        //UpdateSuperBar();
    }

    protected virtual void Update()
    {
        CheckInputs();
    }

    protected virtual void FixedUpdate()
    {
        TryRegenHealth();
        transform.localScale = Vector3.Scale(originalScale, new Vector3(facingRight ? 1 : -1, 1, 1));
        startedButtons = cc.StartedButtons;
        downButtons = cc.DownButtons;
        stickInput = cc.StickInput;

        //base.FixedUpdate();



        motionInputBufferFrameCount += 1;
        if (motionInputBufferFrameCount >= motionInputBufferFrames)
        {
            MyEnums.StickDirection sd = (directionBuffer.Count == 0 ? MyEnums.StickDirection.NEUTRAL : directionBuffer[^1]);
            directionBuffer.Clear();
            AddToDirectionBuffer(sd);
            motionInputBufferFrameCount = 0;
        }

        if (sh.IsControllable) //if Controllable, 
        {
            CheckInputArrays();
            if (FullCanMove())
            {
                bool b = (m.MoveCharacter(StickInput.x, ref facingRight));
                ani.SetBool("Moving", b);
            }
            else
            {
                ani.SetBool("Moving", false);
            }
        }

        else
        {
            // if (!sh.InLag || (sh.InLag && ac.performingAction && ac.GetCurrentAction().canMoveDuring)) //Either not in lag, or in lag, is performing an action,
            if (!(sh.InHitstun || sh.IsDashing || sh.IsBlocking))
            {
                m.MoveCharacter(0, ref facingRight); //For Dummy Behavior

            }
        }
        ani.SetFloat("xVelocity", rb2d.velocity.x);
        ani.SetFloat("yVelocity", rb2d.velocity.y);

        //cc.FrameUpdate();

    }

    #region HUD
    protected void UpdateHealthBar()
    {
        ct.UpdateHealthBar();
        // UIController.instance.UpdateHealthbar(UISide, currentHealth, maxHealth);
    }
    protected void UpdateSuperBar()
    {
        ct.UpdateSuperBar();
        // UIController.instance.UpdateSuperBar(UISide, currMeter, maxMeter);
    }
    #endregion


    private void ApplyLandingLag()
    {
        if ((ac.performingAction || ac.InEndLag) && ac.GetCurrentAction().info.endOnLanding)
        {
            sh.GiveLag(ac.GetCurrentAction().landingLag);
        }
    }

    protected int ApplyProration(int damageNum)
    {
        int steps = Mathf.FloorToInt(comboHit / movesPerProrationStep);
        float percentage = Mathf.Max(100 - (steps * percentageLostPer), minDamagePercentage) / 100f;
        return (int)Mathf.Max(damageNum * percentage, 1);
    }


    public void SetAnimatorStateB(string name, bool b)
    {
        ani.SetBool(name, b);
    }
    public virtual void RestoreGravity()
    {
        if (ac.GetCurrentAction().info.restoreGravityAfter)
        {
            //print("RestoringGravity");
            sh.NegateGravity = false;
        }
    }
    public virtual void TakeHit()
    {
        StartCoroutine(TakeHitEF());
    }
    public virtual IEnumerator TakeHitEF()
    {

        //originOfAttack refers to the point where the hitbox cluster calls the "center" of the attack

        yield return new WaitForEndOfFrame();
        sh.ResetTechCooldown();
        HitboxReceiver.AttackerInfo currentInfo = hbr.HitInfoReceived;
        bool successfulBlock = CheckIfBlocking(currentInfo.attackCoreX);
        //Camera Shake
        StartCoroutine(CameraFollow.instance.Shake((successfulBlock ? currentInfo.justHitBy.framesHitstopBl - 1 : (currentInfo.justHitBy.framesHitstop - 1)) / 60f, currentInfo.justHitBy.shakeStrength * (successfulBlock ? .75f : 1f)));
        //Adjust Facing Right
        facingRight = currentInfo.attackCoreX >= transform.position.x;

        bool isCharacter = currentInfo.attacker.GetComponent<Character>() != null;
        Debug.Log("THE ATTACKER IS " + (isCharacter ? "" : "NOT") + "A CHARACTER!");
        //Handle blocking
        if (!successfulBlock)
        {
            sh.IsBlocking = false;
            ani.SetBool("Blocking", false);
            CancelAction();
            m.RestoreAirFriction();
            comboHit++;
            ct.UpdateComboCounter(comboHit);
        }
        else
        {
            OnBlock();
        }

        //Deal damage
        if (!hbr.infiniteHealth)
        {
            currentHealth = Mathf.Clamp(currentHealth - (successfulBlock ? currentInfo.justHitBy.chipDamage : ApplyProration(currentInfo.justHitBy.damage)), 0, maxHealth);
            OnChangeHealth.Invoke();
        }

        //Take Knockback
        if (hbr.takesKnockback)
        {
            Vector3 mult;
            Vector3 vecFromAngle;

            switch (currentInfo.justHitBy.knockbackAngle)
            {
                case 361f:
                    vecFromAngle = (Vector3)currentInfo.attacker.GetComponent<Rigidbody2D>().velocity -
                       ((Vector3)(.5f * (currentInfo.attacker.transform.position - transform.position + (Vector3)currentInfo.justHitBy.offsetFromAnchor)) * (successfulBlock ? currentInfo.justHitBy.knockbackPowerOnBlock : currentInfo.justHitBy.knockbackPower));
                    if (successfulBlock) {
                        vecFromAngle.y = 0f;
                    }

                    rb2d.velocity = vecFromAngle;
                    break;

                default:
                    vecFromAngle = new Vector2(Mathf.Cos(currentInfo.justHitBy.knockbackAngle * Mathf.Deg2Rad), Mathf.Sin(currentInfo.justHitBy.knockbackAngle * Mathf.Deg2Rad));

                    //Pushback
                    if (sh.IsGrounded && sh.OnWall && sh.WallBehindCharacter(facingRight) && ((facingRight && vecFromAngle.x > 0) || (!facingRight && vecFromAngle.x > 0)) && !currentInfo.justHitBy.IsLauncher && !sh.InFlungState)
                    {
                        mult = vecFromAngle * new Vector2(-1, 0);
                        if (!currentInfo.attackerFaceRight)
                        {
                            mult.x *= -1;
                        }

                        if (isCharacter && comboHit >= 3)
                        {
                            currentInfo.attacker.GetComponent<Rigidbody2D>().velocity = mult * Mathf.Max(currentInfo.justHitBy.knockbackPowerOnBlock, 10f);
                        }
                    }

                    //Normal KBG
                    mult = vecFromAngle * new Vector2(!currentInfo.attackerFaceRight ? -1 : 1, successfulBlock ? 0 : 1);
                    rb2d.velocity = mult * (successfulBlock ? currentInfo.justHitBy.knockbackPowerOnBlock : currentInfo.justHitBy.knockbackPower);
                    break;
            }

        }

        //Take Hitstun
        if (hbr.takesHitstun)
        {
            TakeHitstun(successfulBlock ? currentInfo.justHitBy.framesBlockstun : currentInfo.justHitBy.framesHitstun, successfulBlock);
        }

        //Flung and Bounce
        if (!successfulBlock)
        {
            sh.InFlungState = currentInfo.justHitBy.IsLauncher || sh.InFlungState;
            sh.WillWallBounce = currentInfo.justHitBy.interaction == Hitbox.StageInteraction.WALLBOUNCE;
            sh.WillGroundBounce = currentInfo.justHitBy.interaction == Hitbox.StageInteraction.GROUNDBOUNCE;
            sh.Techable = currentInfo.justHitBy.techable;


            //Animation stuff
            flinchAnim = 1 - flinchAnim;
            ani.SetInteger("FlinchAnim", flinchAnim);
            ani.SetBool("Flung", sh.InFlungState);
            ani.ResetTrigger("Landing");
            ani.SetTrigger("GetHit");



            if (sh.OnWall && sh.WillWallBounce && sh.InFlungState && sh.WallBehindCharacter(facingRight) &&
                (
                (facingRight && -1 * Mathf.Cos(currentInfo.justHitBy.knockbackAngle * Mathf.Deg2Rad) < 0)
                ||
                (!facingRight && Mathf.Cos(currentInfo.justHitBy.knockbackAngle * Mathf.Deg2Rad) > 0))) //If facing right and knockback is to the left or facing right and knockback is to the right
            {
                //print("immediate wall bounce");
                WallBounceImmediate();
            }
            if (sh.IsGrounded && sh.WillGroundBounce && sh.InFlungState)
            {
                //print("OnGround and boutta bounce");
                CheckGroundBounce();
            }
        }

        //wait till end of frame?
        //Hitstop
        int hs = successfulBlock ? currentInfo.justHitBy.framesHitstopBl : currentInfo.justHitBy.framesHitstop;
        TakeHitstop(hs);

        if (isCharacter) {
            currentInfo.attacker.GetComponent<Character>().TakeHitstop(hs);
        }

    }



    /// 
    /// TERRAIN COLLISION HANDLING
    ///

    protected void CheckWallTouch()
    {
        CheckWallBounce();
        //Insert stuff about wall Jump Stuff here;
    }
    protected void CheckWallBounce()
    {
        if (sh.InFlungState && sh.WillWallBounce)
        {
            WallBounce();
        }
    }
    protected void WallBounce()
    {

        if (sh.CanTech())
        {
            WallTech();
        }
        else
        {
            rb2d.velocity = Constants.wallBounceForce * new Vector2((facingRight ? 1 : -1) * .2f, 1);
            sh.WillWallBounce = false;
            ani.SetTrigger("WallBounce");
            TakeHitstop(Constants.wallBounceHitStopTime);
        }


    }
    protected void WallTech()
    {
        ani.SetTrigger("Landing");
        rb2d.velocity = new Vector2((facingRight ? 1 : -1), 1) * Constants.wallTechBounceForce;
        TakeHitstun(Constants.wallTechBounceTime, false);
        sh.InFlungState = false;
    }
    protected void WallBounceImmediate()
    {
        sh.WillWallBounce = false;
        ani.SetTrigger("WallBounce");
        TakeHitstop(Constants.wallBounceHitStopTime);
        rb2d.velocity = Constants.wallBounceForce * new Vector2((facingRight ? 1 : -1) * .2f, 1);
        print(rb2d.velocity);

    }
    protected void CheckLanding()
    {
        if (sh.InFlungState)
        {
            CheckGroundBounce();
        }
        if (ac.performingAction || InStartup || InEndLag)
        {

            if ((ac.performingAction || ac.InEndLag) && ac.GetCurrentAction().info.endOnLanding)
            {
                CancelAction();
                ani.SetTrigger("Landing");
                sh.GiveLag(ac.GetCurrentAction().landingLag);
            }

            else {
                CancelAction();
                ani.SetTrigger("Landing");
                sh.GiveLag(Mathf.FloorToInt(landingLag));
            }

        }
        else
        {
            ani.SetTrigger("Landing");
            CancelAction();
        }
       
    }
    protected void CheckGroundBounce()
    {
        if (sh.CanTech())
        {
            GroundTech();
        }

        else
        {
            if (sh.WillGroundBounce)
            {
                Debug.Log("GroundBounce");
                GroundBounce();
            }
            else
            {
                Debug.Log("Groundbop");
                //print("gorndbop!");
                GroundBop();

            }
        }
    }
    protected void GroundBop()
    {
        ani.SetTrigger("GroundBop");
        TakeHitstun(Constants.groundBopLagTime, false); //Ground bop. Call animation when done!
        sh.WillTech = false;
        sh.InFlungState = false;
        sh.ResetTechCooldown();

    }
    protected void GroundBounce()
    {
        rb2d.velocity = new Vector2(0, Constants.groundBounceForce);
        sh.WillGroundBounce = false;
        ani.SetTrigger("GroundBounce");
        sh.WillTech = false;
        sh.ResetTechCooldown();
        TakeHitstun(Constants.groundBounceLagTime, false); ;
    }
    protected virtual void GroundTech()
    {
        ani.SetTrigger("Landing");
        rb2d.velocity = Vector2.zero;
        sh.WillTech = false;
        sh.InFlungState = false;
        sh.ResetTechCooldown();
        TakeHitstun(Constants.groundTechLagTime, false); ;
    }   


    public virtual void TakeLag(int lagAmountFrames)
    {
        sh.GiveLag(lagAmountFrames);
    }
    public virtual void TakeHitstun(int stunAmountFrames, bool successfulBlock)
    {
        
        if (stunAmountFrames == 0)
        {
            return;
        }

        HidePushBox();
        ani.SetBool("inHitstun", true);
        
        sh.GiveHitStun(stunAmountFrames);
        if (!successfulBlock)
        {
            // bodyCollider.sharedMaterial = stunMaterial;
        }
    }
    public virtual void TakeHitstop(int framesHitStop)
    {
        if (framesHitStop == 0)
        {
            return;
        }
        ac.Pause();
        velocityBeforeHitstop = rb2d.velocity;
        rb2d.velocity = Vector2.zero;
        rb2d.isKinematic = true;
        sh.GiveHitStop(framesHitStop);
        ani.speed = 0;
    }
    protected virtual void RestoreHitStop()
    {
        rb2d.isKinematic = false;
        ani.speed = 1;

        rb2d.velocity = velocityBeforeHitstop;
       
        /*if (sh.InFlungState && sh.InHitstun)
        {

            Vector2 v = velocityBeforeHitstop;
            v.x += Mathf.Max(Mathf.Abs(v.x), Constants.minXDI) * StickInput.x * Constants.diXAmount;
            float mag = v.magnitude;
            rb2d.velocity = (1 + (Constants.diMag * StickInput.y)) * mag * v.normalized;


            print("velocity before " + velocityBeforeHitstop);
            print("velocity now: " + rb2d.velocity);

        }
        else
        {
            rb2d.velocity = velocityBeforeHitstop;
        }
        */
        ac.Play();
        
    }
    protected virtual void KO()
    {
        if (disappearOnDeath)
        {
            Destroy(gameObject);
        }
        else
        {
            GetComponent<Rigidbody>().isKinematic = true;
            foreach (Collider c in GetComponents<Collider>())
            {
                c.enabled = false;
            }
        }

    }




    /// <summary>
    /// ADDING, GETTERS, AND SETTERS
    /// </summary>
    

    public GameObject GetInactiveInstance(int spawnNum)
    {
        for (int i = 0; i < spawnableInstances[spawnNum].Count; i++)
        {
            if (!spawnableInstances[spawnNum][i].GetComponent<Projectile>().activeProjectile)
            {
                return spawnableInstances[spawnNum][i];
            }
        }
        GameObject g = Instantiate(Spawnables[spawnNum]);
        Projectile p = g.GetComponent<Projectile>();

        g.GetComponent<ActionEmitter>().SetParent(gameObject);
        g.GetComponent<ActionEmitter>().targetsToHit = ac.targetsToHit;
        List<Action> ConfirmTypeList = new();
        p.DeactivateProjectile();
        spawnableInstances[spawnNum].Add(g);
        
        return g;
    }
    public void HidePushBox()
    {
        pushBox.Activate(false);
        //Debug.Log("HIDING PUSHBOX!");
    }
    public void ActivatePushBox()
    {

            pushBox.Activate(true);
      
        //Debug.Log("Run that shit back");

    }

    public void ActivatePushBoxHitstun()
    {
        if (sh.IsGrounded)
        {
            pushBox.Activate(true);
        }
    }
    protected bool CheckIfFalling()
    {
        return (rb2d.velocity.y < 0f && !sh.IsGrounded);
    }
    public float GetMass()
    {
        return rb2d.mass;
    }
    public void GetPushBoxInfo(out Vector2 offset, out Vector2 halfExtents)
    {
        offset = pushBox.Offset;
        halfExtents = pushBox.Extents;
    }

    public bool GetPushBoxActivated()
    {
        return pushBox.IsActivated;
    }
    public Action GetCurrAction()
    {
        return ac.GetCurrentAction();
    }


    public void ChangeMeter(int a)
    {
        ct.ChangeMeter(a);
        OnChangeMeter.Invoke();
    }
    public bool GetFacingRight()
    {
        return facingRight;
    }
    public virtual void SetFacingRight(bool b, bool airOverride)
    {
        if (!(airOverride || sh.IsGrounded))
        {
            return;
        }

        if (!FullCanMove() && !FullCanMoveDummy() )
        {
            return;
        }

        if (b == facingRight)
        {
            return;
        }
        facingRight = !facingRight;

        for (int i = 0; i < directionBuffer.Count; i++)
        {
            directionBuffer[i] = MyEnums.GetOppositeX(directionBuffer[i]);
        }
    }
    public virtual void BeginDash()
    {
        m.SetupDash();
    }
    public void SetFinishActionTrigger()
    {
        //ani.SetBool("Flung", false);
        //print("not flung anymore...");
        ani.SetTrigger("FinishAction");
    }
    public void SetHitstunTriggerT()
    {
        ani.SetBool("inHitstun", true);
    }
    public void SetHitstunTriggerF()
    {
        comboHit = 0;
        ani.SetBool("inHitstun", false);
        ani.ResetTrigger("GroundBounce");
        ani.ResetTrigger("WallBounce");
        ani.SetBool("Flung", false);
    }


    /// <summary>
    /// PERFORMING AND TRYING ACTIONS / MOVES
    /// </summary>


    public virtual void PerformRekka(Rekka r)
    {
        if (sh.CanRekka())
        {
            //ac.CancelAction();
            ac.PerformAction(r.RekkaAction, facingRight);
        }
    }
    public virtual void PerformActionDummy(Action a)
    {
        if (sh.CanAttackDummy())
        {
            ac.PerformAction(a, facingRight);
        }
    }
    public virtual void PerformAction(Action a)
    {
        if (sh.InLag && (ac.performingAction || ac.InEndLag) && ac.canCancel)
        {
            if (ac.canCancel && GetCurrAction().CancelsInto(a, CurrMeter >= movementCancelCost && movementCancel, out bool mC, CurrMeter >= specialToSpecialCost && specialToSpecialCancel, out bool stsc, CurrMeter >= reverseBeatCost && reverseBeat, out bool wRVB))
            {
                Debug.Log("ACTION " + GetCurrAction().name + " CANCELS INTO " + a + "!");

                if (mC)
                {
                    Debug.Log("At the cost of movementCancelling (1 bar of meter)");
                    ChangeMeter(-movementCancelCost);
                }
                if (stsc)
                {
                    Debug.Log("At the cost of special to special cancelling (1 bar of meter)");
                    ChangeMeter(-specialToSpecialCost);
                }
                else if (wRVB)
                {
                    Debug.Log("At the cost of reverse beating (1 bar of meter");
                    ChangeMeter(-reverseBeatCost);
                    Debug.Log("new action: " + a.name);
                    //Debug.Break();
                }
               
                CancelAction();
                ac.PerformAction(a, facingRight);
            }
            else
            {
               // Debug.Log("Action " + GetCurrAction().name + " does not cancel into " + a + "..." + (ac.landedHit ? " even though you landed a hit!" : " no hit was landed... "));
            }

        }

        else if (sh.CanAttack())
        {
            ac.PerformAction(a, facingRight);
           
        }
    }
    void DoAction()
    {
        Action currAc = ac.GetCurrentAction();
        sh.IsBlocking = false;
        if (currAc.info.stopUser || sh.IsGrounded)
        {
            rb2d.velocity = Vector2.zero;
            m.RestoreAirFriction();
        }
        TakeLag(currAc.attackDuration);
        if (currAc.info.negateGravity)
        {
            sh.NegateGravity = true;
        }
        else
        {
            sh.NegateGravity = false;
        }

        /*
        if (currAc.type == Action.MoveType.REKKA && currAc.input == Action.MoveInput.SUPER)
        {
            return;
        }
        */

        if (!currAc.info.skipAnimCall)
        {
            ani.SetInteger("ActionType", (int)currAc.type);
            ani.SetInteger("ActionCase", (int)currAc.input);
            ani.SetTrigger("PerformAction");
        }
    }
    public void CancelAction()
    {
        ac.CancelAction();
        sh.CancelLag();
        sh.NegateGravity = false;
        ani.SetTrigger("FinishAction");
    }
    public virtual bool CheckIfBlocking(float originX)
    {
      //  print("CHECK IF BLOCKING:\nfacingRight = " + facingRight + "\noriginX = " + originX + "\ntransform.position.x = " + transform.position.x + "\n should return " + (blocking && ((facingRight && transform.position.x <= originX) || (!facingRight && transform.position.x >= originX))));
        return sh.IsBlocking && ((facingRight && transform.position.x <= originX) || (!facingRight && transform.position.x >= originX));
    }
    public void TryRegenHealth()
    {
        if ((currentHealth != maxHealth) && !sh.InHitstun)
        {

            if (incHealthTimer >= incHealthTimerMax)
            {
                currentHealth = maxHealth;
                OnChangeHealth.Invoke();
                incHealthTimer = 0f;
            }
            else
            {
                incHealthTimer += Time.fixedDeltaTime;
            }
        }
    }
    public virtual void OnBlock()
    {
        print("You blocked!");
        //ani.SetBool("Blocking", true);
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(transform.position + Vector3.right * (facingRight ? 1 : -1), .1f);
    }
    public bool FullCanMove()
    {
        return sh.CanMove() && (!sh.InLag || (ac.performingAction && ac.GetCurrentAction().info.canMoveDuring && !sh.IsGrounded));
    }
    public bool FullCanMoveDummy()
    {
        return sh.CanAttackDummy() && (!sh.InLag || (ac.performingAction && ac.GetCurrentAction().info.canMoveDuring && !sh.IsGrounded));
    }
    public void ChangeStance(int newStance)
    {
        currentStance = newStance;
        ani.runtimeAnimatorController = stances[newStance].AnimationController;
    }
    protected void AddToDirectionBuffer(MyEnums.StickDirection sd)
    {
        if (directionBuffer.Count != 0 && directionBuffer[^1] == sd)
        {
            return;
        }

        directionBuffer.Add(sd);
        motionInputBufferFrameCount = 0;
        if (directionBuffer.Count >= directionBufferMaxLength)
        {
            directionBuffer.RemoveAt(0);
        }
    }
    protected bool CheckAllRekkas()
    {
        if (ac.CanCheckForRekkas())
        {
            //print("checkin rekkas");
            for (int i = 0; i < ac.GetCurrentAction().rekkas.Length; i++)
            {

                if (startedButtons[(int)MyEnums.ButtonInput.LIGHT])
                {
                    if (ac.CheckForRekkas(out Rekka r, facingRight, sh.IsGrounded, StickInput, MyEnums.ButtonInput.LIGHT, i))
                    {
                        PerformRekka(r);
                        return true;
                    }
                }

                if (startedButtons[(int)MyEnums.ButtonInput.MEDIUM])
                {
                    if (ac.CheckForRekkas(out Rekka r, facingRight, sh.IsGrounded, StickInput, MyEnums.ButtonInput.MEDIUM, i))
                    {
                        PerformRekka(r);
                        return true;
                    }
                }

                if (startedButtons[(int)MyEnums.ButtonInput.HEAVY])
                {
                    if (ac.CheckForRekkas(out Rekka r, facingRight, sh.IsGrounded, StickInput, MyEnums.ButtonInput.HEAVY, i))
                    {
                        PerformRekka(r);
                        return true;
                    }
                }

                if (startedButtons[(int)MyEnums.ButtonInput.SPECIAL])
                {
                    if (ac.CheckForRekkas(out Rekka r, facingRight, sh.IsGrounded, StickInput, MyEnums.ButtonInput.SPECIAL, i))
                    {
                        PerformRekka(r);
                        return true;
                    }
                }

                if (startedButtons[(int)MyEnums.ButtonInput.BLOCK])
                {
                    if (ac.CheckForRekkas(out Rekka r, facingRight, sh.IsGrounded, StickInput, MyEnums.ButtonInput.BLOCK, i))
                    {
                        PerformRekka(r);
                        return true;
                    }
                }

                if (startedButtons[(int)MyEnums.ButtonInput.STANCE])
                {
                    if (ac.CheckForRekkas(out Rekka r, facingRight, sh.IsGrounded, StickInput, MyEnums.ButtonInput.STANCE, i))
                    {
                        PerformRekka(r);
                        return true;
                    }
                }

                if (downButtons[(int)MyEnums.ButtonInput.JUMP])
                {
                    if (ac.CheckForRekkas(out Rekka r, facingRight, sh.IsGrounded, StickInput, MyEnums.ButtonInput.JUMP, i))
                    {
                        PerformRekka(r);
                        return true;
                    }
                }

                if (startedButtons[(int)MyEnums.ButtonInput.DASH])
                {
                    if (ac.CheckForRekkas(out Rekka r, facingRight, sh.IsGrounded, StickInput, MyEnums.ButtonInput.DASH, i))
                    {
                        PerformRekka(r);
                        return true;
                    }
                }


                if (ac.CheckForRekkas(out Rekka r1, facingRight, sh.IsGrounded, StickInput, MyEnums.ButtonInput.IGNORE, i))
                {
                    PerformRekka(r1);
                    return true;

                }

            }
        }

        return false;
    }
    protected void CheckInputs()
    {
        MyEnums.StickDirection sd = MyEnums.GetStickPosition(stickInput, facingRight);
        AddToDirectionBuffer(sd);

        /*
        if (pia == null)
        {
            print(name + ": this is an issue");
        }
        if (pia.Player.Light.WasPressedThisFrame())
        {
            startedButtons[(int)MyEnums.ButtonInput.LIGHT] = true;
        }
        if (pia.Player.Light.IsPressed())
        {
            downButtons[(int)MyEnums.ButtonInput.LIGHT] = true;
        }

        if (pia.Player.Medium.WasPressedThisFrame())
        {
            startedButtons[(int)MyEnums.ButtonInput.MEDIUM] = true;
        }
        if (pia.Player.Medium.IsPressed())
        {
            downButtons[(int)MyEnums.ButtonInput.MEDIUM] = true;
        }

        if (pia.Player.Heavy.WasPressedThisFrame())
        {
            startedButtons[(int)MyEnums.ButtonInput.HEAVY] = true;
        }
        if (pia.Player.Heavy.IsPressed())
        {
            downButtons[(int)MyEnums.ButtonInput.HEAVY] = true;
        }

        if (pia.Player.Special.WasPressedThisFrame())
        {
            startedButtons[(int)MyEnums.ButtonInput.SPECIAL] = true;
        }
        if (pia.Player.Special.IsPressed())
        {
            downButtons[(int)MyEnums.ButtonInput.SPECIAL] = true;
        }

        if (pia.Player.Block.WasPressedThisFrame())
        {
            startedButtons[(int)MyEnums.ButtonInput.BLOCK] = true;
        }
        if (pia.Player.Block.IsPressed())
        {
            downButtons[(int)MyEnums.ButtonInput.BLOCK] = true;
        }


        if (pia.Player.Jump.WasPressedThisFrame())
        {
            startedButtons[(int)MyEnums.ButtonInput.JUMP] = true;
        }
        if (pia.Player.Jump.IsPressed())
        {
            downButtons[(int)MyEnums.ButtonInput.JUMP] = true;
        }

        if (pia.Player.Dash.WasPerformedThisFrame())
        {
            startedButtons[(int)MyEnums.ButtonInput.DASH] = true;
            Debug.Log("Dash was pressed this frame");
        }
        if (pia.Player.Dash.IsPressed())
        {
            downButtons[(int)MyEnums.ButtonInput.DASH] = true;
        }
        
        */

    }
    protected void CheckInputArrays()
    {
        if (CheckAllRekkas())
        {
            return;
        }
        if (startedButtons[(int)MyEnums.ButtonInput.BLOCK])
        {
            if (sh.InHitstun)
            {
                sh.TryTech();
            }

            else if (sh.CanBlock())
            {
                ani.SetBool("Blocking", true);
                ani.SetTrigger("CallBlocking");
                sh.IsBlocking = true;
                rb2d.velocity = new Vector2(0f, rb2d.velocity.y);
            }
        }

        if (downButtons[(int)MyEnums.ButtonInput.BLOCK])
        {
            if (sh.CanBlock())
            {
                sh.IsBlocking = true;
                rb2d.velocity = new Vector2(0f, rb2d.velocity.y);
                ani.SetBool("Blocking", true);
            }

        }
        else
        {
            sh.IsBlocking = false;
            ani.SetBool("Blocking", false);
        }


        motionInputs.Clear();

        if (startedButtons[(int)MyEnums.ButtonInput.LIGHT] || startedButtons[(int)MyEnums.ButtonInput.MEDIUM] || startedButtons[(int)MyEnums.ButtonInput.HEAVY])
        {
            motionInputs = MyEnums.FindMotions(directionBuffer);
        }



        if (startedButtons[(int)MyEnums.ButtonInput.SPECIAL]) //Special
        {
            if (directionBuffer[^1] == MyEnums.StickDirection.DOWN || directionBuffer[^1] == MyEnums.StickDirection.DOWNFORWARD)
            {
                PerformAction(stances[currentStance].moveset[MyEnums.GetMovesetPosition(MyEnums.AniNums._DPL)]);
            }
            else if (directionBuffer[^1] == MyEnums.StickDirection.FORWARD)
            {
                PerformAction(stances[currentStance].moveset[MyEnums.GetMovesetPosition(MyEnums.AniNums._HCFL)]);
            }
            else
            {
                PerformAction(stances[currentStance].moveset[MyEnums.GetMovesetPosition(MyEnums.AniNums._QCFL)]);
            }

            //directionBuffer.Clear();

        }




        if (startedButtons[(int)MyEnums.ButtonInput.LIGHT]) //LIGHT
        {

            if (motionInputs.Count > 0)
            {
                PerformAction(stances[currentStance].moveset[MyEnums.GetMovesetPosition(MyEnums.MotionToAniNums(motionInputs[^1], MyEnums.ButtonInput.LIGHT))]);
            }

            else if (!sh.IsGrounded)
            {
                PerformAction(stances[currentStance].moveset[MyEnums.GetMovesetPosition(MyEnums.AniNums._jL)]);
                Debug.Log(sh.IsGrounded);
                //Debug.Break();
            }
            else if (directionBuffer[^1] == MyEnums.StickDirection.DOWN || directionBuffer[^1] == MyEnums.StickDirection.DOWNFORWARD)
            {
                PerformAction(stances[currentStance].moveset[MyEnums.GetMovesetPosition(MyEnums.AniNums._2L)]);
            }

            else if (directionBuffer[^1] == MyEnums.StickDirection.FORWARD)
            {
                PerformAction(stances[currentStance].moveset[MyEnums.GetMovesetPosition(MyEnums.AniNums._6L)]);
            }
            else
            {
                PerformAction(stances[currentStance].moveset[MyEnums.GetMovesetPosition(MyEnums.AniNums._5L)]);
            }
            directionBuffer.Clear();
        }

        else if (startedButtons[(int)MyEnums.ButtonInput.MEDIUM]) //Medium
        {

            if (motionInputs.Count > 0)
            {
                PerformAction(stances[currentStance].moveset[MyEnums.GetMovesetPosition(MyEnums.MotionToAniNums(motionInputs[^1], MyEnums.ButtonInput.MEDIUM))]);
            }

            else if (!sh.IsGrounded)
            {
                PerformAction(stances[currentStance].moveset[MyEnums.GetMovesetPosition(MyEnums.AniNums._jM)]);
            }
            else if (directionBuffer[^1] == MyEnums.StickDirection.DOWN || directionBuffer[^1] == MyEnums.StickDirection.DOWNFORWARD)
            {
                PerformAction(stances[currentStance].moveset[MyEnums.GetMovesetPosition(MyEnums.AniNums._2M)]);
            }
            else if (directionBuffer[^1] == MyEnums.StickDirection.FORWARD)
            {
                PerformAction(stances[currentStance].moveset[MyEnums.GetMovesetPosition(MyEnums.AniNums._6M)]);
            }
            else
            {
                PerformAction(stances[currentStance].moveset[MyEnums.GetMovesetPosition(MyEnums.AniNums._5M)]);
            }

            directionBuffer.Clear();
        }

        else if (startedButtons[(int)MyEnums.ButtonInput.HEAVY]) //Heavy
        {

            if (motionInputs.Count > 0)
            {
                Debug.Log("INT OF HIGHEST PRIORITY IS...: " + (int)MyEnums.GetMovesetPosition(MyEnums.MotionToAniNums(motionInputs[^1], MyEnums.ButtonInput.HEAVY)));
                PerformAction(stances[currentStance].moveset[MyEnums.GetMovesetPosition(MyEnums.MotionToAniNums(motionInputs[^1], MyEnums.ButtonInput.HEAVY))]);
            }
            else if (!sh.IsGrounded)
            {
                PerformAction(stances[currentStance].moveset[MyEnums.GetMovesetPosition(MyEnums.AniNums._jH)]);
            }
            else if (directionBuffer[^1] == MyEnums.StickDirection.DOWN || directionBuffer[^1] == MyEnums.StickDirection.DOWNFORWARD)
            {
                PerformAction(stances[currentStance].moveset[MyEnums.GetMovesetPosition(MyEnums.AniNums._2H)]);
            }
            else if (directionBuffer[^1] == MyEnums.StickDirection.FORWARD)
            {
                PerformAction(stances[currentStance].moveset[MyEnums.GetMovesetPosition(MyEnums.AniNums._6H)]);
            }
            else
            {
                PerformAction(stances[currentStance].moveset[MyEnums.GetMovesetPosition(MyEnums.AniNums._5H)]);
            }

            directionBuffer.Clear();
        }




        else if (startedButtons[(int)MyEnums.ButtonInput.JUMP]) //Jump
        {
            m.OnPressJump();

        }

        else if (stickInput.y == -1)
        {
            m.FastFall();
        }

        else if (startedButtons[(int)MyEnums.ButtonInput.DASH]) //Dash
        {
            PerformAction(DashAction);
            //m.BeginDash(m.dashSpeed, new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), ref facingRight);
            //sc.SetTrigger("Dash");
        }


    }



}