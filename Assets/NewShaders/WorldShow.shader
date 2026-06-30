Shader "VRC_MINE/WorldShow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _InitChunks ("InitChunks", 2D) = "white" {}
        _Optimizer ("Optimizer", 2D) = "white" {}
        _Atlas ("Atlas", 2D) = "white" {}
        _AtlasFlipMap ("AtlasFlipMap", 2D) = "white" {}
        _Light ("Light", 2D) = "white" {}
        _FogColor ("FogColor", Color) = (0,0,0,0)
        _ShadowMap ("ShadowMap", 2D) = "white" {}
        _ShadowMap1 ("ShadowMap", 2D) = "white" {}
        _Special ("Special", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Cull Off
        Blend One Zero

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            Texture2D<fixed> _MainTex;
            Texture2D<fixed4> _InitChunks;
            Texture2D<uint4> _Optimizer;
            sampler2D _Atlas;
            Texture2D<fixed4> _AtlasFlipMap;
            sampler2D _Light;
            fixed4 _FogColor;
            float4x4 _ShadowMatrix;
            sampler2D _ShadowMap;
            sampler2D _ShadowMap1;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 inter : TEXCOORD0;
                float3 shadow : TEXCOORD1;
                fixed fog : TEXCOORD2;
                nointerpolation int placeCoord : TEXCOORD3;
                nointerpolation bool3 offset : TEXCOORD4;
                nointerpolation bool face : TEXCOORD5;
            };

            v2f vert(appdata v)
            {
                v2f o;

                if(v.color.y!=1||v.vertex.y>0)
                {
                    int2 ch = unity_ObjectToWorld._m03_m23;
                    ch>>=5;
                    ch&=15;
                    ch*=int2(32,12);
                    if(v.color.x)
                    {
                        o.face =_WorldSpaceCameraPos.x<unity_ObjectToWorld._m03+v.vertex.x;
                        ch.y+=o.face?192:0;
                        uint4 t = _Optimizer.Load(int3(v.uv+ch,0));
                        if(t.y==0)
                        {
                            o.vertex = float4(0,0,-1,0);
                            return o;
                        }
                        o.shadow.z=(_ShadowMatrix._m20<0)==o.face?10:0;
                        v.vertex.y-=(v.color.a%1)==0?0:32;
                        v.vertex.y+=v.color.a%1==0?t.x:t.y;
                        v.vertex.z=v.color.a>0.5?t.z:t.w;
                    }
                    else if(v.color.z)
                    {
                        o.face =_WorldSpaceCameraPos.z<unity_ObjectToWorld._m23+v.vertex.z;
                        ch.y+=o.face?192:0;
                        uint4 t = _Optimizer.Load(int3(v.uv+ch,0));
                        if(t.y==0)
                        {
                            o.vertex = float4(0,0,-1,0);
                            return o;
                        }
                        o.shadow.z=(_ShadowMatrix._m22<0)==o.face?10:0;
                        v.vertex.y-=v.color.a%1==0?0:32;
                        v.vertex.y+=v.color.a%1==0?t.x:t.y;
                        v.vertex.x=v.color.a>0.5?t.z:t.w;
                    }
                    else if(v.color.y)
                    {
                        o.face =_WorldSpaceCameraPos.y<unity_ObjectToWorld._m13+v.vertex.y;
                        ch.y+=o.face?192:0;
                        uint4 t = _Optimizer.Load(int3(v.uv+ch,0));
                        if(t.y==0)
                        {
                            o.vertex = float4(0,0,-1,0);
                            return o;
                        }
                        o.shadow.z=(_ShadowMatrix._m21<0)==o.face?10:0;
                        v.vertex.z=v.color.a%1==0?t.x:t.y;
                        v.vertex.x=v.color.a>0.5?t.z:t.w;
                    }
                }
                else
                {
                    o.face =_WorldSpaceCameraPos.y<unity_ObjectToWorld._m13+v.vertex.y;
                }
                o.inter = unity_ObjectToWorld._m03_m13_m23+v.vertex.xyz;
                float3 camOffset = (_WorldSpaceCameraPos - o.inter)/280;
                if(o.shadow.z==0)
                o.shadow= mul(_ShadowMatrix, float4(o.inter, 1)).xyz/2+0.5;
                else
                o.shadow.xy= mul(_ShadowMatrix, float4(o.inter, 1)).xy/2+0.5;
                o.fog = dot(camOffset, camOffset);
                float3 objectPos = round(unity_ObjectToWorld._m03_m13_m23);

float3 relativePos = v.vertex.xyz + objectPos - _WorldSpaceCameraPos;

float3 viewPos = mul((float3x3)UNITY_MATRIX_V, relativePos);

o.vertex = mul(UNITY_MATRIX_P, float4(viewPos, 1.0));
                
                int3 p = o.inter*v.color.xyz;
                o.placeCoord = p.x+p.y+p.z;
                o.offset = v.color.xyz;

                return o;
            }

            uint block(int3 pos)
            {
                if(pos.y>127&&pos.y<0) return 0;
                int2 ch = ((pos.xz>>4)&31)*int2(256,128);
                pos&=int3(15,127,15);
                pos.x+=pos.z*16;
                return _MainTex.Load(int3(pos.xy+ch,0))*255;
            }

            int Hash3(int x, int y, int z, int v)
            {
                int h = x * 73856093
                      ^ y * 19349663
                      ^ z * 83492791
                      ^ v * 2654435761;

                h ^= (h >> 13);
                h *= 1274126177;
                h ^= (h >> 16);

                return h & 7;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 p = i.offset?i.placeCoord:i.inter;
                int3 pos =floor(p);
                uint b1 = block(pos);
                uint b2 = block(pos-i.offset);
                if ((b1>0&&b2>0) || (i.face?b1:b2)==0)discard;
                b1 = i.face?b1:b2;
                float2 uv = i.offset.x?frac(p.zy):i.offset.y?frac(p.xz):frac(p.xy);
                fixed dv1=block(pos-(i.face?i.offset:0) + (i.offset.x?int3(0,1,0):i.offset.y?int3(0,0,1):int3(0,1,0)))==0;
                fixed dv2=block(pos-(i.face?i.offset:0) + (i.offset.x?int3(0,-1,0):i.offset.y?int3(0,0,-1):int3(0,-1,0)))==0;
                fixed dh1=block(pos-(i.face?i.offset:0) + (i.offset.x?int3(0,0,1):i.offset.y?int3(1,0,0):int3(1,0,0)))==0;
                fixed dh2=block(pos-(i.face?i.offset:0) + (i.offset.x?int3(0,0,-1):i.offset.y?int3(-1,0,0):int3(-1,0,0)))==0;
                float ao = (dv1*uv.y+dv2*(1-uv.y))*(dh1*uv.x+dh2*(1-uv.x));
                ao = ao*0.8+0.2;
                float2 uvOffset = i.offset.x?0:i.offset.y?int2(0,1):int2(0,2);
                uvOffset.x+=i.face + (b1&15)*2;
                uvOffset.y+=(b1>>4)*3;
                fixed4 flip=_AtlasFlipMap.Load(int3(uvOffset,0));
                int fliper = Hash3(pos.x,pos.y,pos.z,round(flip.w*255));
                if(flip.x && fliper&1)uv=uv.yx;
                if(flip.y && (fliper>>1)&1)uv.x=1-uv.x;
                if(flip.z && (fliper>>2)&1)uv.y=1-uv.y;
                uv+=uvOffset;
                uv/=32;
                fixed4 c = tex2D(_Atlas, uv);
                float shadow;
                if(i.shadow.z>9.9) shadow = 0.4;
                else
                {
                    shadow = i.shadow.x<0||i.shadow.x>1||i.shadow.y<0||i.shadow.y>1||i.shadow.z<0.05||i.shadow.z>0.95;
                    if(!shadow) shadow = 1-clamp(i.shadow.z-tex2D(_ShadowMap, i.shadow.xy).r,0,0.002)*400;
                    else
                    {
                        i.shadow.xy=(i.shadow.xy-0.5)/10+0.5;
                        shadow = i.shadow.x<0||i.shadow.x>1||i.shadow.y<0||i.shadow.y>1||i.shadow.z<0.05||i.shadow.z>0.95;
                        if(!shadow) shadow = 1-clamp(i.shadow.z-tex2D(_ShadowMap1, i.shadow.xy).r,0,0.002)*400;
                    }
                }
                c.rgb = (c.rgb*ao*(1-i.fog)*shadow+_FogColor*i.fog);
                return c;
            }

            ENDCG
        }
    }
}