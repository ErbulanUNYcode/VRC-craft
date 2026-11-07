Shader "Hidden/MineTools/StructurePreview"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "black" {}
        _MaskTex ("Leaf Mask", 2D) = "black" {}
        _LeafTint ("Leaf Tint", Color) = (1, 1, 1, 1)
        _BiomeLeafColor ("Biome Leaf Color", Color) = (0.45, 1, 0.45, 1)
        _PreviewBrightness ("Preview Brightness", Float) = 1
        _FaceBrightness ("Face Brightness", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _MaskTex;
            fixed4 _LeafTint;
            fixed4 _BiomeLeafColor;
            float _PreviewBrightness;
            float _FaceBrightness;
            float _HasMask;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv);
                fixed4 maskPixel = tex2D(_MaskTex, i.uv);
                if (_HasMask > 0.5 && maskPixel.a > 0.5)
                {
                    float strength = maskPixel.g * _LeafTint.g;
                    color.rgb = _BiomeLeafColor.rgb * strength;
                    color.r += _LeafTint.r * maskPixel.g;
                    color.b += _LeafTint.b * maskPixel.g;
                    color.rgb = saturate(color.rgb);
                }
                color.rgb *= _PreviewBrightness * _FaceBrightness;
                color.rgb = 1.0 - exp(-saturate(color.rgb) * 3.0);
                return color;
            }
            ENDCG
        }
    }
}
