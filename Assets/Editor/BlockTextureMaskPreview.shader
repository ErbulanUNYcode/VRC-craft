Shader "Hidden/MineTools/BlockTextureMaskPreview"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "black" {}
        _MaskTex ("Biome Mask", 2D) = "black" {}
        _MaskTint ("Mask Tint", Color) = (0.35, 1, 0.35, 1)
        _LeafTint ("Leaf Tint", Color) = (1, 1, 1, 1)
        _BiomeLeafColor ("Preview Biome Leaf Color", Color) = (0.45, 1, 0.45, 1)
        _PreviewGamma ("Preview Color Curve", Range(0.15, 2.0)) = 0.3
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
            fixed4 _MaskTint;
            fixed4 _LeafTint;
            fixed4 _BiomeLeafColor;
            float _PreviewGamma;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

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
                if (maskPixel.a > 0.5)
                {
                    // Match the packed foliage format used by MineAtlas:
                    // biome color is controlled by the packed green channel,
                    // while authored red and blue are additive offsets.
                    fixed biomeStrength = maskPixel.g * _LeafTint.g;
                    fixed maskBrightness = maskPixel.g;
                    color.rgb = _BiomeLeafColor.rgb * biomeStrength;
                    color.r += _LeafTint.r * maskBrightness;
                    color.b += _LeafTint.b * maskBrightness;
                    color.rgb = saturate(color.rgb);
                }
                // Aggressive preview lift: brightens mid-tones much more than
                // a simple power curve while preserving channel differences.
                color.rgb = 1.0 - exp(-saturate(color.rgb) * 3.0);
                return color;
            }
            ENDCG
        }
    }
}
