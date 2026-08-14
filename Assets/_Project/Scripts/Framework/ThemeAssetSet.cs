using PPS.MapEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "ThemeAssetSet", menuName = "Scriptable Objects/ThemeAssetSet")]
public class ThemeAssetSet : ScriptableObject
{
    public MapEditStyle MapStyle;
    public Sprite stageSelectBackground;

    public TextAsset[] stages;
}
