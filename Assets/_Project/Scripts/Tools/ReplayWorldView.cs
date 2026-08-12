using PPS.Core;
using UnityEngine;

namespace PPS.Tools
{
    // SimWorld의 공, 지형, Stroke와 LevelData의 목표 등을 화면에 그린다.
    public class ReplayWorldView : MonoBehaviour
    {
        // 원을 구성할 선분 개수다.
        const int CircleSegments = 28;

        // 킬 라인을 좌우로 얼마나 길게 표시할지 결정한다.
        const float KillLineHalfWidth = 30f;

        // 공, 목표, 지형이 화면 가장자리에 붙지 않도록 추가할 여백이다.
        [SerializeField] float _cameraPadding = 1f;

        // 공을 표시할 색상이다.
        [SerializeField] Color _ballColor = Color.red;

        // 고정된 지형을 표시할 색상이다.
        [SerializeField] Color _staticColor = Color.black;

        // 플레이어가 그린 움직이는 물체를 표시할 색상이다.
        [SerializeField] Color _dynamicColor = Color.blue;

        // 목표 지점을 표시할 색상이다.
        [SerializeField] Color _goalColor = Color.green;

        // 위험 물체를 표시할 색상이다.
        [SerializeField] Color _hazardColor = new Color(1f, 0.4f, 0f);

        // 별을 표시할 색상이다.
        [SerializeField] Color _starColor = Color.yellow;

        // 킬 라인을 표시할 색상이다.
        [SerializeField] Color _killLineColor = Color.red;

        // 현재 화면에 표시할 레벨 정보다.
        LevelData _level;

        // 현재 화면에 표시할 물리 월드다.
        SimWorld _world;

        // GL 선을 그릴 때 사용할 공용 Material이다.
        static Material _material;

        // SimWorldRenderer가 새 리플레이 월드를 만들었을 때 호출한다.
        public void Show(LevelData level, SimWorld world)
        {
            // 목표, 별, 킬 라인을 그리기 위해 LevelData를 보관한다.
            _level = level;

            // 공, 지형, Stroke를 그리기 위해 SimWorld를 보관한다.
            _world = world;

            // 새로 불러온 리플레이 전체가 화면에 들어오도록 카메라를 맞춘다.
            FitCamera();
        }

        // 공, 목표, 지형과 모든 Stroke가 화면 안에 들어오도록 카메라를 조정한다.
        void FitCamera()
        {
            // MainCamera 태그가 지정된 카메라를 가져온다.
            Camera camera = Camera.main;

            // 카메라나 리플레이 데이터가 없으면 조정할 수 없다.
            if (camera == null || _level == null || _world == null)
                return;

            // 목표 지점을 최초 카메라 영역으로 사용한다.
            Bounds bounds = new Bounds(
                _level.GoalPosition,
                Vector3.one * LevelData.GoalRadius * 2f);

            // SimWorld에 생성된 모든 물리 바디를 가져온다.
            var bodies = _world.Bodies;

            // 공, 지형, Stroke, 장치의 Collider 범위를 차례대로 포함한다.
            for (int i = 0; i < bodies.Count; i++)
            {
                // 현재 물리 바디를 가져온다.
                Rigidbody2D body = bodies[i];

                // 파괴되었거나 존재하지 않는 바디는 건너뛴다.
                if (body == null)
                    continue;

                // 현재 바디에 붙은 모든 Collider2D를 가져온다.
                Collider2D[] colliders = body.GetComponents<Collider2D>();

                // 각 Collider의 월드 좌표 범위를 카메라 영역에 포함한다.
                for (int colliderIndex = 0;
                     colliderIndex < colliders.Length;
                     colliderIndex++)
                {
                    // 현재 Collider를 가져온다.
                    Collider2D collider = colliders[colliderIndex];

                    // 비활성화된 Collider는 화면 범위 계산에서 제외한다.
                    if (collider == null || !collider.enabled)
                        continue;

                    // Collider 전체 범위를 카메라 영역에 포함한다.
                    bounds.Encapsulate(collider.bounds);
                }
            }

            // 별은 물리 Collider가 없으므로 별도로 카메라 영역에 포함한다.
            for (int i = 0; i < _level.Stars.Count; i++)
            {
                // 별의 중심에서 반지름만큼 떨어진 왼쪽 아래 좌표다.
                Vector2 starMin =
                    _level.Stars[i] - Vector2.one * LevelData.StarCaptureRadius;

                // 별의 중심에서 반지름만큼 떨어진 오른쪽 위 좌표다.
                Vector2 starMax =
                    _level.Stars[i] + Vector2.one * LevelData.StarCaptureRadius;

                // 별의 왼쪽 아래 좌표를 카메라 영역에 포함한다.
                bounds.Encapsulate(starMin);

                // 별의 오른쪽 위 좌표를 카메라 영역에 포함한다.
                bounds.Encapsulate(starMax);
            }

            // 계산된 전체 영역의 절반 높이에 여백을 추가한다.
            float requiredHalfHeight = bounds.extents.y + _cameraPadding;

            // 계산된 전체 영역의 절반 너비에 여백을 추가한다.
            float requiredHalfWidth = bounds.extents.x + _cameraPadding;

            // 세로형 화면에서도 전체 너비가 잘리지 않도록 필요한 높이를 계산한다.
            float heightFromWidth =
                requiredHalfWidth / Mathf.Max(camera.aspect, 0.01f);

            // 원근이 아닌 2D 직교 카메라로 설정한다.
            camera.orthographic = true;

            // 높이와 너비 중 더 큰 화면 범위를 기준으로 카메라 크기를 설정한다.
            camera.orthographicSize =
                Mathf.Max(requiredHalfHeight, heightFromWidth);

            // 전체 리플레이 영역의 중심으로 카메라를 이동한다.
            camera.transform.position = new Vector3(
                bounds.center.x,
                bounds.center.y,
                -10f);
        }

