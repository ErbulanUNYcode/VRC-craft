Shader "VRC_MINE/WorldShadow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Optimizer ("Optimizer", 2D) = "white" {}
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
            Texture2D<uint4> _Optimizer;
            bool _Special;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                noperspective float3 inter : TEXCOORD0;
                noperspective float depth : TEXCOORD1;
                nointerpolation bool3 offset : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                if(!_Special)
                {
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.offset = 0;
                    o.depth = -mul(UNITY_MATRIX_MV, v.vertex).z/_ProjectionParams.z;
                    return o;
                }
                if(v.color.y!=1||v.vertex.y>0)
                {
                    int2 ch = unity_ObjectToWorld._m03_m23;
                    ch>>=5;
                    ch&=15;
                    ch*=int2(32,12);
                    if(v.color.x==1)
                    {
                        uint4 t1 = _Optimizer.Load(int3(v.uv+ch,0));
                        ch.y+=192;
                        uint4 t2 = _Optimizer.Load(int3(v.uv+ch,0));
                        if(t1.y==0&&t2.y==0)
                        {
                            o.vertex = float4(0,0,-1,0);
                            return o;
                        }
                        v.vertex.y-=(v.color.a%1)==0?0:32;
                        v.vertex.y+=v.color.a%1==0?min(t1.x,t2.x):max(t1.y,t2.y);
                        v.vertex.z=v.color.a>0.5?min(t1.z,t2.z):max(t1.w,t2.w);
                    }
                    else if(v.color.z==1)
                    {
                        uint4 t1 = _Optimizer.Load(int3(v.uv+ch,0));
                        ch.y+=192;
                        uint4 t2 = _Optimizer.Load(int3(v.uv+ch,0));
                        if(t1.y==0&&t2.y==0)
                        {
                            o.vertex = float4(0,0,-1,0);
                            return o;
                        }
                        v.vertex.y-=v.color.a%1==0?0:32;
                        v.vertex.y+=v.color.a%1==0?min(t1.x,t2.x):max(t1.y,t2.y);
                        v.vertex.x=v.color.a>0.5?min(t1.z,t2.z):max(t1.w,t2.w);
                    }
                    else if(v.color.y==1)
                    {
                        uint4 t1 = _Optimizer.Load(int3(v.uv+ch,0));
                        ch.y+=192;
                        uint4 t2 = _Optimizer.Load(int3(v.uv+ch,0));
                        if(t1.y==0&&t2.y==0)
                        {
                            o.vertex = float4(0,0,-1,0);
                            return o;
                        }
                        v.vertex.z=v.color.a%1==0?min(t1.x,t2.x):max(t1.y,t2.y);
                        v.vertex.x=v.color.a>0.5?min(t1.z,t2.z):max(t1.w,t2.w);
                    }
                }
                o.inter = unity_ObjectToWorld._m03_m13_m23+v.vertex.xyz;
                if(_Special)
                {
                    float3 objectPos = round(unity_ObjectToWorld._m03_m13_m23);

                    float3 relativePos = v.vertex.xyz + objectPos - _WorldSpaceCameraPos;

                    float3 viewPos = mul((float3x3)UNITY_MATRIX_V, relativePos);

                    o.vertex = mul(UNITY_MATRIX_P, float4(viewPos, 1.0));
                }
                else
                o.vertex = UnityObjectToClipPos(v.vertex);
                int3 p = o.inter*v.color.xyz;
                o.offset = v.color.xyz;
                o.depth = -mul(UNITY_MATRIX_MV, v.vertex).z/_ProjectionParams.z;
                return o;
            }

            uint block(int3 pos)
            {
                if(pos.y>127||pos.y<0) return 0;
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

            float frag(v2f i) : SV_Target
            {
                if(!_Special) return i.depth;
                float3 p = i.inter;
                int3 pos =floor(p);
                uint b1 = block(pos);
                uint b2 = block(pos-i.offset);
                if ((b1>0&&b2>0) || b1==b2)discard;
                return i.depth;
            }

            ENDCG
        }
    }
}