using PPS.Core;
using PPS.MapEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "ThemeAssetSet", menuName = "Scriptable Objects/ThemeAssetSet")]
public class ThemeAssetSet : ScriptableObject
{
    public MapEditStyle MapStyle;
    public Sprite stageSelectBackground;

    /// 드로잉툴 판 뒤에 깔리는 그림.
    public Sprite playBackground;

    public TextAsset[] stages;
    public Tutorial[] tutorials;
}
