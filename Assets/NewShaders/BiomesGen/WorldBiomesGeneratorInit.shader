Shader "VRC_MINE/WorldBiomesGenerator"
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            int _Seed;
            int _Size;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                o.uv = v.uv * _Size;

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

            uint frag(v2f i) : SV_Target
            {
                int2 pos = (int2)i.uv;

                if(hash3DUint(pos) % 9 == 0)
                    return 1;

                return 0;
            }

            ENDHLSL
        }
    }
}