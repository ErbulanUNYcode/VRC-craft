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

            float lerp1(float a, float b, float t){return a + t * (b - a);}

            float fade(float t){return t * t * (3.0 - 2.0 * t);}

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

            float noise01(float2 p, float ch)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash21(i)>ch;
                float b = hash21(i + float2(1, 0))>ch;
                float c = hash21(i + float2(0, 1))>ch;
                float d = hash21(i + float2(1, 1))>ch;

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
                r=lerp(b.x,r,max(b.y-0.2,0)*1.25);
                r=lerp(c.x,r,max(c.y-0.2,0)*1.25);
                r=lerp(d.x,r,max(d.y-0.2,0)*1.25);

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
                float2 pos = int2(uv.x+(_ChunkPosX<<4), uv.y+(_ChunkPosY<<4));
                float height = fbm((pos.xy)/100,4);
                uint b = 0;//classic
                
                float desert=fbm((pos.xy-1204)/1024,5)+height/4;

                if(desert<0.8)
                {
                    desert-=0.8;
                    desert=desert*10;
                    desert+=0.8;
                    desert=max(desert,0);
                }
                else
                {
                    b=8;
                }
                height*=20;
                float m = fbmm((pos.xy+1024)/150,3)*(230-desert*25)-150+height*2;
                height+=40;
                float wom = height;
                if(m>=height)//mountain
                {
                    height=m;
                    if(b!=8) b=fbm((pos.xy-200)/10,4)>0.5?1:2;
                }

                float l = pow(fbm((pos.xy-1524)/300+desert/2,5)*6,4)+15;//lake

                float lr = max(pow(fbm((pos.xy-1324)/1550,3),6)*2,(m-35)/6)+desert*desert*5;
                if(lr>5)
                {
                    lr-=5;
                    lr*=lr;
                    lr*=lr;
                    lr+=5;
                }
                l = min(pow(abs(fbm((pos.xy+1024)/512,5)-0.5)*(120-(50-clamp(l,30,50))*10),2)+34+lr,l);//river
                if(l>40)
                {
                    l-=40;
                    l/=3;
                    l+=40;
                }
                l+=fbm((pos.xy+124)/30,3)*6-3;
                if(l>wom)
                {
                    l-=wom;
                    l*=l;
                    l+=wom;
                }
                if(l<height+1)//water
                {
                    height=min(l,height);
                    float r=fbm((pos.xy-124)/15,2);
                    if(l<44+r*4-desert*5)
                    {
                        b = abs(l-40)<(r*7-3)?3:l>40?4:l>37?5:noise01(pos.xy/7.3,0.98)>0.6||noise01(pos.xy/7.3+1594.5,0.98)>0.6?6:7;
                    }
                }

                return uint2(height,b);
            }

            ENDHLSL
        }
    }
}