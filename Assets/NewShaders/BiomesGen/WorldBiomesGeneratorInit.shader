Shader "VRC_MINE/WorldBiomesGenerator/InitContinents"
{
    Properties
    {
        _Seed ("Seed", Int) = 0
        _Size ("Size", Int) = 20
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityCustomRenderTexture.cginc"

            struct BiomeInitV2F
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            int _Seed;
            int _Size;

            BiomeInitV2F vert(appdata_customrendertexture v)
            {
                v2f_customrendertexture base = CustomRenderTextureVertexShader(v);
                BiomeInitV2F o;
                o.pos = base.vertex;
                o.uv = base.globalTexcoord.xy * _Size;

                return o;
            }

            uint hash3DUint(int2 p)
            {
                uint x = asuint(p.x);
                uint y = asuint(p.y);
                uint z = asuint(_Seed);

                uint n = x * 374761393u + y * 668265263u + z * 1446658333u;
                n = (n ^ (n >> 13)) * 1274126177u;

                return n;
            }

            uint frag(BiomeInitV2F i) : SV_Target
            {
                return (hash3DUint((int2)i.uv) % 9 == 0)?21:0;
            }

            ENDHLSL
        }
    }
}
