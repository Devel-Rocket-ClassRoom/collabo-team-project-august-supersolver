using System;
using System.Collections.Generic;
using System.IO;
using PPS.Core;
using UnityEditor;
using UnityEngine;

namespace PPS.MapEditor.Dev
{
    /// <summary>
    /// Levels 폴더의 판을 현재 저장 형식으로 올려 다시 쓴다.
    /// 읽을 때 올리는 것만으로도 게임은 돌지만, 저장소의 파일은
    /// 편집해서 저장한 판만 바뀌어 형식이 오래 섞여 있게 된다.
    ///
    /// 읽는 길에 끼우지 않고 메뉴로 둔 것은 언제 무엇이 바뀌는지
    /// 보이게 하려는 것이다 — 뷰어를 한 번 여는 것만으로
    /// 작업 폴더가 더러워지면 diff 를 믿을 수 없다.
    /// </summary>
    public static class LevelFormatUpgrade
    {
        [MenuItem("Tools/맵 에디터/레벨 파일을 현재 형식으로 올리기")]
        static void Run()
        {
            if (!Directory.Exists(MapFile.Folder))
            {
                Debug.LogWarning($"[레벨 형식] 폴더가 없다: {MapFile.Folder}");
                return;
            }

            string[] paths = Directory.GetFiles(MapFile.Folder, "*.json", SearchOption.AllDirectories);
            Array.Sort(paths, StringComparer.Ordinal);

            var upgraded = new List<string>();
            var failed = new List<string>();
            int skipped = 0;

            for (int i = 0; i < paths.Length; i++)
            {
                switch (Upgrade(paths[i], out string problem))
                {
                    case Result.Upgraded:
                        upgraded.Add(Path.GetFileName(paths[i]));
                        break;

                    case Result.Failed:
                        failed.Add($"{Path.GetFileName(paths[i])} — {problem}");
                        break;

                    default:
                        skipped++;
                        break;
                }
            }

            if (upgraded.Count > 0) AssetDatabase.Refresh();

            Debug.Log(
                $"[레벨 형식] 올린 판 {upgraded.Count}개, 그대로 둔 파일 {skipped}개" +
                (upgraded.Count > 0 ? "\n  " + string.Join("\n  ", upgraded) : ""));

            // 못 읽은 것은 조용히 넘기지 않는다. 판 하나가 빠진 채
            // 다 됐다고 여기면 그게 제일 나쁘다.
            if (failed.Count > 0)
                Debug.LogError("[레벨 형식] 올리지 못한 파일:\n  " + string.Join("\n  ", failed));
        }

        enum Result
        {
            Skipped,
            Upgraded,
            Failed,
        }

        static Result Upgrade(string path, out string problem)
        {
            problem = null;

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                problem = e.Message;
                return Result.Failed;
            }

            // 올리기 전의 형식 번호를 봐야 한다.
            // StageData.FromJson 은 읽으면서 이미 올려 버린다.
            StageData raw;
            try
            {
                raw = JsonUtility.FromJson<StageData>(json);
            }
            catch (Exception e)
            {
                problem = $"json 이 아니다 — {e.Message}";
                return Result.Failed;
            }

            // 풀이·도형 파일도 같은 폴더에 있다. 판이 아니면 건드리지 않는다.
            if (raw?.Level?.Terrain == null || raw.Level.Terrain.Count == 0) return Result.Skipped;

            if (raw.Version >= StageData.CurrentVersion) return Result.Skipped;

            try
            {
                File.WriteAllText(path, StageData.FromJson(json).ToJson());
            }
            catch (Exception e)
            {
                problem = e.Message;
                return Result.Failed;
            }

            return Result.Upgraded;
        }
    }
}
