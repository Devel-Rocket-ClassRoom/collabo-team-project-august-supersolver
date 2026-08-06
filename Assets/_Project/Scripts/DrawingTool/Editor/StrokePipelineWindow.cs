using System.Collections.Generic;
using PPS.Core;
using UnityEditor;
using UnityEngine;

namespace PPS.DrawingTool.Dev
{
    /// <summary>
    /// 마우스로 그어 파이프라인 결과를 눈으로 본다.
    /// 원본·필터·최종을 겹쳐 그린다. 씬과 카메라가
    /// 아직 없어도 상수 감각을 잡을 수 있다.
    /// </summary>
    public class StrokePipelineWindow : EditorWindow
    {
        /// 설계 박스. 이 안에서만 레벨을 만든다.
        const float BoxWidth = 8f;
        const float BoxHeight = 12f;

        [SerializeField] List<Vector2> _raw = new List<Vector2>();
        [SerializeField] float _inkLimit = 20f;

        Rect _canvas;
        float _scale;
        bool _drawing;

        [MenuItem("Tools/드로잉툴/궤적 파이프라인")]
        static void Open() => GetWindow<StrokePipelineWindow>("궤적 파이프라인");

        void OnGUI()
        {
            DrawToolbar();

            _canvas = GUILayoutUtility.GetRect(0f, 100000f, 0f, 100000f);
            _scale = Mathf.Min(_canvas.width / BoxWidth, _canvas.height / BoxHeight) * 0.92f;

            // 레이아웃이 아직 안 잡힌 프레임에서 좌표
            // 변환이 0 으로 나눈다.
            if (_scale <= 0f) return;

            HandleMouse();

            var builder = new StrokeBuilder();
            builder.Begin(_inkLimit);
            for (int i = 0; i < _raw.Count; i++)
                builder.AddPoint(_raw[i]);

            var stroke = new StrokeProcessor().Process(ToolType.FixedLine, builder.Points);

            if (Event.current.type == EventType.Repaint)
                Draw(builder.Points, stroke);

            DrawStats(builder, stroke);
        }

        void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("잉크 상한", EditorStyles.miniLabel, GUILayout.Width(56f));
                _inkLimit = EditorGUILayout.Slider(_inkLimit, 0.5f, 40f);

                if (GUILayout.Button("지우기", EditorStyles.toolbarButton, GUILayout.Width(56f)))
                {
                    _raw.Clear();
                    Repaint();
                }
            }
        }

        void HandleMouse()
        {
            Event e = Event.current;
            bool inside = _canvas.Contains(e.mousePosition);

            if (e.type == EventType.MouseDown && e.button == 0 && inside)
            {
                _raw.Clear();
                _drawing = true;
                _raw.Add(ToWorld(e.mousePosition));
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseDrag && _drawing)
            {
                // 캔버스 밖에서는 점을 받지 않는다.
                // 다시 들어오면 이어진다 (작업 5 규칙).
                if (inside) _raw.Add(ToWorld(e.mousePosition));
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseUp && _drawing)
            {
                _drawing = false;
                e.Use();
                Repaint();
            }
        }

        void Draw(List<Vector2> filtered, Stroke stroke)
        {
            EditorGUI.DrawRect(_canvas, new Color(0.16f, 0.16f, 0.18f));

            // 박스 밖 여백은 색을 달리해 "여기는 못 그린다"를
            // 알린다. 클램프 기준이 캔버스가 아니라 박스다.
            Vector2 topLeft = ToScreen(new Vector2(-BoxWidth * 0.5f, BoxHeight * 0.5f));
            Vector2 bottomRight = ToScreen(new Vector2(BoxWidth * 0.5f, -BoxHeight * 0.5f));
            EditorGUI.DrawRect(
                Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y),
                new Color(0.21f, 0.21f, 0.24f));

            DrawPoints(_raw, new Color(0.55f, 0.55f, 0.58f), 2f);
            DrawPolyline(filtered, new Color(0.35f, 0.62f, 0.95f), 1.5f);
            DrawPoints(filtered, new Color(0.35f, 0.62f, 0.95f), 4f);

            if (stroke.IsValid)
            {
                DrawPolyline(stroke.Points, new Color(0.95f, 0.35f, 0.35f), 3f);
                DrawPoints(stroke.Points, new Color(1f, 0.6f, 0.3f), 6f);
            }
        }

        void DrawPolyline(List<Vector2> points, Color color, float width)
        {
            if (points == null || points.Count < 2) return;

            var screen = new Vector3[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 p = ToScreen(points[i]);
                screen[i] = new Vector3(p.x, p.y, 0f);
            }

            Handles.color = color;
            Handles.DrawAAPolyLine(width, screen);
        }

        void DrawPoints(List<Vector2> points, Color color, float size)
        {
            if (points == null) return;

            float half = size * 0.5f;
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 p = ToScreen(points[i]);
                EditorGUI.DrawRect(new Rect(p.x - half, p.y - half, size, size), color);
            }
        }

        void DrawStats(StrokeBuilder builder, Stroke stroke)
        {
            int finalCount = stroke.IsValid ? stroke.Points.Count : 0;
            float ratio = _raw.Count == 0 ? 0f : finalCount / (float)_raw.Count;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(
                    $"원본 {_raw.Count}  →  필터 {builder.Points.Count}  →  최종 {finalCount}" +
                    $"   (압축률 {ratio:P0})",
                    EditorStyles.miniLabel);

                GUILayout.FlexibleSpace();

                GUILayout.Label(
                    $"길이 {stroke.Length():F2}wu   잔량 {builder.RemainingInk:F2}   " +
                    (stroke.IsValid ? "확정" : "롤백"),
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.LabelField(
                "회색=원본 · 파랑=필터 통과 · 빨강=RDP 확정",
                EditorStyles.centeredGreyMiniLabel);
        }

        Vector2 ToWorld(Vector2 screen) => new Vector2(
            (screen.x - _canvas.center.x) / _scale,
            (_canvas.center.y - screen.y) / _scale);

        Vector2 ToScreen(Vector2 world) => new Vector2(
            _canvas.center.x + world.x * _scale,
            _canvas.center.y - world.y * _scale);
    }
}
