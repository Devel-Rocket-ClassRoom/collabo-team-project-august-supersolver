using PPS.Core;
using PPS.DrawingTool;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageButton : MonoBehaviour
{
    static bool locked = false;
    [Header("Image")]
    [SerializeField] Image Img_Locked;
    [SerializeField] Image Img_Star1;
    [SerializeField] Image Img_Star2;
    [SerializeField] Image Img_Star3;

    [Header("txt")]
    [SerializeField] TextMeshProUGUI stageNumText;
    int stageIdx = -1;

    public void OnUpdate(int stageIdx, int maxStageIdx, int lastCleared)
    {
        bool isLocked = stageIdx < 0 || stageIdx >= maxStageIdx
            || stageIdx > CurrentStageIndex.GetStageAndThemeIndex(lastCleared).Item2;

        Img_Locked.gameObject.SetActive(isLocked);
        this.stageIdx = isLocked ? -1 : stageIdx;
        stageNumText.text = isLocked ? "" : (stageIdx + 1).ToString();

        var best = isLocked ? (stars: 0, grade: InkGrade.Bronze) : BestClearOf(stageIdx);
        ApplyThemeSprites(best.grade);

        Img_Star1.gameObject.SetActive(best.stars >= 1);
        Img_Star2.gameObject.SetActive(best.stars >= 2);
        Img_Star3.gameObject.SetActive(best.stars >= 3);
    }

    // 스프라이트는 테마마다 달라져서 갱신 시점마다 다시 받아온다.
    // 별 세 개는 개수만 나타내고, 그림은 등급 하나로 통일한다.
    void ApplyThemeSprites(int grade)
    {
        if (!ServiceLocator.TryGet<IThemeRepository>(out var repo)) return;

        Img_Locked.sprite = repo.Asset.SprLocked;

        Sprite star = StarOf(repo.Asset, grade);
        Img_Star1.sprite = star;
        Img_Star2.sprite = star;
        Img_Star3.sprite = star;
    }

    static Sprite StarOf(ThemeModel theme, int grade)
    {
        switch (grade)
        {
            case InkGrade.Gold: return theme.SprStarGold;
            case InkGrade.Silver: return theme.SprStarSilver;
            default: return theme.SprStarBronze;
        }
    }

    // 같은 스테이지 기록이 여러 번 쌓일 수 있어 가장 좋은 값을 고른다.
    static (int stars, int grade) BestClearOf(int stageIdx)
    {
        if (!ServiceLocator.TryGet<IUserDataRepository>(out var repo))
            return (0, InkGrade.Bronze);

        int stars = 0;
        int grade = InkGrade.Bronze;
        var clears = repo.Data.StageClears;
        for (int i = 0; i < clears.Count; i++)
        {
            if (clears[i].StageIndex != stageIdx || !clears[i].IsCleared) continue;
            stars = Mathf.Max(stars, clears[i].BestStars);
            grade = Mathf.Max(grade, clears[i].StarGrade);
        }
        return (stars, grade);
    }

    public async void OnClicked()
    {
        if (stageIdx == -1) return;
        if (locked) return;
        locked = true;
        if (ServiceLocator.TryGet<IThemeRepository>(out var repo))
        {
            var StageData = repo.Asset.Stages[stageIdx];

            await UIManager.Instance.ShowScene<DrawingToolSceneUI>();

            StageLoader.SetStage(StageData);
            TutorialViewer.SetStage(stageIdx);
            CurrentStageIndex.CurrentStage = stageIdx;
        }
        locked = false;
    }
}
