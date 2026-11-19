Shader "VRC_MINE/WorldShadow"
{
    Properties
    {
        _MainTex ("Texture", 3D) = "white" {}
        _Optimizer ("Optimizer", 2D) = "white" {}
        _Atlas ("Block Atlas", 2DArray) = "" {}
        _Special ("Special", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Cull Off
        ZWrite On
        ZTest LEqual
        Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #include "UnityCG.cginc"

            Texture3D<uint> _MainTex;
            UNITY_DECLARE_TEX2DARRAY(_Atlas);
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
                nointerpolation fixed faceData : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o = (v2f)0;
                if(!_Special)
                {
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.faceData = 0;
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
                uint axis = v.color.x ? 0u : v.color.y ? 1u : 2u;
                o.faceData = axis * 0.125;
                o.depth = -mul(UNITY_MATRIX_MV, v.vertex).z/_ProjectionParams.z;
                return o;
            }

            uint block(int3 pos)
            {
                if(pos.y>127||pos.y<0) return 0;
                return _MainTex.Load(int4(pos.x & 511, 511 - (pos.z & 511), pos.y, 0));
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
                uint axis = (uint)round(i.faceData * 8.0);
                int3 offset = axis == 0u ? int3(1,0,0) : axis == 1u ? int3(0,1,0) : int3(0,0,1);
                int3 pos = (int3)floor(p);
                if (axis == 0u) pos.x = (int)round(p.x);
                else if (axis == 1u) pos.y = (int)round(p.y);
                else pos.z = (int)round(p.z);
                uint b1 = block(pos);
                uint b2 = block(pos-offset);
                if ((b1 > 8u && b2 > 8u) || (b1 == 0u && b2 == 0u) || (b1 == 1u && b2 == 1u)) discard;
                uint id = b1 != 0u ? b1 : b2;
                if (id > 8u) return i.depth;
                float2 faceUV = axis == 0u ? p.zy : axis == 1u ? p.xz : p.xy;
                uint face = axis == 0u ? (b1 != 0u ? 0u : 1u) : axis == 1u ? (b1 != 0u ? 2u : 3u) : (b1 != 0u ? 4u : 5u);
                float2 atlasUV = (float2(id & 15u, id >> 4) + frac(faceUV)) / 16.0;
                float footprint = max(length(ddx(faceUV)), length(ddy(faceUV))) * 32.0;
                float mip = clamp(log2(max(footprint, 1.0)), 0.0, 5.0);
                clip(UNITY_SAMPLE_TEX2DARRAY_LOD(_Atlas, float3(atlasUV, face), mip).a - 0.25);
                return i.depth;
            }

            ENDCG
        }
    }
}
