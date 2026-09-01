Shader "HeavyDowner/Block Debris"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _ShardIndex ("Shard Index", Float) = 0
        [HideInInspector] _SpriteUVRect ("Sprite UV Rect", Vector) = (0, 0, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _ShardIndex;
            float4 _SpriteUVRect;

            float TriangleSign(float2 samplePosition, float2 vertexA, float2 vertexB)
            {
                return (samplePosition.x - vertexB.x) * (vertexA.y - vertexB.y)
                    - (vertexA.x - vertexB.x) * (samplePosition.y - vertexB.y);
            }

            float TriangleMask(
                float2 samplePosition,
                float2 vertexA,
                float2 vertexB,
                float2 vertexC)
            {
                float firstSign = TriangleSign(samplePosition, vertexA, vertexB);
                float secondSign = TriangleSign(samplePosition, vertexB, vertexC);
                float thirdSign = TriangleSign(samplePosition, vertexC, vertexA);
                float hasNegative = step(min(firstSign, min(secondSign, thirdSign)), -0.00001);
                float hasPositive = step(0.00001, max(firstSign, max(secondSign, thirdSign)));
                return 1.0 - hasNegative * hasPositive;
            }

            float QuadMask(
                float2 samplePosition,
                float2 vertexA,
                float2 vertexB,
                float2 vertexC,
                float2 vertexD)
            {
                return max(
                    TriangleMask(samplePosition, vertexA, vertexB, vertexC),
                    TriangleMask(samplePosition, vertexA, vertexC, vertexD));
            }

            float ShardMask(float2 uv, float shardIndex)
            {
                if (shardIndex < 0.5)
                {
                    return QuadMask(
                        uv,
                        float2(0.08, 0.72),
                        float2(0.28, 0.91),
                        float2(0.34, 0.62),
                        float2(0.17, 0.57));
                }

                if (shardIndex < 1.5)
                {
                    return QuadMask(
                        uv,
                        float2(0.22, 0.48),
                        float2(0.44, 0.61),
                        float2(0.50, 0.38),
                        float2(0.31, 0.29));
                }

                if (shardIndex < 2.5)
                {
                    return QuadMask(
                        uv,
                        float2(0.58, 0.83),
                        float2(0.84, 0.91),
                        float2(0.78, 0.61),
                        float2(0.63, 0.56));
                }

                if (shardIndex < 3.5)
                {
                    return QuadMask(
                        uv,
                        float2(0.68, 0.45),
                        float2(0.92, 0.55),
                        float2(0.85, 0.26),
                        float2(0.66, 0.22));
                }

                if (shardIndex < 4.5)
                {
                    return QuadMask(
                        uv,
                        float2(0.42, 0.24),
                        float2(0.62, 0.32),
                        float2(0.58, 0.08),
                        float2(0.36, 0.10));
                }

                if (shardIndex < 5.5)
                {
                    return QuadMask(
                        uv,
                        float2(0.39, 0.76),
                        float2(0.53, 0.84),
                        float2(0.58, 0.67),
                        float2(0.43, 0.62));
                }

                if (shardIndex < 6.5)
                {
                    return QuadMask(
                        uv,
                        float2(0.07, 0.28),
                        float2(0.22, 0.36),
                        float2(0.25, 0.17),
                        float2(0.11, 0.11));
                }

                return QuadMask(
                    uv,
                    float2(0.80, 0.78),
                    float2(0.94, 0.84),
                    float2(0.91, 0.64),
                    float2(0.77, 0.60));
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.position);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                float2 localUv = (input.uv - _SpriteUVRect.xy) / _SpriteUVRect.zw;
                clip(ShardMask(localUv, _ShardIndex) - 0.5);

                fixed4 color = tex2D(_MainTex, input.uv) * input.color;
                clip(color.a - 0.01);
                return color;
            }
            ENDCG
        }
    }
}
