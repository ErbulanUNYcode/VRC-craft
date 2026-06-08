Shader "VRC_MINE/WorldShow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Optimizer ("Optimizer", 2D) = "white" {}
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

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 inter : TEXCOORD0;
                nointerpolation int placeCoord : TEXCOORD1;
                nointerpolation int3 offset : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 cam = _WorldSpaceCameraPos.xyz;
                if(v.color.y==1 && v.vertex.y>0)
                {
                    int2 texPos=int2(0,128);
                    if(v.vertex.y<cam.y) texPos.y+=192;
                    uint id = v.vertex.y-1;
                    v.vertex.xz=0;
                    int2 ch = int2(round(mul(unity_ObjectToWorld, v.vertex).xz))/32;
                    ch&=15;
                    id+=(ch.x<<7)+(ch.y<<11);
                    texPos.x+=id&511;
                    texPos.y+=id>>9;
                    uint4 opt=_Optimizer.Load(int3(texPos,0));
                    v.vertex.x=v.color.a>0.5?opt.x:opt.y;
                    v.vertex.z=v.color.a%1==0?opt.z:opt.w;
                }
                else if(v.color.x==1)
                {
                    int2 texPos=int2(0,0);
                    v.vertex.z=0;
                    if(v.color.a%1!=0) v.vertex.y-=32;
                    int3 p = int3(round(mul(unity_ObjectToWorld, v.vertex).xyz));
                    p.yz>>=5;
                    texPos.x = p.x&511;
                    if(p.x>cam.x) texPos.y+=192;
                    texPos.y+=p.y;
                    texPos.y+=(p.z&15)<<2;
                    uint4 opt=_Optimizer.Load(int3(texPos,0));
                    v.vertex.z = v.color.a>0.5?opt.x:opt.y;
                    v.vertex.y+= v.color.a%1==0?opt.z:opt.w;
                }
                else if(v.color.z==1)
                {
                    int2 texPos=int2(0,64);
                    v.vertex.x=0;
                    if(v.color.a%1!=0) v.vertex.y-=32;
                    int3 p = int3(round(mul(unity_ObjectToWorld, v.vertex).xyz));
                    p.xy>>=5;
                    texPos.x = p.z&511;
                    if(p.z>cam.z) texPos.y+=192;
                    texPos.y+=p.y;
                    texPos.y+=(p.x&15)<<2;
                    uint4 opt=_Optimizer.Load(int3(texPos,0));
                    v.vertex.x = v.color.a>0.5?opt.x:opt.y;
                    v.vertex.y+= v.color.a%1==0?opt.z:opt.w;
                }
                
                o.inter = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                int3 p = o.inter*v.color;
                o.placeCoord = p.x+p.y+p.z;
                o.offset = v.color;
                return o;
            }

            fixed block(int3 pos)
            {
                int2 ch = ((pos.xz>>4)&31)*int2(256,128);
                pos&=int3(15,127,15);
                pos.x+=pos.z*16;
                return _MainTex.Load(int3(pos.xy+ch,0));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 p = i.inter*(1-i.offset)+i.placeCoord*i.offset;
                int3 pos =floor(p);
                fixed b1 = block(pos);
                fixed b2 = block(pos-i.offset);
                if (b1 == b2)
                {
                    /*float3 f = abs(frac(p)-0.5) * (1-i.offset);
                    if(f.x<0.45&&f.y<0.49&&f.z<0.45) discard;
                    if(i.offset.z==0) */discard;
                    //return fixed4(0,0,0,1);
                }
                return fixed4(i.offset,1);
            }

            ENDCG
        }
    }
}