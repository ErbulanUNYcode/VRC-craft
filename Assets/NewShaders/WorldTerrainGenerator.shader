Shader "VRC_MINE/WorldTerrainGenerator"
{
    Properties
    {
        _ChunkPosX ("ChunkPosX", Int) = 0
        _ChunkPosY ("ChunkPosY", Int) = 0
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

                o.uv = v.uv * float2(16, 16);

                return o;
            }

            float hash31(int3 p)
            {
                int n = p.x * 374761393 + p.y * 668265263 + p.z * 1446658333;
                n = (n ^ (n >> 13)) * 1274126177;
                return frac(n * 0.00000000023283064365386963); // 1/2^32
            }

            float lerp1(float a, float b, float t){return a + t * (b - a);}

            float fade(float t){return t * t * (3.0 - 2.0 * t);}

            float noise3D(float3 p)
            {
                int3 i = (int3)floor(p);
                float3 f = frac(p);

                float a = hash31(i);
                float b = hash31(i + int3(1, 0, 0));
                float c = hash31(i + int3(0, 1, 0));
                float d = hash31(i + int3(1, 1, 0));

                float e = hash31(i + int3(0, 0, 1));
                float f1 = hash31(i + int3(1, 0, 1));
                float g = hash31(i + int3(0, 1, 1));
                float h = hash31(i + int3(1, 1, 1));

                float3 u = float3(fade(f.x), fade(f.y), fade(f.z));

                float x00 = lerp(a, b, u.x);
                float x10 = lerp(c, d, u.x);
                float x01 = lerp(e, f1, u.x);
                float x11 = lerp(g, h, u.x);

                float y0 = lerp(x00, x10, u.y);
                float y1 = lerp(x01, x11, u.y);

                return lerp(y0, y1, u.z);
            }

            float fbm3D(float3 p, int o)
            {
                float value = 0;

                for(int i = 0; i < o; i++)
                {
                    value += noise3D(p * (1 << i)) / (1 << (i + 1));
                }

                return value / (1.0 - 1.0 / (1 << o));
            }

            float fbmCave(float3 p)
            {
                float value = 0;

                value += noise3D(p) * 0.5;
                value += noise3D(p * 2.0) * 0.25;

                return value / 0.75;
            }

            float hash21(int2 p)
            {
                int n = p.x * 374761393 + p.y * 668265263;
                n = (n ^ (n >> 13)) * 1274126177;
                return frac(n * 0.00000000023283064365386963); // 1/2^32
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

            float noise2Dmountain(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float2 u = float2(fade(f.x), fade(f.y));

                float2 a = float2(hash21(i), min(dot(u,u),1));
                if(a.x<0.2) a.x=0;
                u.x=1-u.x;
                float2 b = float2(hash21(i + float2(1, 0)),min(dot(u,u),1));
                if(b.x<0.25) b.x=0;
                u=1-u;
                float2 c = float2(hash21(i + float2(0, 1)),min(dot(u,u),1));
                if(c.x<0.25) c.x=0;
                u.x=1-u.x;
                float2 d = float2(hash21(i + float2(1, 1)),min(dot(u,u),1));
                if(d.x<0.25) d.x=0;
                
                float2 t;
                if(a.x>b.x){t=a;a=b;b=t;}
                if(c.x>d.x){t=c;c=d;d=t;}
                if(a.x>c.x){t=a;a=c;c=t;}
                if(b.x>d.x){t=b;b=d;d=t;}
                if(b.x>c.x){t=b;b=c;c=t;}
                
                b.y*=b.y;
                c.y*=c.y;
                d.y*=d.y;
                float r = a.x;
                r=lerp(b.x,r,b.y*b.y);
                r=lerp(c.x,r,c.y*c.y);
                r=lerp(d.x,r,d.y*d.y);

                return r;
            }
            
            float fbm(float2 p,int o)
            {
                float value = 0;
                for(int i = 0; i < o; i++)
                {
                    value+=noise2D(p*(1<<i))/(1<<(i+1));
                }

                return value/(1.0-1.0/(1<<o));
            }

            float fbmm(float2 p,int o)
            {
                float value = 0;
                for(int i = 0; i < o; i++)
                {
                    value+=noise2Dmountain(p*(1<<i))/(1<<(i+1));
                }

                return value/(1.0-1.0/(1<<o));
            }

            uint2 frag(v2f i) : SV_Target
            {
                int2 uv = i.uv;
                int2 pos = int2(uv.x+(_ChunkPosX<<4), uv.y+(_ChunkPosY<<4));
                float h1 = fbm(float2(pos.xy)/100,4)*20;
                uint b = 0;//classic
                bool br = fbm(float2(pos.xy-200)/10,4)>0.7;

                float h2 = fbmm(float2(pos.xy+1024)/150,3)*250-150+h1;
                h1+=40;
                float wom = h1;
                if(h2>=h1)
                {
                    h1=h2;
                    b=br?1:2;
                }

                
                
                h2 = pow(fbm(float2(pos.xy-1024)/300,5),2)*200+10;
                if(h2>wom)
                {
                    h2-=wom;
                    h2*=h2;
                    h2+=wom;
                }
                if(h2<h1)
                {
                    h1=h2;
                    if(h2<40)
                    {
                        b=br?3:4;
                    }
                }

                return uint2(h1,b);
            }

            ENDHLSL
        }
    }
}