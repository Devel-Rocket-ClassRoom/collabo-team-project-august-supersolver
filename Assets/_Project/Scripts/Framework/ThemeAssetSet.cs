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

    /// 스테이지 버튼 위에 덮이는 자물쇠 그림.
    public Sprite SprLocked;

    /// 스테이지 버튼이 획득한 별을 왼쪽부터 채울 때 쓰는 그림.
    public Sprite SprStar1;
    public Sprite SprStar2;
    public Sprite SprStar3;

    public TextAsset[] stages;
    public Tutorial[] tutorials;
}
