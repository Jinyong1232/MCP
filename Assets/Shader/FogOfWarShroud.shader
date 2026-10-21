// 스타크래프트식 안개(shroud). 바닥 전체를 어둡지만 반투명한 색으로 덮어 배경/길이 은은하게 비쳐 보이게
// 하고, 플레이어(및 조명탄 등 추가 시야 소스) 주변 현재 시야 반경 안쪽만 매 프레임 동적으로 걷어낸다.
// 이전 방식(FogOfWarGround가 텍스처에 원을 직접 칠해서 "한 번 걷히면 영구히 안 돌아옴")과 달리
// 여기서는 상태를 전혀 저장하지 않는다 — 시야를 벗어나면 즉시 다시 어두워진다.
// 월드 좌표를 프래그먼트 셰이더에서 직접 계산하므로 바닥 오브젝트의 크기/스케일과 무관하게 항상
// 정확한 원으로 보인다(과거 worldSize/worldCenter를 따로 맞춰야 했던 방식의 불일치 문제가 구조적으로 없음).
Shader "Custom/FogOfWarShroud"
{
    Properties
    {
        _FogColor("Fog Color", Color) = (0.05, 0.05, 0.08, 0.72)
        _EdgeSoftness("Edge Softness", Float) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define MAX_SOURCES 8

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float _EdgeSoftness;
                float4 _PlayerPos;      // xy = 월드 x,z
                float _VisionRadius;
                float _SourceCount;
                float4 _SourcePos[MAX_SOURCES]; // xy = 월드 x,z, z = 반경
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            // 원 안쪽=1(완전히 걷힘), 바깥쪽=0(안개 그대로), 경계는 _EdgeSoftness 폭으로 부드럽게.
            float VisibilityFactor(float2 p, float2 center, float radius)
            {
                float d = distance(p, center);
                return 1.0 - smoothstep(radius - _EdgeSoftness, radius, d);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 p = IN.positionWS.xz;
                float visible = VisibilityFactor(p, _PlayerPos.xy, _VisionRadius);

                int count = (int)_SourceCount;
                for (int i = 0; i < MAX_SOURCES; i++)
                {
                    if (i >= count) break;
                    float v = VisibilityFactor(p, _SourcePos[i].xy, _SourcePos[i].z);
                    visible = max(visible, v);
                }

                half alpha = _FogColor.a * (1.0 - visible);
                return half4(_FogColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
