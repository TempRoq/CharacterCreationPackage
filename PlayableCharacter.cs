using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayableCharacter : Character
{
    [SerializeField]
    protected InputDisplay id;
    
    protected override void FixedUpdate()
    {

        base.FixedUpdate();
        if (id) //If InputDisplay exists, draw the inputs.
        {
            //Debug.Log(name + ": " + startedButtons);
            id.DrawInputs(StickInput, startedButtons);
            
        }
        cc.FrameUpdate();
    }
}
