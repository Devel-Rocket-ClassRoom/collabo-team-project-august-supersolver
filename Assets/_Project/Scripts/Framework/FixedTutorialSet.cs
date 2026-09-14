using PPS.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "FixedTutorialSet", menuName = "Scriptable Objects/FixedTutorialSet")]
public class FixedTutorialSet : ScriptableObject
{
    public FixedTutorial[] FixedTutorials;
}
