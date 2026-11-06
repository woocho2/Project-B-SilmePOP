// 슬라임 몸통 + 강조 외곽선. 슬라임 프리팹의 Color(몸통) SpriteRenderer 재질로 씀.
// 몸통: 그림 × SpriteRenderer.color (슬라임 색).
// 외곽선: 화면 기준 최소 두께의 컬러 띠 + 밝은 안쪽 테두리. 몸통 안쪽에 그려 잘림을 피함.
//         _OutlineColor가 투명이면 외곽선 없음. SlimeController가 슬라임마다 드래그(초록·빨강)·힌트(노랑)·파괴(파랑) 색을 넘김.
Shader "SlimePOP/SlimeOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 0)
        _OutlineWidth ("Outline Width (Sprite UV)", Range(0, 0.1)) = 0.028
        _OutlineMinPixels ("Minimum Outline Width (Screen Pixels)", Range(0, 8)) = 3.2
        _RimWidthPixels ("Inner Rim Width (Screen Pixels)", Range(0, 3)) = 0.9
        _RimColor ("Inner Rim Color", Color) = (1, 1, 1, 0.9)
        _FillStrength ("Body Accent Tint", Range(0, 0.3)) = 0.07
        _PulseSpeed ("Pulse Speed (Cycles Per Second)", Range(0, 3)) = 1.1
        _PulseAmount ("Pulse Amount", Range(0, 0.4)) = 0.1
        _Softness ("Edge Softness", Range(0.01, 1)) = 0.2
        [HideInInspector] _SpriteUVRect ("Sprite UV Bounds", Vector) = (0, 0, 1, 1)
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
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                half4 _RimColor;
                float4 _SpriteUVRect;
                float _OutlineWidth;
                float _OutlineMinPixels;
                float _RimWidthPixels;
                float _FillStrength;
                float _PulseSpeed;
                float _PulseAmount;
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
                // Sprite rect 밖은 투명으로 취급: Clamp 텍스처와 아틀라스에서도
                // 이미지 끝까지 찬 몸통의 외곽선이 끊기지 않게 한다.
                float2 inside = step(_SpriteUVRect.xy, uv) * step(uv, _SpriteUVRect.zw);
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a * inside.x * inside.y;
            }

            // 각 방향의 UV 변화량으로 화면 픽셀 두께를 구한다.
            // 두 개의 동심 띠를 같은 16방향으로 샘플링해 둥근 컬러선/안쪽선을 만든다.
            half2 AlphaMinAround(float2 uv, float2 uvDx, float2 uvDy)
            {
                half2 around = half2(1, 1);
                float2 spriteUVSize = max(_SpriteUVRect.zw - _SpriteUVRect.xy, float2(0.000001, 0.000001));
                [unroll]
                for (int i = 0; i < 16; i++)
                {
                    float angle = i * (6.28318530718 / 16.0);
                    float2 direction = float2(cos(angle), sin(angle));
                    float2 uvPerPixel = uvDx * direction.x + uvDy * direction.y;
                    float relativeUVPerPixel = max(length(uvPerPixel / spriteUVSize), 0.000001);
                    float widthPixels = max(_OutlineMinPixels, _OutlineWidth / relativeUVPerPixel);
                    around.x = min(around.x, SampleAlpha(uv + uvPerPixel * widthPixels));
                    around.y = min(around.y, SampleAlpha(uv + uvPerPixel * (widthPixels + _RimWidthPixels)));
                }
                return around;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 body = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                if (_OutlineColor.a <= 0) return body;   // 강조가 꺼져 있으면 몸통만

                half2 around = AlphaMinAround(input.uv, ddx(input.uv), ddy(input.uv));
                half softness = max(0.01h, _Softness * 0.45h);
                half2 edge = 1.0h - smoothstep(0.5h - softness, 0.5h + softness, around);
                half rim = saturate(edge.y - edge.x);

                // 강조가 꺼질 정도로 깜빡이지 않는 작은 밝기 변화.
                half wave = 0.5h + 0.5h * sin(_Time.y * _PulseSpeed * 6.28318530718);
                half pulse = 1.0h - _PulseAmount * wave;
                half strength = saturate(_OutlineColor.a) * pulse;
                half3 outlineRGB = lerp(_OutlineColor.rgb, half3(1, 1, 1), 0.06h);
                half3 rimRGB = lerp(_OutlineColor.rgb, _RimColor.rgb, 0.88h);

                half3 rgb = lerp(body.rgb, _OutlineColor.rgb, _FillStrength * strength);
                rgb = lerp(rgb, outlineRGB, edge.x * strength);
                rgb = lerp(rgb, rimRGB, rim * strength * _RimColor.a);
                return half4(rgb, body.a);
            }
            ENDHLSL
        }
    }
}
