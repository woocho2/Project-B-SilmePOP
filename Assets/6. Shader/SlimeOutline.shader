// 슬라임 몸통 + 강조 외곽선. 슬라임 프리팹의 Color(몸통) SpriteRenderer 재질로 씀.
// 몸통: 그림 × SpriteRenderer.color (슬라임 색).
// 외곽선: 몸통 가장자리 안쪽을 고른 두께로 _OutlineColor로 칠함 (그림을 몸통에 맞게 잘라도 잘리지 않게 안쪽에 그림).
//         _OutlineColor가 투명이면 외곽선 없음. SlimeController가 슬라임마다 드래그(초록·빨강)·힌트(노랑)·파괴(파랑) 색을 넘김.
Shader "SlimePOP/SlimeOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 0)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.02    // 그림 파일 크기 대비 두께
        _Softness ("Softness", Range(0.01, 1)) = 0.35             // 가장자리 흐림
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            // 2D 렌더러가 스프라이트를 그리는 단계
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
                float _Softness;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;       // SpriteRenderer.color
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half SampleAlpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            // 둘레 16방향과 절반 거리 8방향 중 가장 투명한 값. 이 점 근처에 몸통 밖이 있는가.
            half AlphaMinAround(float2 uv)
            {
                half around = 1;
                [unroll]
                for (int i = 0; i < 16; i++)
                {
                    float angle = i * (6.28318530718 / 16.0);
                    float2 direction = float2(cos(angle), sin(angle));
                    around = min(around, SampleAlpha(uv + direction * _OutlineWidth));
                    if (i % 2 == 0) around = min(around, SampleAlpha(uv + direction * _OutlineWidth * 0.5));
                }
                return around;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 body = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                if (_OutlineColor.a <= 0) return body;   // 강조가 꺼져 있으면 몸통만

                // 몸통 안이면서 가장자리 가까운 곳 = 외곽선
                half edge = smoothstep(0, _Softness, 1 - AlphaMinAround(input.uv)) * _OutlineColor.a;
                half3 rgb = lerp(body.rgb, _OutlineColor.rgb, edge);
                return half4(rgb, body.a);
            }
            ENDHLSL
        }
    }
}
