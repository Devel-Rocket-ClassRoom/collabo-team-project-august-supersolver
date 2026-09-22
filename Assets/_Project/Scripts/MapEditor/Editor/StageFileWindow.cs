using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using PPS.Core;
using UnityEditor;
using UnityEngine;

namespace PPS.MapEditor.Dev
{
    /// <summary>
    /// Levels 폴더의 판 이름이 규칙에 맞는지 보고, 어긋난 것을
    /// 규칙대로 고친다. 이름을 바꿀 때 판 안의 ID 와 EditorJson
    /// 의 도형 파일도 함께 간다 — 셋 중 하나만 어긋나도
    /// 맵 에디터가 짝을 거른다.
    /// </summary>
    public class StageFileWindow : EditorWindow
    {
        static readonly Color OkColor = new Color(0.25f, 0.75f, 0.3f);
        static readonly Color WarnColor = new Color(0.85f, 0.7f, 0.15f);
        static readonly Color BadColor = new Color(0.85f, 0.25f, 0.25f);

        readonly List<Row> _rows = new List<Row>();

        /// 도형 파일이 없는 판. 맵 에디터가 지형에서 도형을 새로 굽는다.
        readonly List<string> _shapeMissing = new List<string>();

        /// 판이 없는 도형 파일. 지운 판이 남긴 찌꺼기다.
        readonly List<string> _shapeOrphan = new List<string>();

        Vector2 _scroll;

        class Row
        {
            public string Path;
            public string FileName;
            public string Expected;
            public bool HasSolution;

            public bool NameMatches => FileName == Expected;
        }

        [MenuItem("Tools/맵 에디터/스테이지 파일 이름 검사")]
        static void Open() => GetWindow<StageFileWindow>("스테이지 파일");

        void OnEnable() => Scan();

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("다시 검사", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                    Scan();

                GUILayout.FlexibleSpace();
                GUILayout.Label($"판 {_rows.Count}개", EditorStyles.miniLabel);
            }

            using (var scope = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scope.scrollPosition;

                for (int i = 0; i < _rows.Count; i++) DrawRow(_rows[i]);

                DrawPairSection("도형 파일이 없는 판", _shapeMissing);
                DrawPairSection("판이 없는 도형 파일", _shapeOrphan);
            }
        }

        /// <summary>
        /// 이름이 아니라 짝이 어긋난 것들. 어느 쪽을 지우거나
        /// 만들지는 사람이 정해야 해서 버튼을 두지 않는다.
        /// </summary>
        void DrawPairSection(string title, List<string> names)
        {
            if (names.Count == 0) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"{title} ({names.Count})", EditorStyles.boldLabel);

