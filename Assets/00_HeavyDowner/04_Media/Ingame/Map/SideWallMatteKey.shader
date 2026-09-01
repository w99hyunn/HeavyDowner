Shader "HeavyDowner/Side Wall Matte Key"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _KeyStart ("Matte Key Threshold", Range(0, 1)) = 0.38
        _EdgeErosion ("Edge Erosion (Pixels)", Range(0, 8)) = 4
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
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed _KeyStart;
            fixed _EdgeErosion;

            fixed SampleMatte(float2 uv)
            {
                fixed3 sampleColor = tex2D(_MainTex, uv).rgb;
                fixed minimumChannel = min(sampleColor.r, min(sampleColor.g, sampleColor.b));
                return step(_KeyStart, minimumChannel);
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
                fixed4 color = tex2D(_MainTex, input.uv) * input.color;
                float2 erosion = _MainTex_TexelSize.xy * _EdgeErosion;
                fixed matte = SampleMatte(input.uv);
                matte = max(matte, SampleMatte(input.uv + float2(erosion.x, 0.0)));
                matte = max(matte, SampleMatte(input.uv - float2(erosion.x, 0.0)));
                matte = max(matte, SampleMatte(input.uv + float2(0.0, erosion.y)));
                matte = max(matte, SampleMatte(input.uv - float2(0.0, erosion.y)));
                matte = max(matte, SampleMatte(input.uv + erosion));
                matte = max(matte, SampleMatte(input.uv - erosion));
                matte = max(matte, SampleMatte(input.uv + float2(erosion.x, -erosion.y)));
                matte = max(matte, SampleMatte(input.uv + float2(-erosion.x, erosion.y)));
                clip(0.5 - matte);
                return color;
            }
            ENDCG
        }
    }
}
