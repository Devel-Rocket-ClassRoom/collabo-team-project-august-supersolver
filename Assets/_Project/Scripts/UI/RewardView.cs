using Cysharp.Threading.Tasks;
using DG.Tweening;
using PPS.Core;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>클리어 보상 팝업. 스테이지 표기·별·잉크·스텝을 보여준다.</summary>
/// 
public class RewardView : UIPopup, IRewardView
{
    /// 한 챕터에 들어가는 스테이지 수. 1-1 ~ 1-20 표기용.
    private const int StagesPerChapter = CurrentStageIndex.StagePerTheme;

    [Header("Content")]
    [SerializeField] private RectTransform content;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI stageLabel;
    [SerializeField] private TextMeshProUGUI inkLeftLabel;
    [SerializeField] private TextMeshProUGUI clearTimeLabel;

    [Header("Ink Bar")]
    /// 잉크 잔량 바.
    [SerializeField] private Slider inkBar;

    /// 등급이 갈리는 지점을 짚는 눈금. 각각 동↔은, 은↔금 경계다.
    [SerializeField] private RectTransform bronzeTick;
    [SerializeField] private RectTransform silverTick;

    [Header("Stars")]
    [SerializeField] private Image[] filledStars = new Image[3];

    [Header("Ink Rank")]
    /// 잉크를 아낀 정도에 따라 갈아 끼울 별.
    [SerializeField] private Sprite goldStar;
    [SerializeField] private Sprite silverStar;
    [SerializeField] private Sprite bronzeStar;

    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button nextStageButton;

    [Header("Animation")]
    [SerializeField] private float slideDistance = 1200f;
    [SerializeField] private float slideDuration = 0.4f;
    [SerializeField] private float starInterval = 0.15f;
    [SerializeField] private float starPunchDuration = 0.25f;
    [SerializeField] private float inkDrainDuration = 0.6f;

    private RewardViewModel _vm;

    /// 잉크 바가 줄어들다 멈출 지점. 남은 잉크 비율이다.
    private float _inkLeft;

    public void Show(RewardViewModel vm)
    {
        _vm = vm;
        UIManager.Instance.ShowPopup<RewardView>().Forget();
    }

    public void Hide() => UIManager.Instance.HidePopup(true).Forget();
    public void BindButtonListener(Action retry, Action home, Action next)
    {
        if (retry != null)
            retryButton.onClick.AddListener(new UnityAction(retry));
        if (home != null)
            homeButton.onClick.AddListener(new UnityAction(home));
        if (next != null)
            nextStageButton.onClick.AddListener(new UnityAction(next));
    }
    private void OnDestroy()
    {
        retryButton.onClick.RemoveAllListeners();
        homeButton.onClick.RemoveAllListeners();
        nextStageButton.onClick.RemoveAllListeners();
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

        // 가득 찬 바가 줄어드는 걸 봐야 얼마나 썼는지 읽힌다.
        sequence.Insert(
            slideDuration,
            inkBar.DOValue(_inkLeft, inkDrainDuration).SetEase(Ease.OutCubic));

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
        clearTimeLabel.text = FormatTime(vm.EndStep);

        float left = Mathf.Max(0f, vm.InkLimit - vm.InkUsed);
        _inkLeft = vm.InkLimit > 0f ? Mathf.Clamp01(left / vm.InkLimit) : 0f;
        inkLeftLabel.text = $"{Mathf.RoundToInt(_inkLeft * 100f)}%";

        // 트윈이 가득 찬 데서 출발하도록 미리 채워 둔다.
        inkBar.value = 1f;

        PlaceTick(bronzeTick, InkGrade.Bronze);
        PlaceTick(silverTick, InkGrade.Silver);

        Sprite star = RankStar(vm);

        for (int i = 0; i < filledStars.Length; i++)
        {
            if (star != null) filledStars[i].sprite = star;

            filledStars[i].gameObject.SetActive(i < vm.StarCount);
            filledStars[i].transform.localScale = Vector3.zero;
        }
    }

    /// <summary>등급이 grade 위로 올라서는 잔량 지점에 눈금을 세운다.</summary>
    /// 기준값을 뷰가 복사해 두면 규칙이 바뀔 때 조용히 어긋난다.
    /// 그래서 InkGrade 에 직접 물어 경계를 찾는다.
    private static void PlaceTick(RectTransform tick, int grade)
    {
        float used = UsedRatioAtGradeDrop(grade);

        // 잔량 바라 사용량 축을 뒤집어야 눈금이 제자리에 선다.
        Vector2 min = tick.anchorMin;
        Vector2 max = tick.anchorMax;

        tick.anchorMin = new Vector2(1f - used, min.y);
        tick.anchorMax = new Vector2(1f - used, max.y);
        tick.anchoredPosition = new Vector2(0f, tick.anchoredPosition.y);
    }

    /// grade 를 유지하는 최대 사용량 비율. 이분 탐색으로 좁힌다.
    private static float UsedRatioAtGradeDrop(int grade)
    {
        float low = 0f;
        float high = 1f;

        for (int i = 0; i < 24; i++)
        {
            float mid = (low + high) * 0.5f;

            if (InkGrade.Of(mid, 1f) > grade) low = mid;
            else high = mid;
        }

        return high;
    }

    /// <summary>잉크를 적게 쓸수록 좋은 별이 온다.</summary>
    private Sprite RankStar(RewardViewModel vm)
    {
        switch (InkGrade.Of(vm.InkUsed, vm.InkLimit))
        {
            case InkGrade.Gold: return goldStar;
            case InkGrade.Silver: return silverStar;
            default: return bronzeStar;
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
        (int chapter, int number) = CurrentStageIndex.GetThemeAndStageNumber(stageIndex);
        return $"{chapter} - {number}";
    }

    private void SetButtonsInteractable(bool value)
    {
        retryButton.interactable = value;
        homeButton.interactable = value;
        nextStageButton.interactable = value;
    }
}
