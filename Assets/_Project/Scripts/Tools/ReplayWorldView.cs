using System.Collections.Generic;
using PPS.Core;
using PPS.DrawingTool;
using PPS.Game;
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

        // 리플레이 오브젝트에 사용할 테마 이미지 설정이다.
        [SerializeField] SimStyle _style;

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
        // 재생이 다시 시작되면 표시 객체도 새로 만든다.
        GameObject _visualRoot;
        // 기존 드로잉툴과 같은 방식으로 테마 에셋을 표시한다.
        LevelView _themedLevelView;

        // 폭탄에서 발생한 파편 표시다.
        readonly List<SpriteRenderer> _fragmentSprites =
            new List<SpriteRenderer>();
        SpriteRenderer _ballSprite;
        SpriteRenderer _goalSprite;
        SpriteRenderer[] _starSprites;

        // 드로잉툴과 같은 선 머티리얼을 연결한다.
        [SerializeField] Material _strokeMaterial;

        LineRenderer[] _strokeLines;
        Vector3[][] _strokeLocalPoints;
        bool[] _strokeFollowsBody;

        public void Show(
    LevelData level,
    SimWorld world,
    Solution solution = null)
        {
            Clear();

            _level = level;
            _world = world;

            if (_level == null || _world == null)
                return;

            CreateVisuals();
            CreateStrokeVisuals(solution);
            UpdateVisuals();
            UpdateStrokeVisuals();
            FitCamera();
        }
        // 저장된 획을 드로잉툴과 같은 색과 두께로 표시한다.
        void CreateStrokeVisuals(Solution solution)
        {
            if (solution == null || solution.Strokes == null)
                return;

            if (_strokeMaterial == null)
            {
                Debug.LogWarning(
                    "ReplayWorldView의 Stroke Material을 연결하세요.",
                    this);
                return;
            }

            // 스타일 이미지가 없어도 선은 표시할 수 있다.
            if (_visualRoot == null)
            {
                _visualRoot = new GameObject("ReplayVisuals");
                _visualRoot.transform.SetParent(transform, false);
            }

            int count = solution.Strokes.Count;

            _strokeLines = new LineRenderer[count];
            _strokeLocalPoints = new Vector3[count][];
            _strokeFollowsBody = new bool[count];

            var bodies = _world.StrokeBodies;

            for (int i = 0; i < count; i++)
            {
                Stroke stroke = solution.Strokes[i];

                if (stroke.Points == null || stroke.Points.Count < 2)
                    continue;

                GameObject visual = new GameObject($"Stroke_{i}");
                visual.layer = gameObject.layer;
                visual.transform.SetParent(_visualRoot.transform, false);

                LineRenderer line = visual.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.widthMultiplier = 0.12f;
                line.sharedMaterial = _strokeMaterial;
                line.sortingOrder = i;
                line.positionCount = stroke.Points.Count;

                Color tint = stroke.Tool == ToolType.FreeBody
                    ? new Color32(0x7A, 0x2E, 0x9E, 0xFF)
                    : new Color32(0x1B, 0x4F, 0xA0, 0xFF);

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", tint);
                line.SetPropertyBlock(block);

                Rigidbody2D body = i < bodies.Count ? bodies[i] : null;

                bool follows =
                    body != null &&
                    body.bodyType == RigidbodyType2D.Dynamic;

                _strokeFollowsBody[i] = follows;

                Vector3[] localPoints = follows
                    ? new Vector3[stroke.Points.Count]
                    : null;

                for (int j = 0; j < stroke.Points.Count; j++)
                {
                    Vector2 point = stroke.Points[j];
                    Vector3 position = new Vector3(point.x, point.y, 0f);

                    line.SetPosition(j, position);

                    if (follows)
                    {
                        localPoints[j] =
                            body.transform.InverseTransformPoint(position);
                    }
                }

                _strokeLines[i] = line;
                _strokeLocalPoints[i] = localPoints;
            }
        }
        // 움직이는 획의 표시를 물리 바디 위치에 맞춘다.
        void UpdateStrokeVisuals()
        {
            if (_world == null ||
                _strokeLines == null ||
                _strokeLocalPoints == null ||
                _strokeFollowsBody == null)
            {
                return;
            }

            var bodies = _world.StrokeBodies;

            int count = Mathf.Min(
                _strokeLines.Length,
                bodies.Count);

            for (int i = 0; i < count; i++)
            {
                if (!_strokeFollowsBody[i])
                    continue;

                LineRenderer line = _strokeLines[i];
                Vector3[] localPoints = _strokeLocalPoints[i];
                Rigidbody2D body = bodies[i];

                if (line == null || localPoints == null)
                    continue;

                if (body == null)
                {
                    line.enabled = false;
                    continue;
                }

                line.enabled = true;

                for (int j = 0; j < localPoints.Length; j++)
                {
                    line.SetPosition(
                        j,
                        body.transform.TransformPoint(localPoints[j]));
                }
            }
        }
        // 자유물체
        // 스타일에 등록된 공·목표·별 이미지를 생성한다.
        // 기존 드로잉툴과 같은 테마 표시를 생성한다.
        void CreateVisuals()
        {
            if (_style == null)
            {
                Debug.LogWarning(
                    "ReplayWorldView의 Style을 연결하세요.",
                    this);

                return;
            }

            _visualRoot = new GameObject("ReplayVisuals");
            _visualRoot.transform.SetParent(transform, false);

            _themedLevelView =
                _visualRoot.AddComponent<LevelView>();

            _themedLevelView.SetStyle(_style);
            _themedLevelView.SetLevel(_level);
        }

        // 이미지 비율을 유지하며 지정한 크기로 표시한다.
        SpriteRenderer CreateSprite(
            string objectName,
            Sprite sprite,
            Vector2 position,
            float diameter,
            int sortingOrder)
        {
            if (sprite == null)
                return null;

            GameObject visual = new GameObject(objectName);
            visual.layer = gameObject.layer;
            visual.transform.SetParent(_visualRoot.transform, false);
            visual.transform.position =
                new Vector3(position.x, position.y, 0f);

            SpriteRenderer renderer =
                visual.AddComponent<SpriteRenderer>();

            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = sortingOrder;

            float spriteSize = Mathf.Max(
                sprite.bounds.size.x,
                sprite.bounds.size.y);

            float scale = diameter / Mathf.Max(spriteSize, 0.0001f);
            visual.transform.localScale = Vector3.one * scale;

            return renderer;
        }

        // 물리 스텝이 진행된 뒤 표시 위치를 갱신한다.
        void LateUpdate()
        {
            UpdateVisuals();
            UpdateStrokeVisuals();
        }

        // 물리 월드의 현재 상태를 테마 화면에 반영한다.
        void UpdateVisuals()
        {
            if (_level == null ||
                _world == null ||
                _themedLevelView == null)
            {
                return;
            }

            Rigidbody2D ball = _world.Ball;

            _themedLevelView.SetBallVisible(ball != null);

            if (ball != null)
            {
                _themedLevelView.MoveBall(
                    ball.position,
                    ball.rotation);
            }

            for (int i = 0; i < _level.Stars.Count; i++)
            {
                _themedLevelView.SetStarVisible(
                    i,
                    !_world.Judge.IsCollected(i));
            }

            for (int i = 0; i < _level.Devices.Count; i++)
            {
                (IDeviceData data, Rigidbody2D body) =
                    _world.GetDevice(i);

                bool visible =
                    body != null ||
                    !DeviceRegistry.MakesBody(data.Type);

                _themedLevelView.SetDeviceVisible(i, visible);

                if (visible && body != null)
                {
                    _themedLevelView.MoveDevice(
                        i,
                        body.position,
                        body.rotation);
                }
            }

            _themedLevelView.ShowCountdown(
                _world.CurrentStep);

            UpdateFragments();
        }
        // 위험 파편을 SimStyle의 Dot 이미지로 표시한다.
        void UpdateFragments()
        {
            if (_style == null ||
                _style.Sprites == null ||
                _style.Sprites.Dot == null ||
                _visualRoot == null)
            {
                return;
            }

            var hazards = _world.Hazards;

            while (_fragmentSprites.Count < hazards.Count)
            {
                int index = _fragmentSprites.Count;

                SpriteRenderer renderer =
                    MapHandleGfx.Create(
                        _visualRoot.transform,
                        $"Fragment_{index}",
                        _style.Sprites.Dot);

                renderer.sortingOrder = 1500;
                _fragmentSprites.Add(renderer);
            }

            for (int i = 0; i < _fragmentSprites.Count; i++)
            {
                bool visible =
                    i < hazards.Count &&
                    hazards[i] != null;

                SpriteRenderer renderer =
                    _fragmentSprites[i];

                renderer.gameObject.SetActive(visible);

                if (!visible)
                    continue;

                MapHandleGfx.PlaceDot(
                    renderer,
                    _style.Sprites.Dot,
                    hazards[i].transform.position,
                    FragBombDevice.FragmentRadius,
                    SimStyle.Plain);
            }
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

        public void Clear()
        {
            if (_visualRoot != null)
            {
                // Destroy가 처리되는 프레임까지 중복 표시를 막는다.
                _visualRoot.SetActive(false);
                Destroy(_visualRoot);
            }

            _visualRoot = null;
            _themedLevelView = null;

            _ballSprite = null;
            _goalSprite = null;
            _starSprites = null;

            _fragmentSprites.Clear();

            _strokeLines = null;
            _strokeLocalPoints = null;
            _strokeFollowsBody = null;

            _level = null;
            _world = null;
        }

        // 카메라가 장면을 렌더링할 때 호출된다.
        void OnRenderObject()
        {
            // 표시할 데이터가 없으면 아무것도 그리지 않는다.
            if (_level == null || _world == null)
                return;

            // 테마 화면이 정상 생성됐으면 Collider 윤곽선을 겹쳐 그리지 않는다.
            if (_themedLevelView != null)
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

            void DrawLevelMarkers()
            {
                // 목표 이미지가 없을 때만 기존 원을 표시한다.
                if (_goalSprite == null)
                {
                    GL.Color(_goalColor);
                    DrawCircle(_level.GoalPosition, LevelData.GoalRadius);
                }

                GL.Color(_starColor);

                for (int i = 0; i < _level.Stars.Count; i++)
                {
                    if (_world.Judge.IsCollected(i))
                        continue;

                    bool hasSprite =
                        _starSprites != null &&
                        i < _starSprites.Length &&
                        _starSprites[i] != null;

                    if (!hasSprite)
                    {
                        DrawCircle(
                            _level.Stars[i],
                            LevelData.StarCaptureRadius);
                    }
                }

                GL.Color(_killLineColor);

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
                    {
                        continue;
                    }

                    // 이미지가 있는 공은 기존 GL 원을 중복해서 그리지 않는다.
                    if (ReferenceEquals(body, _world.Ball) &&
                        _ballSprite != null)
                    {
                        continue;
                    }

                    // LineRenderer로 표시하는 획은 기존 GL 선을 중복해서 그리지 않는다.
                    if (IsRenderedStrokeBody(body))
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
            // 새 LineRenderer로 표시 중인 획 바디인지 확인한다.
            bool IsRenderedStrokeBody(Rigidbody2D body)
            {
                if (_world == null || _strokeLines == null)
                    return false;

                var bodies = _world.StrokeBodies;

                int count = Mathf.Min(
                    bodies.Count,
                    _strokeLines.Length);

                for (int i = 0; i < count; i++)
                {
                    if (_strokeLines[i] != null &&
                        ReferenceEquals(bodies[i], body))
                    {
                        return true;
                    }
                }

                return false;
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
                PolygonCollider2D[] polygons = body.GetComponents<PolygonCollider2D>();

                if (polygons.Length > 0)
                {
                    // 지역 좌표를 월드 좌표로 바꾸기 위한 Transform을 가져온다.
                    Transform bodyTransform = body.transform;

                    // 선분마다 붙은 PolygonCollider2D를 모두 확인한다.
                    for (int pathIndex = 0; pathIndex < polygons.Length; pathIndex++)
                    {
                        // 현재 다각형 경로의 점들을 가져온다.
                        Vector2[] path = polygons[pathIndex].GetPath(0);

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
