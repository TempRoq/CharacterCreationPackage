using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteChanger : MonoBehaviour
{
    
    public bool changeSprites;
    public GameObject mainSprite;
    SpriteRenderer sr;
    Animator anim;
    public Sprite[] allSprites;

    public List<SpriteRenderer> allSpriteChildren;
    // Start is called before the first frame update
    private void Start()
    {/*
        sr = mainSprite.GetComponent<SpriteRenderer>();
        anim = mainSprite.GetComponent<Animator>();
        allSpriteChildren = new List<SpriteRenderer>();
        foreach(SpriteRenderer s in GetComponentsInChildren<SpriteRenderer>())
        {
            allSpriteChildren.Add(s);
        }
        */
    }


    private void OnEnable()
    {

    }
    // Update is called once per frame
    void Update()
    {
        
    }

    public bool HasAnim()
    {
        return anim != null;
    }
    public void ChangeSprite(int i)
    {
        sr.sprite = allSprites[i];
    }

    public void FlipSpriteHorizontal(bool b)
    {
        /*
        foreach(SpriteRenderer s in allSpriteChildren)
        {
            s.flipX = b;
        }
        */
    }

    public void SetTrigger(string trigger)
    {
        if (anim != null)
        {
            anim.SetTrigger(trigger);
        }
    }

    public void SetBool(string parameter, bool b)
    {
        if (anim != null)
        anim.SetBool(parameter, b);
    }

    public void SetInt(string parameter, int i)
    {
        if (anim != null)
        anim.SetInteger(parameter, i);
    }

    public void SetFloat(string parameter, float f)
    {
        if (anim != null)
        anim.SetFloat(parameter, f);
    }

    public void ChangeColor(Color c)
    {
        //sr.color = c;
    }
}
