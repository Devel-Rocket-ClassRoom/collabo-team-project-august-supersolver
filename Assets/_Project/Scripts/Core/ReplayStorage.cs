using System;
using System.IO;
using UnityEngine;

namespace PPS.Core
{
    // 완성된 StageData와 Solution을 하나의 리플레이 파일로 저장한다.
    public static class ReplayStorage
    {
        // 리플레이 JSON을 저장할 프로젝트 내부 상대 경로다.
        const string RelativeFolder = "_Project/Replays";

        // 실제 리플레이 저장 폴더의 전체 경로를 반환한다.
        public static string FolderPath =>
            Path.Combine(Application.dataPath, RelativeFolder);

        // StageData와 Solution을 하나의 리플레이 JSON 파일로 저장한다.
        public static string Save(
            StageData stage,
            Solution solution)
        {
            // StageData와 Solution을 하나의 ReplayData로 묶는다.
            ReplayData replay = ReplayData.Create(stage, solution);

            // 필수 데이터가 없으면 저장하지 않는다.
            if (replay == null)
            {
                Debug.LogWarning(
                    "리플레이 저장에는 StageData와 Solution이 필요합니다.");

                return string.Empty;
            }

            // Replays 폴더가 없다면 새로 만든다.
            Directory.CreateDirectory(FolderPath);

            // 같은 스테이지의 리플레이도 중복 저장할 수 있도록 시간을 포함한다.
            string fileName =
                $"Replay_{DateTime.Now:yyyyMMdd_HHmmssfff}.json";

            // 저장 폴더와 파일명을 하나의 전체 경로로 결합한다.
            string filePath = Path.Combine(FolderPath, fileName);

            // StageData와 Solution이 합쳐진 ReplayData를 JSON으로 변환한다.
            string json = replay.ToJson();

            // 완성된 JSON 문자열을 파일로 저장한다.
            File.WriteAllText(filePath, json);

#if UNITY_EDITOR
            // 새로 생성된 JSON이 Unity Project 창에 바로 표시되도록 갱신한다.
            UnityEditor.AssetDatabase.Refresh();
#endif

            // 저장된 위치를 Console에서 확인한다.
            Debug.Log($"리플레이 저장 완료: {filePath}");

            // 다른 코드에서도 저장 위치를 사용할 수 있도록 경로를 반환한다.
            return filePath;
        }
        // 저장된 리플레이 JSON 파일 경로를 최신순으로 반환한다.
        public static string[] GetReplayFiles()
        {
            // 아직 리플레이 폴더가 없다면 저장된 파일도 없다.
            if (!Directory.Exists(FolderPath))
                return Array.Empty<string>();

            // Replay_로 시작하는 모든 JSON 파일 경로를 가져온다.
            string[] filePaths =
                Directory.GetFiles(FolderPath, "Replay_*.json");

            // 파일명의 날짜와 시간이 오래된 순서로 정렬한다.
            Array.Sort(filePaths, StringComparer.Ordinal);

            // UI에는 가장 최근 리플레이가 먼저 나오도록 순서를 뒤집는다.
            Array.Reverse(filePaths);

            // 정렬된 전체 파일 경로를 반환한다.
            return filePaths;
        }

        // 지정된 JSON 파일에서 ReplayData를 복원한다.
        public static bool TryLoad(
            string filePath,
            out ReplayData replay)
        {
            // 실패할 경우를 대비해 결과를 먼저 비운다.
            replay = null;

            // 경로가 비어 있거나 파일이 존재하지 않으면 읽을 수 없다.
            if (string.IsNullOrWhiteSpace(filePath) ||
                !File.Exists(filePath))
            {
                return false;
            }

            try
            {
                // 리플레이 JSON 파일의 전체 내용을 읽는다.
                string json = File.ReadAllText(filePath);

                // JSON 안의 StageData와 Solution을 함께 복원한다.
                replay = ReplayData.FromJson(json);

                // JSON을 ReplayData로 변환하지 못했다면 실패다.
                if (replay == null)
                    return false;

                // 현재 코드가 지원하는 저장 형식인지 확인한다.
                if (replay.Version != ReplayData.CurrentVersion)
                {
                    Debug.LogWarning(
                        $"지원하지 않는 리플레이 버전입니다: {replay.Version}");

                    replay = null;
                    return false;
                }

                // 재생에 필요한 StageData와 LevelData가 있는지 확인한다.
                if (replay.Stage == null || replay.Stage.Level == null)
                {
                    Debug.LogWarning(
                        $"리플레이에 StageData가 없습니다: {filePath}");

                    replay = null;
                    return false;
                }

                // 재생에 필요한 Solution이 있는지 확인한다.
                if (replay.Solution == null)
                {
                    Debug.LogWarning(
                        $"리플레이에 Solution이 없습니다: {filePath}");

                    replay = null;
                    return false;
                }

                // 필요한 데이터를 모두 읽었으므로 성공이다.
                return true;
            }
            catch (Exception exception)
            {
                // 파일 손상이나 읽기 실패가 전체 목록을 중단시키지 않도록 처리한다.
                Debug.LogWarning(
                    $"리플레이 파일을 읽지 못했습니다: {filePath}\n{exception.Message}");

                replay = null;
                return false;
            }
        }
    }
}