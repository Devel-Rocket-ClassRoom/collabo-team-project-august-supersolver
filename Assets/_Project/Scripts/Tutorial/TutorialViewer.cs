using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PPS.Core;
using UnityEngine;

/// <summary>
/// 스테이지에 걸린 튜토리얼 프리팹을 순서대로 Instantiate한다.
/// 튜토리얼에 붙은 타이머(Duration) 만큼의 지속 후 만료되어 프리팹은 파괴된다.
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
        }
    }

    RectTransform Find(TutorialAnchor anchor)
    {
        foreach (var binding in targets)
            if (binding.Anchor == anchor) return binding.Rect;

        return null;
    }

    async UniTask PlayOne(Tutorial tutorial, CancellationToken token)
    {
        if (tutorial.Prefab == null) return;

        var target = Find(tutorial.Target);
        if (target == null)
        {
            Debug.LogError(
                $"[TutorialViewer] 앵커가 안 물려 있다: " +
                $"{tutorial.name} → {tutorial.Target}", this);
        }

        var spawned = Instantiate(tutorial.Prefab, target);

        // 취소로 빠져나가도 스폰한 것은 반드시 지운다.
        try
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(tutorial.Duration), cancellationToken: token);
        }
        finally
        {
            if (spawned != null) Destroy(spawned);
        }
    }
}
