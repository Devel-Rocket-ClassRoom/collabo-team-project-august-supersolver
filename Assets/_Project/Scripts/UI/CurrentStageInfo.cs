using PPS.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 지금 어느 자리를 풀고 있는지 알린다. 그리기 화면은
/// 스테이지가 갈려도 파괴되지 않아 켜질 때 다시 읽는다.
/// </summary>
public class CurrentStageInfo : MonoBehaviour
{
    /// 지금 테마의 그림. 테마 선택 버튼과 같은 것을 쓴다.
    [SerializeField] Image themeIcon;

    /// 보상 화면과 같은 표기로 찍는 자리 번호.
    [SerializeField] TextMeshProUGUI stageLabel;

    /// 테마 선택 버튼 그림이 들어 있는 카탈로그.
    [SerializeField] ThemeAssetCatalog catalog;

    static CurrentStageInfo Instance;

    void Awake()
    {
        if (Instance != null) return;
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 판이 갈렸다. 그리기 화면은 이미 떠 있어 다시 켜지지
    /// 않으므로 진입 지점이 직접 알려 준다.
    /// </summary>
    public static void SetStage(StageEntry entry) => Instance?.Apply(entry);

    void OnEnable() => Apply(StageSelection.Current);

    void Apply(StageEntry entry)
    {
        stageLabel.text = entry.ToLabel();

        if (entry.Theme < catalog.Asset.Count)
            themeIcon.sprite = catalog.Asset[entry.Theme].Spr_SelectButton;
    }
}
