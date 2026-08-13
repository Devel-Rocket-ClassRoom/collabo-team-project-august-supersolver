using Cysharp.Threading.Tasks;
using DG.Tweening;
using PPS.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>클리어 보상 팝업. 스테이지 표기·별·잉크·스텝을 보여준다.</summary>
public class RewardView : UIPopup, IRewardView
{
    /// 한 챕터에 들어가는 스테이지 수. 1-1 ~ 1-20 표기용.
    private const int StagesPerChapter = 20;

    [Header("Content")]
    [SerializeField] private RectTransform content;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI stageLabel;
    [SerializeField] private TextMeshProUGUI inkUsedLabel;
    [SerializeField] private TextMeshProUGUI clearTimeLabel;

    [Header("Stars")]
    [SerializeField] private Image[] filledStars = new Image[3];

    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button nextStageButton;

    [Header("Animation")]
    [SerializeField] private float slideDistance = 1200f;
    [SerializeField] private float slideDuration = 0.4f;
    [SerializeField] private float starInterval = 0.15f;
    [SerializeField] private float starPunchDuration = 0.25f;

    private RewardViewModel _vm;

    public void Show(RewardViewModel vm)
    {
        _vm = vm;
        UIManager.Instance.ShowPopup<RewardView>().Forget();
    }

    public override void OnBeforeShow()
    {
        Bind(_vm);
        SetButtonsInteractable(false);
    }

    protected override async UniTask OnShowAnimation()
    {
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, -slideDistance);

        // 트윈 길이와 대기 시간이 어긋나지 않게
        // 시퀀스 하나로 묶어 끝날 때까지 기다린다.
        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject);

        sequence.Append(content.DOAnchorPosY(0f, slideDuration).SetEase(Ease.OutCubic));

        int count = Mathf.Clamp(_vm.StarCount, 0, filledStars.Length);

        for (int i = 0; i < count; i++)
        {
            sequence.Insert(
                slideDuration + starInterval * i,
                filledStars[i].transform.DOScale(Vector3.one, starPunchDuration).SetEase(Ease.OutBack));
        }

        await sequence.AsyncWaitForCompletion();

        SetButtonsInteractable(true);
    }

    private void Bind(RewardViewModel vm)
    {
        stageLabel.text = FormatStage(vm.StageIndex);
        inkUsedLabel.text = vm.InkUsed.ToString("F1");
        clearTimeLabel.text = FormatTime(vm.EndStep);

        for (int i = 0; i < filledStars.Length; i++)
        {
            filledStars[i].gameObject.SetActive(i < vm.StarCount);
            filledStars[i].transform.localScale = Vector3.zero;
        }
    }

    /// 시뮬은 고정 dt 로만 진행한다.
    /// 그래서 스텝 수가 곧 경과 시간이다.
    private static string FormatTime(int endStep)
    {
        int seconds = Mathf.RoundToInt(endStep * SimWorld.FixedDt);
        return $"{seconds / 60}:{seconds % 60:00}";
    }

    /// 스테이지 인덱스는 0 부터 시작한다고 본다.
    private static string FormatStage(int stageIndex)
    {
        int chapter = stageIndex / StagesPerChapter + 1;
        int number = stageIndex % StagesPerChapter + 1;
        return $"{chapter} - {number}";
    }

    private void SetButtonsInteractable(bool value)
    {
        retryButton.interactable = value;
        homeButton.interactable = value;
        nextStageButton.interactable = value;
    }
}
