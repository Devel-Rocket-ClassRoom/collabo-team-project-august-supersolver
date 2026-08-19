using PPS.Core;
using UnityEditor;
using UnityEngine;

namespace PPS.DrawingTool.Dev
{
    /// <summary>
    /// 지금 그린 그림을 리플레이로 남긴다. 그림이 파일로
    /// 나가는 유일한 길이다 — 전이 핸들러에 붙여 두면 Play
    /// 마다 AssetDatabase 가 갱신되고, 좋은 판 하나가 아니라
    /// 시행착오 전부가 쌓인다.
    /// 인스펙터 버튼과 메뉴 둘 다에서 부른다. 에디터
    /// 어셈블리라 빌드에는 흔적이 없다.
    /// </summary>
    [CustomEditor(typeof(DrawingToolComposite))]
    public class DrawingToolCompositeInspector : Editor
    {
        /// <summary>
        /// 조립자를 찾아 고르지 않아도 되게 메뉴에도
        /// 둔다. 저장은 무엇을 선택했느냐와 상관없는 일이다.
        /// </summary>
        [MenuItem("Tools/드로잉툴/리플레이 저장")]
        static void SaveReplayFromMenu() => SaveReplay();

        /// 재생 중이 아니면 저장할 판이 없다 — 메뉴를 흐린다.
        [MenuItem("Tools/드로잉툴/리플레이 저장", true)]
        static bool CanSaveReplay() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            // 판도 그림도 재생 중에만 있다. 코어는 Awake 에서
            // 서고 판은 스테이지를 고를 때 물린다.
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("리플레이 저장 → Assets/_Project/Replays"))
                    SaveReplay();
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("재생 중에만 저장한다.", MessageType.None);
        }

        /// <summary>
        /// 씬에서 조립자를 직접 찾는다. 코어는 MonoBehaviour 가
        /// 아니라 타입으로 못 찾는다 — 주인을 거쳐 든다.
        /// </summary>
        static void SaveReplay()
        {
            var world = FindFirstObjectByType<DrawingToolComposite>();

            if (world == null || world.Stages == null || world.Stages.Stage == null)
            {
                Debug.LogWarning("저장할 판이나 그림이 없다.");
                return;
            }

            // 저장 경로는 ReplayStorage 가 직접 로그로 남긴다.
            ReplayStorage.Save(world.Stages.Stage, world.Session.Solution);
        }
    }
}
