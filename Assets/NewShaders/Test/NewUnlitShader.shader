Shader "Custom/SimpleWarpNoise"
{
    Properties
    {
        _Size ("Size", Float) = 1
        _Seed ("Seed", Int) = 0
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

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float2 p : TEXCOORD0; };

            float _Size;
            int _Seed;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.p = v.vertex.xy;
                return o;
            }

            float hash(float2 p)
            {
                uint2 x = p > 0 ? uint2(p) : 4294967295 - uint2(-p);
                uint a=x.y;
                a ^= a >> 16;
                a *= 0x7feb352d;
                a ^= a >> 15;
                a *= 0x846ca68b;
                a ^= a >> 16;
                a&=x.x;
                return float(a)/255;
            }

            float noise(float2 p)
            {
                p*=_Size;
                float a = hash(p);
                p.x++;
                float b = hash(p);
                p.y++;
                float c = hash(p);
                p.x--;
                float d = hash(p);

                float2 f = frac(p);

                float ab = lerp(a, b, f.x);
                float cd = lerp(d, c, f.x);
                float result = lerp(ab, cd, f.y);

                return result;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 p = i.p;

                float n = noise(p);
                return float4(n,n,n,1);
            }

            ENDHLSL
        }
    }
}