Shader "Custom/UV_Write"
{
    Properties
    {
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

            int _ChunkPosX;
            int _ChunkPosY;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                o.uv = v.uv * float2(256, 128);

                return o;
            }

            float hash(float3 p)
            {
                p = frac(p * float3(123.34, 456.21, 789.12));
                p += dot(p, p.yzx + 45.32);
                return frac(p.x * p.y * p.z);
            }

            float lerp1(float a, float b, float t){return a + t * (b - a);}

            float fade(float t){return t * t * (3.0 - 2.0 * t);}

            float noise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);

                float a = hash(i);
                float b = hash(i + float3(1, 0, 0));
                float c = hash(i + float3(0, 1, 0));
                float d = hash(i + float3(1, 1, 0));

                float e = hash(i + float3(0, 0, 1));
                float f1 = hash(i + float3(1, 0, 1));
                float g = hash(i + float3(0, 1, 1));
                float h = hash(i + float3(1, 1, 1));

                float3 u = float3(fade(f.x), fade(f.y), fade(f.z));

                float x00 = lerp1(a, b, u.x);
                float x10 = lerp1(c, d, u.x);
                float x01 = lerp1(e, f1, u.x);
                float x11 = lerp1(g, h, u.x);

                float y0 = lerp1(x00, x10, u.y);
                float y1 = lerp1(x01, x11, u.y);

                return lerp1(y0, y1, u.z);
            }

            float fbm3D(float3 p, int o)
            {
                float value = 0;

                for(int i = 0; i < o; i++)
                {
                    value += noise(p * (1 << i)) / (1 << (i + 1));
                }

                return value / (1.0 - 1.0 / (1 << o));
            }

            float fbmCave(float3 p)
            {
                float value = 0;

                value += noise(p) * 0.5;
                value += noise(p * 2.0) * 0.25;

                return value / 0.75;
            }
            
            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));

                float2 u = float2(fade(f.x), fade(f.y));

                float x1 = lerp1(a, b, u.x);
                float x2 = lerp1(c, d, u.x);

                return lerp1(x1, x2, u.y);
            }
            float fbm(float2 p,int o)
            {
                float value = 0;
                for(int i = 0; i < o; i++)
                {
                    value+=noise(p*(1<<i))/(1<<(i+1));
                }

                return value/(1.0-1.0/(1<<o));
            }

            fixed frag(v2f i) : SV_Target
            {
                int2 uv = i.uv;
                int3 pos = int3((uv.x&15)+(_ChunkPosX<<4), uv.y, (uv.x>>4)+(_ChunkPosY<<4));
                float h = fbm(float2(pos.xz)/100,4)*32+32;

                float x = h<pos.y?0:fbmCave(float3(pos)/25)<0.5?0:h-3<pos.y?0.5:1;

                return x;
            }

            ENDHLSL
        }
    }
}