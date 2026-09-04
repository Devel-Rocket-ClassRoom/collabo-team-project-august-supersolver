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
        ApplyThemeSprites();

        bool isLocked = stageIdx < 0 || stageIdx >= maxStageIdx
            || stageIdx > CurrentStageIndex.GetStageAndThemeIndex(lastCleared).Item2;

        Img_Locked.gameObject.SetActive(isLocked);
        this.stageIdx = isLocked ? -1 : stageIdx;
        stageNumText.text = isLocked ? "" : (stageIdx + 1).ToString();

        int stars = isLocked ? 0 : BestStarsOf(stageIdx);
        Img_Star1.gameObject.SetActive(stars >= 1);
        Img_Star2.gameObject.SetActive(stars >= 2);
        Img_Star3.gameObject.SetActive(stars >= 3);
    }

    // 스프라이트는 테마마다 달라져서 갱신 시점마다 다시 받아온다.
    void ApplyThemeSprites()
    {
        if (!ServiceLocator.TryGet<IThemeRepository>(out var repo)) return;

        Img_Locked.sprite = repo.Asset.SprLocked;
        Img_Star1.sprite = repo.Asset.SprStar1;
        Img_Star2.sprite = repo.Asset.SprStar2;
        Img_Star3.sprite = repo.Asset.SprStar3;
    }

    // 같은 스테이지 기록이 여러 번 쌓일 수 있어 가장 높은 별 개수를 고른다.
    static int BestStarsOf(int stageIdx)
    {
        if (!ServiceLocator.TryGet<IUserDataRepository>(out var repo)) return 0;

        int best = 0;
        var clears = repo.Data.StageClears;
        for (int i = 0; i < clears.Count; i++)
        {
            if (clears[i].StageIndex != stageIdx || !clears[i].IsCleared) continue;
            best = Mathf.Max(best, clears[i].BestStars);
        }
        return best;
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
