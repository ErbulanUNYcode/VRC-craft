Shader "VRC_MINE/BioneColorizer"
{
    Properties
    {
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
            
            int _Seed;

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
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                
                o.uv = v.uv * float2(256, 128);
                
                return o;
            }

            float lerp1(float a, float b, float t){return a + t * (b - a);}
            
            float fade(float t){return t * t * (3.0 - 2.0 * t);}

            float hash21(int2 p)
            {
                uint h = asuint(p.x);
                h ^= asuint(p.y) * 0x9E3779B9u;
                h ^= asuint(_Seed) * 0x85EBCA6Bu;

                h ^= h >> 16;
                h *= 0x7FEB352Du;
                h ^= h >> 15;
                h *= 0x846CA68Bu;
                h ^= h >> 16;

                return h * 0.00000000023283064365386963;
            }

            uint hash1D(int p)
            {
                uint h = asuint(p) ^ asuint(_Seed);
                h *= 0x9E3779B9u;
                h ^= h >> 16;
                return h;
            }
            
            float noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                
                float2 u = float2(fade(f.x), fade(f.y));
                
                float x1 = lerp1(a, b, u.x);
                float x2 = lerp1(c, d, u.x);
                
                return lerp1(x1, x2, u.y);
            }

            float fbm2D(float2 p,int o)
            {
                float value = 0;
                for(int i = 0; i < o; i++)
                {
                    value+=noise2D(p*(1<<i))/(1<<(i+1));
                }

                return value/(1.0-1.0/(1<<o));
            }
            
            fixed frag(v2f i) : SV_Target
            {
                return 1;
            }
            
            ENDHLSL
        }
    }
}