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

    /// 잉크를 아낀 등급별 별 그림. 스테이지 버튼이
    /// 채우는 별은 전부 같은 등급 그림을 쓴다.
    public Sprite SprStarBronze;
    public Sprite SprStarSilver;
    public Sprite SprStarGold;

    public TextAsset[] stages;
    public Tutorial[] tutorials;
}
