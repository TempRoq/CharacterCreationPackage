using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PushBox : MonoBehaviour
{
    BoxCollider2D bc2d;

    public bool IsActivated { get { return !bc2d.isTrigger; } }
    public Vector2 Offset { get { return bc2d.offset; } }
    public Vector2 Extents { get { return bc2d.bounds.extents; } }
    public LayerMask enemyLayerMask;
    private void Start()
    {
        bc2d = GetComponent<BoxCollider2D>();
    }

    public void Activate(bool b)
    {
        bc2d.isTrigger = !b;
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {

        FightManager.instance.HandlePlayerSlidePrimer(gameObject, collision.gameObject);
       
        /*
        if (collision.GetContact(0).normal == new Vector2(0f, 1f))
        {
            //print(transform.root.name + ": I made contact with a normal of " + collision.GetContact(0).normal + " with " + collision.collider.gameObject.transform.parent.name);
            FightManager.instance.HandlePlayerSlidePrimer();
            return;
        } 
        */
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //print(transform.parent.gameObject.name + ": I made contact with a normal of " + collision.GetContact(0).normal + " with " + collision.collider.gameObject.transform.parent.name);
        //print(transform.root.gameObject.name + ": HANDLE COLLISION. I'M IN THE AIR!");
        FightManager.instance.HandlePlayerSlidePrimer(gameObject, collision.gameObject);
    }
    private void FixedUpdate()
    {

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, .5f);
        BoxCollider2D bc2d = GetComponent<BoxCollider2D>();
        Vector3 mult = bc2d.offset;
        mult.Scale(new Vector3(GetComponentInParent<Character>().facingRight ? 1 : -1, 1));
        Gizmos.DrawCube(transform.root.position + mult, bc2d.bounds.extents * 2);
    }
}
