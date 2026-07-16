Shader "VRC_MINE/WorldGenerator"
{
    Properties
    {
        _Data ("Data", 2D) = "white" {}
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

            Texture2D<uint2> _Data;
            int _ChunkPosX;
            int _ChunkPosY;

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

            fixed frag(v2f i) : SV_Target
            {
                int2 uv = i.uv;
                int3 pos = int3((uv.x&15)+(_ChunkPosX<<4), uv.y, (uv.x>>4)+(_ChunkPosY<<4));
                int2 h = _Data.Load(int3(pos.xz&15,0));
                if(pos.y==0) return float(h.x)/255;
                if(h.y==3||h.y==4)h.x=40;
                if(pos.y<3&&pos.y<hash21(pos.xz)*3) return 16.0/255;
                if(h.x<pos.y) return 0;
                if(h.y==3||h.y==4) return 2;
                float3 cavePos = pos;
                cavePos.y*=1.5;
                float cave1 = (fbmCave(cavePos/40)-0.5)*30;
                float cave2 = (fbmCave((cavePos+220)/40)-0.5)*30;
                bool cave = cave1<1&&cave1>-1&&cave2<1&&cave2>-1;
                fixed x = 
                h.x<pos.y?0://air
                cave?0://cave
                (h.y<2)?
                (
                    h.x-1<pos.y?19://grass
                    h.x-(h.y==0?3:0)<pos.y?18://dirt
                    17//stone
                ):17;//stone
                if(x==17)
                {
                    float sp =hash31(pos);
                    if((hash31(pos>>2)>0.99||hash31((pos+222)>>2)>0.99)&&sp>0.3) x=20;//coal ore
                    else if(h.x-3<pos.y||hash21(pos.xz>>1)>0.3)
                    {
                        float sp1 = hash31(pos>>1);
                        float sp2 = hash31((pos+111)>>1);
                        if((sp1>0.997||sp2>0.997)&&sp>0.2) x=21;//iron ore
                        else if(pos.y<15&&(sp1>0.996||sp2>0.996)&&sp>0.5) x=22;//gold ore
                        else if(pos.y<15&&(sp1>0.994||sp2>0.994)&&sp>0.5) x=23;//diamond ore
                    }
                }
                return x/255;
            }

            ENDHLSL
        }
    }
}