#if UNITY_EDITOR
using PPS.DrawingTool;
using UnityEngine;
using PPS.Core;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor.SceneManagement;
using PPS.Game;
using UnityEngine.EventSystems;
using System.Threading;

namespace PPS.Tools
{
    public class ReplayDebugController : MonoBehaviour
    {
        ReplayDebugPanel _panel;

        const string ReplayScenePath =
    "Assets/_Project/Scenes/RePlay.unity";

        Scene _previousActiveScene;
        Scene _replayScene;

        bool _isTransitioning;
        bool _isReplayOpen;
        CancellationToken _lifetimeToken;

        readonly List<Behaviour> _suspendedBehaviours =
    new List<Behaviour>();

        readonly List<Renderer> _hiddenRenderers =
            new List<Renderer>();

        readonly List<AudioSource> _pausedAudioSources =
            new List<AudioSource>();

        void Awake()
        {
            // 컨트롤러가 파괴되면 취소되는 토큰을 보관한다.
            _lifetimeToken = destroyCancellationToken;
        }

        // 씬에 직접 배치하지 않아도 재생 시작 시 생성한다.
        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            var existing =
                FindFirstObjectByType<ReplayDebugController>(
                    FindObjectsInactive.Include);

            if (existing != null)
                return;

            GameObject controllerObject =
                new GameObject("ReplayDebugController");

            controllerObject.AddComponent<ReplayDebugController>();
            DontDestroyOnLoad(controllerObject);
        }

        void Update()
        {
            // 리플레이에서는 드로잉툴 기준으로 패널을 숨기지 않는다.
            if (_isTransitioning || _isReplayOpen)
                return;

            StageLoader stage =
                FindFirstObjectByType<StageLoader>();

            bool drawingToolOpen =
                stage != null && stage.isActiveAndEnabled;

            if (!drawingToolOpen)
            {
                if (_panel != null)
                    _panel.gameObject.SetActive(false);

                return;
            }

            if (_panel == null)
                CreatePanel();

            if (!_panel.gameObject.activeSelf)
                _panel.gameObject.SetActive(true);

            DrawingSession session =
    FindFirstObjectByType<DrawingSession>();

            bool canSave =
                stage.Stage != null &&
                stage.Stage.Level != null &&
                session != null &&
                session.isActiveAndEnabled;

            StageFlow flow =
    FindFirstObjectByType<StageFlow>();

            DrawInputBehaviour input =
                FindFirstObjectByType<DrawInputBehaviour>();

            bool canReplay =
                canSave &&
                flow != null &&
                flow.Mode == StageMode.Draw &&
                input != null &&
                !input.IsDrawing;

            _panel.SetButtonsEnabled(
                canSave, ReplayDebugCache.HasReplay, canReplay);
        }

        // 표시 오브젝트를 제어 오브젝트와 분리한다.
        void CreatePanel()
        {
            GameObject panelObject = new GameObject(
                "ReplayDebugPanel",
                typeof(RectTransform));

            panelObject.transform.SetParent(transform, false);

            _panel = panelObject.AddComponent<ReplayDebugPanel>();

            _panel.SaveRequested += SaveCurrentReplay;
            _panel.ExportRequested += ExportCachedReplay;

            _panel.ReplayRequested += OpenReplay;
            _panel.ReturnRequested += ReturnToDrawingTool;

            _panel.SetStatus("저장을 누르면 현재 그림을 캐시합니다.");
        }

        // 버튼을 누른 시점의 스테이지와 그림을 캐시한다.
        void SaveCurrentReplay()
        {
            StageLoader stage =
                FindFirstObjectByType<StageLoader>();

            DrawingSession session =
                FindFirstObjectByType<DrawingSession>();

            if (stage == null || !stage.isActiveAndEnabled ||
                session == null || !session.isActiveAndEnabled)
            {
                _panel.ShowError("저장할 드로잉툴을 찾지 못했습니다.");
                return;
            }

            try
            {
                bool saved = ReplayDebugCache.Save(
                    stage.Stage, session.Solution);
                if (saved)
                {
                    _panel.SetStatus("캐시 저장 완료");
                    _panel.ShowCacheSaved();
                }
                else
                {
                    _panel.ShowError(
                        "스테이지 데이터가 준비되지 않았습니다.");
                }
            }
            catch (System.Exception exception)
            {
                _panel.ShowError("캐시 저장 실패: Console을 확인하세요.");
                Debug.LogException(exception);
            }
        }

