using PPS.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "TutorialSet", menuName = "Scriptable Objects/TutorialSet")]
public class TutorialSet : ScriptableObject
{
    public Tutorial[] Tutorials;
}
