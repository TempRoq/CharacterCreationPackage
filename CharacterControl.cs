using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterControl : MonoBehaviour
{

    protected PlayerInput pInpt;
    protected bool[] startedButtons;
    protected bool[] downButtons;
    protected bool[] upButtons;

    protected Vector2 stickInput;
    public bool[] StartedButtons { get { return startedButtons; } }
    public bool[] DownButtons { get { return downButtons; } }
    public bool[] UpButtons {  get { return upButtons; } }

    public Vector2 StickInput {  get { return stickInput; } }




    protected void CheckButtonPressedDown(MyEnums.ButtonInput inpt, InputAction.CallbackContext ctx)
    {

       
        if (ctx.started && !downButtons[(int)inpt]) //If the button was pressed and the button is not considered started
        {
            Debug.Log(name + ": " + inpt.ToString() + " Button Pressed Down");
            startedButtons[(int)inpt] = true;    
            downButtons[(int)inpt] = true;
        }

        else if (ctx.canceled && downButtons[(int)inpt])
        {
            Debug.Log(name + ": " + inpt.ToString() + " Button Released");        
            upButtons[(int)inpt] = true; 
            downButtons[(int)inpt] = false;
        }
        
    }

    protected void CheckButtonPressedDown(MyEnums.ButtonInput inpt, bool b)
    {
        //Debug.Log(name + ": " + stickInput.y);
        if (b && !downButtons[(int)inpt]) //If the button was pressed and the button is not considered started
        {
            Debug.Log(name + ": " + inpt.ToString() + " Button Pressed Down");
            startedButtons[(int)inpt] = true;
            downButtons[(int)inpt] = true;
        }

        else if (!b && downButtons[(int)inpt])
        {
            Debug.Log(name + ": " + inpt.ToString() + " Button Released");
            upButtons[(int)inpt] = true;
            downButtons[(int)inpt] = false;
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        //Debug.Log(context.ReadValue<float>());
        CheckButtonPressedDown(MyEnums.ButtonInput.JUMP, context);
    }

    public void OnLight(InputAction.CallbackContext context)
    {
        CheckButtonPressedDown(MyEnums.ButtonInput.LIGHT, context);
    }

    public void OnMedium(InputAction.CallbackContext context)
    {
        CheckButtonPressedDown(MyEnums.ButtonInput.MEDIUM, context);
    }

    public void OnHeavy(InputAction.CallbackContext context)
    {
        CheckButtonPressedDown(MyEnums.ButtonInput.HEAVY, context);
    }

    public void OnSpecial(InputAction.CallbackContext context)
    {
        CheckButtonPressedDown(MyEnums.ButtonInput.SPECIAL, context);
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        CheckButtonPressedDown(MyEnums.ButtonInput.DASH, context);
    }

    public void OnBlock(InputAction.CallbackContext context)
    {
        CheckButtonPressedDown(MyEnums.ButtonInput.BLOCK, context);
    }

    public void OnStance(InputAction.CallbackContext context)
    {
        CheckButtonPressedDown(MyEnums.ButtonInput.STANCE, context);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        stickInput = context.ReadValue<Vector2>();
        CheckButtonPressedDown(MyEnums.ButtonInput.JUMP, stickInput.y >= .7f);
    }

    public void FrameUpdate()
    {
        for (int i = 0; i < startedButtons.Length; i++)
        {
            startedButtons[i] = false;
            upButtons[i] = false;
            //downButtons[i] = false;
        }
    }

    protected virtual void Awake()
    {
        pInpt = GetComponent<PlayerInput>();
        pInpt.enabled = true;
        startedButtons = new bool[9];
        downButtons = new bool[9];
        upButtons = new bool[9];
        // pia.Enable();
    }
    // Update is called once per frame
    protected virtual void Update()
    {

    }

    



}