        // 현재 그림이 아니라 마지막으로 캐시한 내용을 저장한다.
        void ExportCachedReplay()
        {
            if (!ReplayDebugCache.HasReplay)
            {
                _panel.ShowError("먼저 저장 버튼으로 캐시하세요.");
                return;
            }

            string path =
                ReplayStorage.Save(ReplayDebugCache.Current);

            if (string.IsNullOrEmpty(path))
            {
                _panel.ShowError(
                    "JSON 저장 실패: Console을 확인하세요.");
            }
            else
            {
                _panel.SetStatus("JSON 저장 완료");
            }
        }
        // 지정한 씬 안에 있는 재생기만 찾는다.
        SimWorldRenderer FindReplayRenderer(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                SimWorldRenderer renderer =
                    root.GetComponentInChildren<SimWorldRenderer>();

                if (renderer != null && renderer.isActiveAndEnabled)
                    return renderer;
            }

            return null;
        }
        // 원래 화면을 보존한 채 리플레이 씬을 연다.
        async void OpenReplay()
        {
            if (_isTransitioning || _isReplayOpen ||
                !ReplayDebugCache.HasReplay)
                return;

            Scene existing = SceneManager.GetSceneByPath(
                ReplayScenePath);

            if (existing.IsValid() && existing.isLoaded)
            {
                _panel.ShowError("이미 열린 RePlay 씬을 먼저 닫으세요.");
                return;
            }

            _isTransitioning = true;
            _panel.SetBusy(true);
            _panel.SetStatus("리플레이 준비 중");

            try
            {
                // 재생 측에서 캐시 원본을 변경하지 않도록 복사한다.
                ReplayData cached = ReplayDebugCache.Current;
                ReplayData replay = ReplayData.CreateSnapshot(
                    cached.Stage, cached.Solution);

                if (replay == null)
                    throw new System.InvalidOperationException(
                        "재생할 캐시 데이터가 없습니다.");

                _previousActiveScene = SceneManager.GetActiveScene();

                SuspendOriginalRoots();

                System.Exception prepareError = null;

                // Start 실행 전에 로그인용 컴포넌트를 정리한다.
                void OnReplayLoaded(Scene scene, LoadSceneMode mode)
                {
                    if (_lifetimeToken.IsCancellationRequested)
                        return;

                    if (scene.path != ReplayScenePath)
                        return;

                    _replayScene = scene;

                    try
                    {
                        PrepareReplayScene();
                    }
                    catch (System.Exception exception)
                    {
                        prepareError = exception;
                    }
                }

                SceneManager.sceneLoaded += OnReplayLoaded;

                try
                {
                    AsyncOperation operation =
                        EditorSceneManager.LoadSceneAsyncInPlayMode(
                            ReplayScenePath,
                            new LoadSceneParameters(
                                LoadSceneMode.Additive));

                    if (operation == null)
                        throw new System.InvalidOperationException(
                            "리플레이 로드를 시작하지 못했습니다.");

                    while (!operation.isDone)
                    {
                        _lifetimeToken.ThrowIfCancellationRequested();
                        await Task.Yield();
                    }

                    _lifetimeToken.ThrowIfCancellationRequested();

                    _replayScene =
                        SceneManager.GetSceneByPath(ReplayScenePath);

                    if (!_replayScene.IsValid() ||
                        !_replayScene.isLoaded)
                    {
                        throw new System.InvalidOperationException(
                            "RePlay 씬을 불러오지 못했습니다.");
                    }

                    if (prepareError != null)
                        throw prepareError;

                    SceneManager.SetActiveScene(_replayScene);
                }
                finally
                {
                    SceneManager.sceneLoaded -= OnReplayLoaded;
                }

                SimWorldRenderer renderer =
                    FindReplayRenderer(_replayScene);

                if (renderer == null)
                    throw new System.InvalidOperationException(
                        "RePlay 씬에서 활성 재생기를 찾지 못했습니다.");

                renderer.SetReplay(replay.Stage, replay.Solution);
                renderer.Play();

                _isReplayOpen = true;
                _panel.SetReplayMode(true);
                _panel.SetStatus("캐시 리플레이 재생 중");
            }
            catch (System.OperationCanceledException)
            {
                // Play 종료로 중단된 경우에는 오류를 표시하지 않는다.
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);

                // 로드 도중 예외가 나도 생성된 씬을 찾아 정리한다.
                if (!_replayScene.IsValid())
                    _replayScene =
                        SceneManager.GetSceneByPath(ReplayScenePath);

                try
                {
                    await CloseReplayScene();
                    RestoreOriginalRoots();

                    _isReplayOpen = false;
                    _panel.SetReplayMode(false);
                    _panel.ShowError("이동 실패: Console을 확인하세요.");
                }
                catch (System.OperationCanceledException)
                {
                    // Play 종료로 중단된 경우에는 오류를 표시하지 않는다.
                }
                catch (System.Exception cleanupException)
                {
                    // 기존 내용 그대로 유지
                    {
                        Debug.LogException(cleanupException);
                    }

                    // 정리에 실패하면 돌아가기를 다시 시도할 수 있다.
                    _isReplayOpen = true;
                    _panel.SetReplayMode(true);
                    _panel.ShowError("정리 실패: 돌아가기를 다시 누르세요.");
                }
            }
            finally
            {
                _isTransitioning = false;

                if (_panel != null)
                    _panel.SetBusy(false);
            }
        }

        // 리플레이 씬을 내린 뒤 기존 화면을 복구한다.
        async void ReturnToDrawingTool()
        {
            if (_isTransitioning || !_isReplayOpen)
                return;

            _isTransitioning = true;
            _panel.SetBusy(true);
            _panel.SetStatus("드로잉툴로 돌아가는 중");

            try
            {
                await CloseReplayScene();
                RestoreOriginalRoots();

                _isReplayOpen = false;
                _panel.SetReplayMode(false);
                _panel.SetStatus("드로잉툴 복귀 완료");
            }
                catch (System.OperationCanceledException)
                {
                    // Play 종료로 중단된 경우에는 오류를 표시하지 않는다.
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception);
                    _panel.ShowError("복귀 실패: 돌아가기를 다시 누르세요.");
                }
                finally
            {
                _isTransitioning = false;

                if (_panel != null)
                    _panel.SetBusy(false);
            }
        }

        // 오브젝트는 유지하고 출력과 입력만 중단한다.
        void SuspendOriginalRoots()
        {
            _suspendedBehaviours.Clear();
            _hiddenRenderers.Clear();
            _pausedAudioSources.Clear();

            Behaviour[] behaviours = FindObjectsByType<Behaviour>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            foreach (Behaviour item in behaviours)
            {
                if (item == null ||
                    !item.isActiveAndEnabled ||
                    IsDebugObject(item))
                {
                    continue;
                }

                bool shouldSuspend =
                    item is Camera ||
                    item is Canvas ||
                    item is Light ||
                    item is AudioListener ||
                    item is BaseRaycaster ||
                    item is EventSystem ||
                    item is BaseInputModule ||
                    item is DrawInputBehaviour ||
                    item is PPS.Input.MobileInput ||
                    item is GameSimDriver ||
                    item is StageOutcomeHandler;

                if (!shouldSuspend)
                    continue;

                _suspendedBehaviours.Add(item);
                item.enabled = false;
            }

            //기존 맵이 새 카메라에 보이지 않게 한다.
            Renderer[] renderers = FindObjectsByType<Renderer>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            foreach (Renderer item in renderers)
            {
                if (item == null ||
                    IsDebugObject(item) ||
                    item.forceRenderingOff)
                {
                    continue;
                }

                _hiddenRenderers.Add(item);
                item.forceRenderingOff = true;
            }

            AudioSource[] sources = FindObjectsByType<AudioSource>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            foreach (AudioSource source in sources)
            {
                if (source == null ||
                    IsDebugObject(source) ||
                    !source.isPlaying)
                {
                    continue;
                }

                _pausedAudioSources.Add(source);
                source.Pause();
            }
        }
        // 디버그 패널과 컨트롤러는 계속 동작해야 한다.
        bool IsDebugObject(Component item)
        {
            return item.transform == transform ||
                item.transform.IsChildOf(transform);
        }

        // 이번 이동에서 변경한 상태만 복구한다.
        void RestoreOriginalRoots()
        {
            if (_previousActiveScene.IsValid() &&
                _previousActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(_previousActiveScene);
            }

            foreach (Renderer item in _hiddenRenderers)
            {
                if (item != null)
                    item.forceRenderingOff = false;
            }

            foreach (Behaviour item in _suspendedBehaviours)
            {
                if (item != null)
                    item.enabled = true;
            }

            foreach (AudioSource source in _pausedAudioSources)
            {
                if (source != null && source.isActiveAndEnabled)
                    source.UnPause();
            }

            _hiddenRenderers.Clear();
            _suspendedBehaviours.Clear();
            _pausedAudioSources.Clear();
        }

        // 디버그 재생에는 로그인과 저장 테스트를 실행하지 않는다.
        void PrepareReplayScene()
        {
            HideReplayTitleLogo();

            foreach (GameObject root in _replayScene.GetRootGameObjects())
            {
                MonoBehaviour[] behaviours =
                    root.GetComponentsInChildren<MonoBehaviour>(true);

                foreach (MonoBehaviour behaviour in behaviours)
                {
                    if (behaviour == null)
                        continue;

                    if (behaviour is FirebaseAuthService ||
                        behaviour is FirebaseUserDataLoader ||
                        behaviour is GoogleSignInBridge ||
                        behaviour is TitleLoginController ||
                        behaviour is UserDataSaveLoadTest)
                    {
                        behaviour.enabled = false;
                    }

                    if (behaviour is FirebaseAuthView ||
    behaviour is TitleLoginController)
                    {
                        behaviour.gameObject.SetActive(false);
                    }
                }
            }
        }

        // 디버그로 불러온 리플레이 씬의 로고만 숨긴다.
        void HideReplayTitleLogo()
        {
            if (!_replayScene.IsValid() ||
                !_replayScene.isLoaded ||
                _replayScene.path != ReplayScenePath)
            {
                return;
            }

            foreach (GameObject root in _replayScene.GetRootGameObjects())
            {
                if (root.name != "ReplayCanvas")
                    continue;

                Transform logo =
                    root.transform.Find("YummyDrawerTitleUI");

                if (logo != null)
                {
                    logo.gameObject.SetActive(false);
                    return;
                }
            }

            Debug.LogWarning(
                "RePlay 씬에서 ReplayCanvas/YummyDrawerTitleUI를 찾지 못했습니다.");
        }

        // 씬 제거가 끝나야 원래 화면을 다시 활성화한다.
        async Task CloseReplayScene()
        {
            if (_previousActiveScene.IsValid() &&
                _previousActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(_previousActiveScene);
            }

            if (_replayScene.IsValid() && _replayScene.isLoaded)
            {
                AsyncOperation operation =
                    SceneManager.UnloadSceneAsync(_replayScene);

                if (operation == null)
                    throw new System.InvalidOperationException(
                        "리플레이 씬 종료를 시작하지 못했습니다.");

                while (!operation.isDone)
                {
                    _lifetimeToken.ThrowIfCancellationRequested();
                    await Task.Yield();
                }

                _lifetimeToken.ThrowIfCancellationRequested();

                if (_replayScene.IsValid() && _replayScene.isLoaded)
                    throw new System.InvalidOperationException(
                        "리플레이 씬이 아직 열려 있습니다.");
            }

            _replayScene = default;
        }
    }
}
#endif