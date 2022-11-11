using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterTeam
{
    public Character teammate1;
    public Character teammate2;
    public UIController.Side UISide;

    public int currMeter;
    public int maxMeter = 500;
    // Start is called before the first frame update
    
    public enum PlayState
    {
        OUTOFPLAY = 0,
        CONTROLLED,
        ASSIST,
        KO  
    }

    public void ChangeMeter(int a)
    {
        currMeter = Mathf.Clamp(currMeter + a, 0, maxMeter);
    }

    public void UpdateSuperBar()
    {
        UIController.instance.UpdateSuperBar(UISide, currMeter, maxMeter);
    }

    public void UpdateHealthBar()
    {
        UIController.instance.UpdateHealthbar(UISide, teammate1.currentHealth, teammate1.maxHealth, teammate2.currentHealth, teammate2.maxHealth);
    }

    public void UpdateComboCounter(int comboNum)
    {
        UIController.instance.UpdateComboCounter(UISide, comboNum);
    }
    
}
