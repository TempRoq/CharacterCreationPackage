using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Stance")]
public class Stance : ScriptableObject
{
    [Tooltip("Move Index = \nLight = 0\nMedium = 3\nHeavy = 6\nSpecial = 9\n\n + \n\nNeutral = 0\nCrouching = 1\nAerial/Side = 2")]
    public Action[] moveset;
    public RuntimeAnimatorController AnimationController;
}
