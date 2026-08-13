using UnityEngine;

[CreateAssetMenu(fileName = "ThemeAssetSet", menuName = "Scriptable Objects/ThemeAssetSet")]
public class ThemeAssetSet : ScriptableObject
{
    public Sprite stageSelectBackground;
    //public Sprite ballIcon;

    public TextAsset[] stages;
}