        // 현재 표시 중인 데이터를 제거한다.
        public void Clear()
        {
            _level = null;
            _world = null;
        }

        // 카메라가 장면을 렌더링할 때 호출된다.
        void OnRenderObject()
        {
            // 표시할 데이터가 없으면 아무것도 그리지 않는다.
            if (_level == null || _world == null)
                return;

            // GL 선 그리기에 필요한 Material을 준비한다.
            SetMaterialPass();

            // GL 좌표 계산을 시작한다.
            GL.PushMatrix();

            // 모든 시각 요소를 선으로 그린다.
            GL.Begin(GL.LINES);

            // 목표, 별, 킬 라인처럼 Rigidbody2D가 아닌 정보를 그린다.
            DrawLevelMarkers();

            // SimWorld에 생성된 모든 물리 바디를 그린다.
            DrawWorldBodies();

            // 선 그리기를 종료한다.
            GL.End();

            // GL 좌표 계산을 종료한다.
            GL.PopMatrix();
        }

        // 목표, 별, 킬 라인을 그린다.
        void DrawLevelMarkers()
        {
            // 목표 지점 색상을 설정한다.
            GL.Color(_goalColor);

            // 목표 지점에 원을 그린다.
            DrawCircle(_level.GoalPosition, LevelData.GoalRadius);

            // 별 색상을 설정한다.
            GL.Color(_starColor);

            // 레벨에 등록된 별을 순서대로 그린다.
            for (int i = 0; i < _level.Stars.Count; i++)
            {
                DrawCircle(_level.Stars[i], LevelData.StarCaptureRadius);
            }

            // 킬 라인 색상을 설정한다.
            GL.Color(_killLineColor);

            // KillY 높이에 가로선을 그린다.
            DrawLine(
                new Vector2(-KillLineHalfWidth, _level.KillY),
                new Vector2(KillLineHalfWidth, _level.KillY));
        }

        // SimWorld의 공, 지형, Stroke와 장치를 그린다.
        void DrawWorldBodies()
        {
            // SimWorld에 등록된 물리 바디 목록을 가져온다.
            var bodies = _world.Bodies;

            // 모든 물리 바디를 순서대로 확인한다.
            for (int i = 0; i < bodies.Count; i++)
            {
                // 현재 그릴 Rigidbody2D를 가져온다.
                Rigidbody2D body = bodies[i];

                // 파괴된 물체는 그리지 않는다.
                if (body == null)
                    continue;

                // 물체의 역할에 따라 색상을 선택한다.
                if (ReferenceEquals(body, _world.Ball))
                {
                    // 현재 바디가 공이면 공 색상을 사용한다.
                    GL.Color(_ballColor);
                }
                else if (IsHazard(body))
                {
                    // 위험 충돌체가 붙은 바디면 위험 색상을 사용한다.
                    GL.Color(_hazardColor);
                }
                else if (body.bodyType == RigidbodyType2D.Static)
                {
                    // 움직이지 않는 지형이면 고정 물체 색상을 사용한다.
                    GL.Color(_staticColor);
                }
                else
                {
                    // 나머지 움직이는 물체는 동적 물체 색상을 사용한다.
                    GL.Color(_dynamicColor);
                }

                // Rigidbody2D에 붙은 Collider 모양을 그린다.
                DrawBody(body);
            }
        }

