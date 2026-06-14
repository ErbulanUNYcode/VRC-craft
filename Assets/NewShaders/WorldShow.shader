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
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Cull Off

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
                nointerpolation int placeCoord : TEXCOORD1;
                nointerpolation bool3 offset : TEXCOORD2;
                nointerpolation bool face : TEXCOORD3;
            };

            v2f vert(appdata v)
            {
                v2f o;

                if(v.color.y!=1||v.vertex.y>0)
                {
                    int2 ch = mul(unity_ObjectToWorld, float4(0,0,0,1)).xz;
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
                        v.vertex.z=v.color.a%1==0?t.x:t.y;
                        v.vertex.x=v.color.a>0.5?t.z:t.w;
                    }
                }
                else
                {
                    o.face =_WorldSpaceCameraPos.y<unity_ObjectToWorld._m13+v.vertex.y;
                }
                o.inter = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
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
                return c;
                /*{
                    float3 f = abs(frac(p)-0.5) * (1-i.offset);
                    if(f.x<0.495&&f.y<0.495&&f.z<0.49) discard;
                    if(i.offset.x==0&&i.offset.z==0) discard;
                    return fixed4(0,0,0,1);
                }*/
            }

            ENDCG
        }
    }
}