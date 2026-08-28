using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PPS.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지에 걸린 튜토리얼 프리팹을 순서대로 Instantiate한다.
/// 컷마다 걸린 조건(Condition)이 채워지면 프리팹은 파괴되고
/// 다음 컷으로 넘어간다. 모드 전이는 StageFlow 몫이고
/// 여기는 그것을 지켜보기만 한다.
/// </summary>
public class TutorialViewer : MonoBehaviour
{
    /// TutorialAnchor(Enum)과 실제 UI 자리를 짝지은 표
    [SerializeField] List<AnchorBinding> targets = new();

    [Serializable]
    public struct AnchorBinding
    {
        public TutorialAnchor Anchor;
        public RectTransform Rect;
    }

    static TutorialViewer Instance;

    CancellationTokenSource _cts;

    void Awake()
    {
        if (Instance != null) return;
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void SetStage(int stageIndex)
    {
        if (Instance == null)
        {
            Debug.LogError("[TutorialViewer] 씬에 뷰어가 없다.");
            return;
        }

        Instance.Play(stageIndex);
    }

    public void Play(int stageIndex)
    {
        if (!ServiceLocator.TryGet<IThemeRepository>(out var repo)) return;

        Stop();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(
            this.GetCancellationTokenOnDestroy());

        PlayAll(repo.Asset.Tutorials, stageIndex, _cts.Token).Forget();
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    async UniTaskVoid PlayAll(
        IReadOnlyList<Tutorial> tutorials, int stageIndex, CancellationToken token)
    {
        if (tutorials == null) return;

        foreach (var tutorial in tutorials)
        {
            if (tutorial == null || tutorial.StageIndex != stageIndex) continue;
            await PlayOne(tutorial, token);

            // 버튼을 눌러 넘어온 컷은 StageFlow 가 패널을
            // 갈아 끼우기 전에 돌아온다. 한 프레임 늦춰야
            // 다음 컷이 살아 있는 자리에 붙는다.
            await UniTask.NextFrame(token);
        }
    }

    /// <summary>
    /// 앵커에 물린 자리. 에디터가 컷의 손가락 위치를
    /// 잡을 때도 같은 표를 봐야 해서 열어 둔다.
    /// </summary>
    public RectTransform Find(TutorialAnchor anchor)
    {
        foreach (var binding in targets)
            if (binding.Anchor == anchor) return binding.Rect;

        return null;
    }

    async UniTask PlayOne(Tutorial tutorial, CancellationToken token)
    {
        GameObject spawned = Show(tutorial);

        // 취소로 빠져나가도 스폰한 것은 반드시 지운다.
        try
        {
            await WaitFor(tutorial, token);
        }
        finally
        {
            if (spawned != null) Destroy(spawned);
        }
    }

    /// <summary>
    /// 띄울 것이 없는 컷도 있다 — 그 자리는 조건만
    /// 기다린다(공이 죽기를 기다리는 구간이 그렇다).
    /// </summary>
    GameObject Show(Tutorial tutorial)
    {
        if (tutorial.Prefab == null) return null;

        var target = Find(tutorial.Target);
        if (target == null)
        {
            Debug.LogError(
                $"[TutorialViewer] 앵커가 안 물려 있다: " +
                $"{tutorial.name} → {tutorial.Target}", this);
            return null;
        }

        var spawned = Instantiate(tutorial.Prefab, target);
        if (spawned.transform is RectTransform rect)
            rect.anchoredPosition += tutorial.Offset;

        var gesture = spawned.GetComponent<TutorialGesture>();
        if (gesture != null) gesture.Play(tutorial.Drag);

        return spawned;
    }

    UniTask WaitFor(Tutorial tutorial, CancellationToken token)
    {
        switch (tutorial.Condition)
        {
            case TutorialAdvanceCondition.Press: return WaitForPress(tutorial, token);
            case TutorialAdvanceCondition.DrawingChanged: return WaitForDrawingChange(token);
            case TutorialAdvanceCondition.SimDecided: return WaitForSimDecision(token);
            case TutorialAdvanceCondition.Time: return WaitForTime(tutorial, token);

            // 조건을 늘리고 case 를 빠뜨리면 컴파일은 통과한다.
            // 짚어 주지 않으면 그 컷이 조용히 시간 대기가 된다.
            default:
                Debug.LogError(
                    $"[TutorialViewer] 모르는 조건이라 시간으로 넘긴다: " +
                    $"{tutorial.name} → {tutorial.Condition}", this);

                return WaitForTime(tutorial, token);
        }
    }

    UniTask WaitForTime(Tutorial tutorial, CancellationToken token) =>
        UniTask.Delay(TimeSpan.FromSeconds(tutorial.Duration), cancellationToken: token);

    /// <summary>
    /// 대상 앵커의 버튼이 눌릴 때까지. 버튼이 없으면
    /// 영영 못 넘어가므로 시간으로 물러선다.
    /// </summary>
    async UniTask WaitForPress(Tutorial tutorial, CancellationToken token)
    {
        var target = Find(tutorial.Target);
        var button = target == null ? null : target.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError(
                $"[TutorialViewer] 누를 버튼이 없다: " +
                $"{tutorial.name} → {tutorial.Target}", this);

            await WaitForTime(tutorial, token);
            return;
        }

        // 리스너를 손으로 걸었다 떼면, 버튼이 먼저 파괴된
        // 판에서 finally 가 죽은 참조를 건드린다.
        await button.OnClickAsync(token);
    }

    /// 플레이어가 도구로 무언가 할 때까지.
    async UniTask WaitForDrawingChange(CancellationToken token)
    {
        if (!ServiceLocator.TryGet<ITutorialSignals>(out var signals))
        {
            Debug.LogError("[TutorialViewer] 판을 지켜볼 창구가 없다.", this);
            return;
        }

        var changed = new UniTaskCompletionSource();
        Action listener = () => changed.TrySetResult();

        signals.ToolActed += listener;
        try { await changed.Task.AttachExternalCancellation(token); }
        finally { signals.ToolActed -= listener; }
    }


    UniTask WaitForSimDecision(CancellationToken token)
    {
        if (!ServiceLocator.TryGet<ITutorialSignals>(out var signals))
        {
            Debug.LogError("[TutorialViewer] 판을 지켜볼 창구가 없다.", this);
            return UniTask.CompletedTask;
        }

        return UniTask.WaitUntil(() => signals.SimDecided, cancellationToken: token);
    }
}
