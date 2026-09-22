using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 인스펙터에서 고른 UIScene 으로 이동하는 버튼.
/// 전환이 끝날 때까지 모든 이동 버튼의 입력을 막는다.
/// 전환 도중 다시 눌리면 현재 씬 참조가 어긋난다.
/// </summary>
public class MoveSceneButton : MonoBehaviour
{
    private enum SceneTarget
    {
        StageSelect,
        ThemeSelect,
        DrawingTool,
    }

    [SerializeField] private SceneTarget target;

    private static bool _moving;

    public void OnClick()
    {
        if (_moving) return;

        Move().Forget();
    }

    private async UniTaskVoid Move()
    {
        _moving = true;

        try
        {
            switch (target)
            {
                case SceneTarget.StageSelect:
                    await UIManager.Instance.ShowScene<StageSelectView>();
                    break;
                case SceneTarget.ThemeSelect:
                    await UIManager.Instance.ShowScene<ThemeSelectView>();
                    break;
                case SceneTarget.DrawingTool:
                    await UIManager.Instance.ShowScene<DrawingToolSceneUI>();
                    break;
            }
        }
        finally
        {
            _moving = false;
        }
    }
}