        // Rigidbody2D에 붙은 Collider 모양을 그린다.
        static void DrawBody(Rigidbody2D body)
        {
            // 선 형태 Collider가 있는지 확인한다.
            EdgeCollider2D edge = body.GetComponent<EdgeCollider2D>();

            if (edge != null)
            {
                // 선을 구성하는 지역 좌표들을 가져온다.
                Vector2[] points = edge.points;

                // 지역 좌표를 월드 좌표로 바꾸기 위한 Transform을 가져온다.
                Transform bodyTransform = body.transform;

                // 인접한 점들을 선으로 연결한다.
                for (int i = 0; i + 1 < points.Length; i++)
                {
                    DrawLine(
                        bodyTransform.TransformPoint(points[i]),
                        bodyTransform.TransformPoint(points[i + 1]));
                }

                return;
            }

            // 다각형 Collider가 있는지 확인한다.
            PolygonCollider2D polygon = body.GetComponent<PolygonCollider2D>();

            if (polygon != null)
            {
                // 지역 좌표를 월드 좌표로 바꾸기 위한 Transform을 가져온다.
                Transform bodyTransform = body.transform;

                // PolygonCollider2D의 모든 경로를 확인한다.
                for (int pathIndex = 0; pathIndex < polygon.pathCount; pathIndex++)
                {
                    // 현재 다각형 경로의 점들을 가져온다.
                    Vector2[] path = polygon.GetPath(pathIndex);

                    // 마지막 점과 첫 번째 점까지 연결해 닫힌 도형으로 만든다.
                    for (int i = 0; i < path.Length; i++)
                    {
                        DrawLine(
                            bodyTransform.TransformPoint(path[i]),
                            bodyTransform.TransformPoint(path[(i + 1) % path.Length]));
                    }
                }

                return;
            }

            // 원형 Collider가 있는지 확인한다.
            CircleCollider2D circle = body.GetComponent<CircleCollider2D>();

            if (circle != null)
            {
                // Rigidbody2D의 현재 위치에 원을 그린다.
                DrawCircle(body.position, circle.radius);
            }
        }

        // 해당 Rigidbody2D가 위험 물체인지 확인한다.
        bool IsHazard(Rigidbody2D body)
        {
            // SimWorld에 등록된 위험 Collider 목록을 가져온다.
            var hazards = _world.Hazards;

            // 모든 위험 Collider를 확인한다.
            for (int i = 0; i < hazards.Count; i++)
            {
                // 현재 위험 Collider를 가져온다.
                Collider2D hazard = hazards[i];

                // Collider가 현재 Rigidbody2D에 붙어 있으면 위험 물체다.
                if (hazard != null &&
                    ReferenceEquals(hazard.attachedRigidbody, body))
                {
                    return true;
                }
            }

            // 위험 목록에 없으면 일반 물체다.
            return false;
        }

        // 두 좌표 사이에 선을 그린다.
        static void DrawLine(Vector2 start, Vector2 end)
        {
            // 선의 시작점을 등록한다.
            GL.Vertex3(start.x, start.y, 0f);

            // 선의 끝점을 등록한다.
            GL.Vertex3(end.x, end.y, 0f);
        }

        // 중심점과 반지름을 사용해 원을 그린다.
        static void DrawCircle(Vector2 center, float radius)
        {
            // 원의 가장 오른쪽 지점을 첫 점으로 사용한다.
            Vector2 previous = center + new Vector2(radius, 0f);

            // 원 둘레를 여러 선분으로 나누어 그린다.
            for (int i = 1; i <= CircleSegments; i++)
            {
                // 현재 선분의 각도를 계산한다.
                float angle = i * 2f * Mathf.PI / CircleSegments;

                // 현재 각도에 해당하는 원 둘레 좌표를 계산한다.
                Vector2 next = center +
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                // 이전 점과 현재 점을 연결한다.
                DrawLine(previous, next);

                // 다음 선분에서 사용할 이전 점을 갱신한다.
                previous = next;
            }
        }

        // GL 렌더링에 사용할 Material을 준비한다.
        static void SetMaterialPass()
        {
            // 아직 Material이 만들어지지 않았다면 한 번만 생성한다.
            if (_material == null)
            {
                // Unity 내부 컬러 Shader로 Material을 만든다.
                _material = new Material(Shader.Find("Hidden/Internal-Colored"));

                // 씬이나 에셋 파일에 저장되지 않도록 설정한다.
                _material.hideFlags = HideFlags.HideAndDontSave;

                // 깊이 버퍼에 값을 기록하지 않는다.
                _material.SetFloat("_ZWrite", 0f);

                // 다른 물체의 깊이와 관계없이 항상 선이 보이게 한다.
                _material.SetFloat(
                    "_ZTest",
                    (float)UnityEngine.Rendering.CompareFunction.Always);
            }

            // 생성한 Material의 첫 번째 Pass를 적용한다.
            _material.SetPass(0);
        }
    }
}
