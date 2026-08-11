using System.IO;
using PPS.Core;
using PPS.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PPS.UI
{
    // 저장된 리플레이 파일을 읽어 스크롤 목록에 버튼으로 표시한다.
    public class ReplayListUI : MonoBehaviour
    {
        // 선택된 ReplayData를 실제 물리 월드로 만들 재생기다.
        [SerializeField] SimWorldRenderer _renderer;

        // 자동으로 생성된 리플레이 버튼들이 들어갈 부모다.
        [SerializeField] Transform _content;

        // 목록 버튼을 복제할 때 사용할 원본 버튼이다.
        [SerializeField] Button _buttonTemplate;

        // 씬이 시작되면 저장된 리플레이 목록을 생성한다.
        void Start()
        {
            Refresh();
        }

        // Replays 폴더를 다시 읽어 목록 버튼을 생성한다.
        public void Refresh()
        {
            // Inspector 연결이 빠졌다면 목록을 만들 수 없다.
            if (_renderer == null ||
                _content == null ||
                _buttonTemplate == null)
            {
                Debug.LogWarning(
                    "ReplayListUI의 Renderer, Content, Button Template을 연결해야 합니다.");

                return;
            }

            // 이전에 자동으로 생성했던 버튼들을 제거한다.
            ClearGeneratedButtons();

            // 저장된 리플레이 파일 경로를 최신순으로 가져온다.
            string[] filePaths = ReplayStorage.GetReplayFiles();

            // 저장된 리플레이 파일을 하나씩 확인한다.
            for (int i = 0; i < filePaths.Length; i++)
            {
                // 현재 리플레이 파일 경로를 가져온다.
                string filePath = filePaths[i];

                // 손상되었거나 지원하지 않는 파일은 목록에서 제외한다.
                if (!ReplayStorage.TryLoad(filePath, out ReplayData replay))
                    continue;

                // 원본 버튼을 복제하여 Content 아래에 배치한다.
                Button button = Instantiate(_buttonTemplate, _content);

                // 복제한 버튼을 화면에 표시한다.
                button.gameObject.SetActive(true);

                // 버튼에 표시할 TMP 텍스트를 찾는다.
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();

                // 텍스트 컴포넌트가 있다면 리플레이 정보를 표시한다.
                if (label != null)
                {
                    // 파일 이름에서 확장자를 제외한다.
                    string fileName =
                        Path.GetFileNameWithoutExtension(filePath);

                    // 스테이지 이름, 파일명 표시
                    label.text = $"{replay.Stage.StageId} - {fileName}";
                }

                // 버튼 Template에 있던 기존 클릭 연결을 제거한다.
                button.onClick.RemoveAllListeners();

                // 각 버튼이 자신에게 해당하는 ReplayData를 기억하게 한다.
                ReplayData selectedReplay = replay;

                // 버튼 클릭 시 해당 리플레이를 재생기에 전달한다.
                button.onClick.AddListener(
                    () => SelectReplay(selectedReplay));
            }

            // 원본 Template 버튼은 목록에 표시하지 않는다.
            _buttonTemplate.gameObject.SetActive(false);
        }

        // 선택한 ReplayData를 SimWorldRenderer에 전달한다.
        void SelectReplay(ReplayData replay)
        {
            // 재생에 필요한 데이터가 없으면 중단한다.
            if (replay == null ||
                replay.Stage == null ||
                replay.Solution == null)
            {
                return;
            }

            // StageData와 Solution으로 리플레이 월드를 생성한다.
            _renderer.SetReplay(
                replay.Stage,
                replay.Solution);

            // 어떤 리플레이가 선택되었는지 Console에 표시한다.
            Debug.Log(
                $"리플레이 선택 완료: {replay.Stage.StageId}");
        }

        // 이전에 자동 생성된 목록 버튼을 제거한다.
        void ClearGeneratedButtons()
        {
            // Content의 마지막 자식부터 역순으로 확인한다.
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                // 현재 자식 Transform을 가져온다.
                Transform child = _content.GetChild(i);

                // 원본 Template 버튼은 삭제하지 않는다.
                if (child == _buttonTemplate.transform)
                    continue;

                // 이전에 생성된 버튼을 제거한다.
                Destroy(child.gameObject);
            }
        }
    }
}