            for (int i = 0; i < names.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var dot = GUILayoutUtility.GetRect(10f, 10f, GUILayout.Width(10f), GUILayout.Height(10f));
                    dot.y += 3f;
                    EditorGUI.DrawRect(dot, BadColor);

                    GUILayout.Label(names[i]);
                }
            }
        }

        void DrawRow(Row row)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var dot = GUILayoutUtility.GetRect(10f, 10f, GUILayout.Width(10f), GUILayout.Height(10f));
                dot.y += 3f;
                EditorGUI.DrawRect(dot, ColorOf(row));

                GUILayout.Label(row.FileName);

                if (!row.NameMatches && GUILayout.Button("이름 변경", GUILayout.Width(80f)))
                {
                    Rename(row);
                    GUIUtility.ExitGUI();
                }
            }

            if (!row.NameMatches)
            {
                Note($"기대: {row.Expected}");
                Note($"실제: {row.FileName}");
            }

            if (!row.HasSolution) Note("풀이 토큰이 없다 — 이름 변경으로는 채울 수 없다.");
        }

        static void Note(string text)
        {
            EditorGUILayout.LabelField(" ", text, EditorStyles.miniLabel);
        }

        static Color ColorOf(Row row)
        {
            if (!row.NameMatches) return BadColor;
            return row.HasSolution ? OkColor : WarnColor;
        }

        void Scan()
        {
            _rows.Clear();
            _shapeMissing.Clear();
            _shapeOrphan.Clear();

            if (!Directory.Exists(MapFile.Folder))
            {
                Debug.LogWarning($"[스테이지 이름] 폴더가 없다: {MapFile.Folder}");
                return;
            }

            // 도형 파일은 하위 폴더에 있다. 판만 본다.
            string[] paths = Directory.GetFiles(MapFile.Folder, "*.json", SearchOption.TopDirectoryOnly);
            Array.Sort(paths, StringComparer.Ordinal);

            for (int i = 0; i < paths.Length; i++)
            {
                StageData stage;
                try
                {
                    if (!MapFile.TryLoad(paths[i], out stage)) continue;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[스테이지 이름] 읽지 못했다: {paths[i]} — {e.Message}");
                    continue;
                }

                string fileName = Path.GetFileNameWithoutExtension(paths[i]);

                _rows.Add(new Row
                {
                    Path = paths[i].Replace('\\', '/'),
                    FileName = fileName,
                    Expected = StageNameConvention.Expected(fileName, stage.Level),
                    HasSolution = StageNameConvention.HasSolution(fileName),
                });
            }

            ScanPairs();
            Repaint();
        }

        /// <summary>
        /// 판과 도형 파일의 짝을 맞춘다. 한쪽만 있으면 맵 에디터가
        /// 짝을 거르고 지형에서 도형을 새로 구워 편집 정보가 날아간다.
        /// </summary>
        void ScanPairs()
        {
            string shapeFolder = $"{MapFile.Folder}/{MapFile.ShapeFolderName}";

            var shapes = new HashSet<string>(StringComparer.Ordinal);
            if (Directory.Exists(shapeFolder))
            {
                string[] paths = Directory.GetFiles(shapeFolder, "*.edit.json", SearchOption.TopDirectoryOnly);
                for (int i = 0; i < paths.Length; i++)
                {
                    // 확장자가 두 겹이라 한 번 더 벗긴다.
                    shapes.Add(Path.GetFileNameWithoutExtension(
                        Path.GetFileNameWithoutExtension(paths[i])));
                }
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                if (shapes.Remove(_rows[i].FileName)) continue;

                _shapeMissing.Add(_rows[i].FileName);
            }

            _shapeOrphan.AddRange(shapes);
            _shapeOrphan.Sort(StringComparer.Ordinal);
        }

        void Rename(Row row)
        {
            string stageTarget = MapFile.PathOf(row.Expected);
            if (File.Exists(stageTarget))
            {
                Debug.LogError($"[스테이지 이름] 같은 이름의 판이 이미 있다: {stageTarget}");
                return;
            }

            string shapePath = MapFile.ShapePathOf(row.FileName);
            string shapeTarget = MapFile.ShapePathOf(row.Expected);
            bool hasShape = File.Exists(shapePath);

            if (hasShape && File.Exists(shapeTarget))
            {
                Debug.LogError($"[스테이지 이름] 같은 이름의 도형 파일이 이미 있다: {shapeTarget}");
                return;
            }

            if (!hasShape)
                Debug.LogWarning($"[스테이지 이름] 도형 파일이 없다: {shapePath}");

            if (!ReplaceStageId(row.Path, row.Expected)) return;
            if (hasShape && !ReplaceStageId(shapePath, row.Expected)) return;

            string error = AssetDatabase.RenameAsset(row.Path, row.Expected);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"[스테이지 이름] 판 이름을 바꾸지 못했다: {error}");
                return;
            }

            // 도형 파일은 확장자가 두 겹이라 앞쪽까지 넘겨야 한다.
            if (hasShape)
            {
                error = AssetDatabase.RenameAsset(shapePath, row.Expected + ".edit");
                if (!string.IsNullOrEmpty(error))
                    Debug.LogError($"[스테이지 이름] 도형 파일 이름을 바꾸지 못했다: {error}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[스테이지 이름] {row.FileName} → {row.Expected}");

            Scan();
        }

        /// <summary>
        /// 파일 안의 StageId 한 자리만 고친다. 전체를 다시 찍으면
        /// 좌표 표기까지 바뀌어 diff 를 믿을 수 없다.
        /// </summary>
        static bool ReplaceStageId(string path, string stageId)
        {
            try
            {
                string json = File.ReadAllText(path);
                string replaced = new Regex("\"StageId\"\\s*:\\s*\"[^\"]*\"")
                    .Replace(json, $"\"StageId\": \"{stageId}\"", 1);

                if (replaced == json)
                {
                    Debug.LogError($"[스테이지 이름] StageId 를 찾지 못했다: {path}");
                    return false;
                }

                File.WriteAllText(path, replaced);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[스테이지 이름] StageId 를 고치지 못했다: {path} — {e.Message}");
                return false;
            }
        }
    }
}
