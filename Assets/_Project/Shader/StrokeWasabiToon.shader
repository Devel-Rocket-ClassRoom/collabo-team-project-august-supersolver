Shader "PPS/Stroke/WasabiToon"
{
    Properties
    {
        [Header(Tint)]
        // StrokePreviewRenderer 가 프로퍼티 블록으로 덮는 칸.
        _BaseColor      ("Tint", Color) = (1, 1, 1, 1)

        [Header(Body)]
        // 폭을 가로지르는 3 단 그라데이션. 위가 밝고 아래가
        // 짙어야 짜낸 반죽이 둥글게 보인다.
        _TopColor       ("Top Color", Color)    = (0.741, 0.882, 0.475, 1)
        _BodyColor      ("Body Color", Color)   = (0.596, 0.804, 0.318, 1)
        _BottomColor    ("Bottom Color", Color) = (0.451, 0.690, 0.239, 1)
        _BodyMid        ("Body Mid Position", Range(0, 1)) = 0.45

        [Header(Highlight)]
        _HighlightColor ("Highlight Color", Color) = (0.902, 0.965, 0.741, 1)
        _HighlightPos   ("Highlight Position", Range(-1, 1)) = -0.45
        _HighlightWidth ("Highlight Width", Range(0.02, 0.8)) = 0.16
        _HighlightSoft  ("Highlight Softness", Range(0, 1)) = 0.55
        _Highlight      ("Highlight Strength", Range(0, 1)) = 0.9

        [Header(Ends)]
        // 짜다 뗀 끝은 가늘게 빠진다. 월드 단위라 획 길이가
        // 달라도 끝 모양이 일정하다.
        _TailLength     ("Tail Length (world)", Float) = 0.45
        _TailSharp      ("Tail Sharpness", Range(0.3, 3)) = 1.4
        _HeadLength     ("Head Length (world)", Float) = 0.06

        [Header(Stroke Metrics)]
        _StrokeLength   ("Stroke Length (set by script)", Float) = 1
        _StrokeWidth    ("Stroke Width (set by script)", Float) = 0.12
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TopColor;
                float4 _BodyColor;
                float4 _BottomColor;
                float4 _HighlightColor;
                float  _BodyMid;
                float  _HighlightPos;
                float  _HighlightWidth;
                float  _HighlightSoft;
                float  _Highlight;
                float  _TailLength;
                float  _TailSharp;
                float  _HeadLength;
                float  _StrokeLength;
                float  _StrokeWidth;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float length = max(_StrokeLength, _StrokeWidth);
                half across = IN.uv.y * 2.0 - 1.0;

                // ---- 실루엣 ---------------------------------------
                // 시작은 가늘게 빠지고 끝은 짜다 뗀 듯 둥글다.
                // 길이 기준을 월드로 두어야 짧은 획도 같은 끝을 갖는다.
                float fromTail = IN.uv.x * length;
                float fromHead = (1.0 - IN.uv.x) * length;

                half tail = pow(saturate(fromTail / max(_TailLength, 1e-4)),
                                _TailSharp);

                half h = 1.0 - saturate(fromHead / max(_HeadLength, 1e-4));
                half head = sqrt(saturate(1.0 - h * h));

                half widthRatio = min(tail, head);

                half edge = abs(across) / max(widthRatio, 0.001);

                // 화면 미분으로 한 픽셀만 부드럽게 한다. 플랫한
                // 그림이라 테두리가 흐려지면 바로 티가 난다.
                half aa = max(fwidth(edge), 0.001);
                half alpha = 1.0 - smoothstep(1.0 - aa, 1.0 + aa, edge);

                // ---- 몸통 ------------------------------------------
                // 실루엣이 좁아져도 그라데이션은 같은 자리를 지켜야
                // 한다. 폭에 맞춰 정규화한 좌표로 색을 뽑는다.
                half v = saturate(across / max(widthRatio, 0.001) * 0.5 + 0.5);

                half3 color = lerp(_TopColor.rgb, _BodyColor.rgb,
                                   smoothstep(0.0, _BodyMid, v));
                color = lerp(color, _BottomColor.rgb,
                             smoothstep(_BodyMid, 1.0, v));

                // ---- 하이라이트 --------------------------------------
                // 위쪽에 뜬 밝은 줄 하나. 이것 하나로 납작한 띠가
                // 둥근 반죽이 된다.
                half toLight = abs(across / max(widthRatio, 0.001)
                                   - _HighlightPos);
                half band = 1.0 - smoothstep(
                    _HighlightWidth * (1.0 - _HighlightSoft),
                    _HighlightWidth * (1.0 + _HighlightSoft) + 1e-4,
                    toLight);

                // 가늘어지는 꼬리에서는 하이라이트도 함께 사라진다.
                color = lerp(color, _HighlightColor.rgb,
                             band * _Highlight * widthRatio);

                return half4(color, alpha) * _BaseColor;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
