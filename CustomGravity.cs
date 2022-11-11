using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public class CustomGravity : MonoBehaviour
{

    public float gravityScale = 1.0f;
    public float gravityThreshold;

    float baseGravScale;
    float baseGravThresh;

    public float TerminalVelocity;



    float globalGravity;
    Rigidbody2D rb2d;
    // Start is called before the first frame update

    private void OnEnable()
    {

    }

    private void Awake()
    {

    }
    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        globalGravity = Physics2D.gravity.y;
        rb2d.gravityScale = 0f;

        baseGravScale = gravityScale;
        baseGravThresh = gravityThreshold;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 gravity = globalGravity * Vector3.up * (Mathf.Abs(rb2d.velocity.y) > gravityThreshold ? gravityScale : 1f);
        rb2d.AddForce(gravity, ForceMode2D.Force);

        if (Mathf.Abs(rb2d.velocity.y) > TerminalVelocity)
        {
            Vector3 newVel = rb2d.velocity;
            newVel.y = Mathf.Clamp(newVel.y, -TerminalVelocity, TerminalVelocity);
        }
    }

    public void ChangeGravity(float globGrav, float gravScal, float gravThresh)
    {
        gravityScale = gravScal;
        gravityThreshold = gravThresh;
        globalGravity = globGrav;
    }

    public void ReturnBaseValues()
    {
        gravityScale = baseGravScale;
        gravityThreshold = baseGravThresh;
        globalGravity = Physics.gravity.y;

    }
}