Shader "VRC_MINE/WorldShow"
{
    Properties
    {
        _MainTex ("Texture", 3D) = "white" {}
        _InitChunks ("InitChunks", 2D) = "white" {}
        _Optimizer ("Optimizer", 2D) = "white" {}
        _Atlas ("Atlas", 2DArray) = "" {}
        _AtlasFlipMap ("AtlasFlipMap", 2DArray) = "" {}
        _BiomeLeafColor ("Foliage tint", Color) = (0.45,0.7,0.3,1)
        _FogColor ("FogColor", Color) = (1,1,1,1)
        _WorldBrightness ("World Brightness", Range(1, 1.25)) = 1.08
        _UpLightColor ("UpLightColor", Color) = (1,1,1,1)
        _DownLightColor ("DownLightColor", Color) = (1,1,1,1)
        _RightLightColor ("RightLightColor", Color) = (1,1,1,1)
        _LeftLightColor ("LeftLightColor", Color) = (1,1,1,1)
        _FrontLightColor ("FrontLightColor", Color) = (1,1,1,1)
        _BackLightColor ("BackLightColor", Color) = (1,1,1,1)
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
            #pragma target 4.5
            #include "UnityCG.cginc"
            #include "../Optimization/SurfaceVisibility.cginc"
            
            Texture3D<uint> _MainTex;
            Texture2D<fixed2> _LightTex;
            Texture2D<fixed4> _InitChunks;
            Texture2D<uint4> _Optimizer;
            UNITY_DECLARE_TEX2DARRAY(_Atlas);
            Texture2DArray<float4> _AtlasFlipMap;
            float4 _BiomeLeafColor;
            fixed4 _FogColor;
            float _WorldBrightness;
            fixed4 _UpLightColor;
            fixed4 _DownLightColor;
            fixed4 _RightLightColor;
            fixed4 _LeftLightColor;
            fixed4 _FrontLightColor;
            fixed4 _BackLightColor;
            float4x4 _ShadowMatrix;
            float4 _ShadowLodOffset;
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
                nointerpolation fixed faceData : TEXCOORD3;
                nointerpolation fixed4 light : TEXCOORD6;
            };

            v2f vert(appdata v)
            {
                v2f o = (v2f)0;
                bool face;

                if(v.color.y!=1||v.vertex.y>0)
                {
                    int2 ch = unity_ObjectToWorld._m03_m23;
                    ch>>=5;
                    ch&=15;
                    ch*=int2(32,12);
                    if(v.color.x)
                    {
                        face =_WorldSpaceCameraPos.x<unity_ObjectToWorld._m03+v.vertex.x;
                        ch.y+=face?192:0;
                        uint4 t = _Optimizer.Load(int3(v.uv+ch,0));
                        if(t.y==0)
                        {
                            o.vertex = float4(0,0,-1,0);
                            return o;
                        }
                        o.light=face?_LeftLightColor:_RightLightColor;
                        v.vertex.y-=(v.color.a%1)==0?0:32;
                        v.vertex.y+=v.color.a%1==0?t.x:t.y;
                        v.vertex.z=v.color.a>0.5?t.z:t.w;
                    }
                    else if(v.color.z)
                    {
                        face =_WorldSpaceCameraPos.z<unity_ObjectToWorld._m23+v.vertex.z;
                        ch.y+=face?192:0;
                        uint4 t = _Optimizer.Load(int3(v.uv+ch,0));
                        if(t.y==0)
                        {
                            o.vertex = float4(0,0,-1,0);
                            return o;
                        }
                        o.light=face?_BackLightColor:_FrontLightColor;
                        v.vertex.y-=v.color.a%1==0?0:32;
                        v.vertex.y+=v.color.a%1==0?t.x:t.y;
                        v.vertex.x=v.color.a>0.5?t.z:t.w;
                    }
                    else if(v.color.y)
                    {
                        face =_WorldSpaceCameraPos.y<unity_ObjectToWorld._m13+v.vertex.y;
                        ch.y+=face?192:0;
                        uint4 t = _Optimizer.Load(int3(v.uv+ch,0));
                        if(t.y==0)
                        {
                            o.vertex = float4(0,0,-1,0);
                            return o;
                        }
                        o.light=face?_DownLightColor:_UpLightColor;
                        v.vertex.z=v.color.a%1==0?t.x:t.y;
                        v.vertex.x=v.color.a>0.5?t.z:t.w;
                    }
                }
                else
                {
                    face =_WorldSpaceCameraPos.y<unity_ObjectToWorld._m13+v.vertex.y;
                    o.light=face?_DownLightColor:_UpLightColor;
                }
                o.inter.xz = v.vertex.xz + unity_ObjectToWorld._m03_m23;
                o.inter.y = v.vertex.y;
                if(o.light.a>0)
                o.shadow= mul(_ShadowMatrix, float4(o.inter, 1)).xyz/2+0.5;
                else
                o.shadow.xy= mul(_ShadowMatrix, float4(o.inter, 1)).xy/2+0.5;
                o.shadow.z-=0.0004;
                float3 camOffset = (_WorldSpaceCameraPos - o.inter)/333;
                o.fog = dot(camOffset.xz, camOffset.xz);

                float3 objectPos = round(unity_ObjectToWorld._m03_m13_m23);
                float3 relativePos = v.vertex.xyz + objectPos - _WorldSpaceCameraPos;
                float3 viewPos = mul((float3x3)UNITY_MATRIX_V, relativePos);
                o.vertex = mul(UNITY_MATRIX_P, float4(viewPos, 1.0));
                
                o.inter.xz = v.vertex.xz + (int2(unity_ObjectToWorld._m03_m23)&511);
                uint axis = v.color.x ? 0u : v.color.y ? 1u : 2u;
                o.faceData = (axis | (face ? 4u : 0u)) * 0.125;

                return o;
            }

            uint block(int3 pos)
            {
                if (pos.y < 0 || pos.y >= 128) return 0;
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

            fixed4 SampleSurface(uint id, int3 voxel, float2 uv, uint atlasFace, float mipLevel)
            {
                uint2 tile = uint2(id & 15u, id >> 4);
                if (id > 9u)
                {
                    fixed4 flip = _AtlasFlipMap.Load(int4(tile, atlasFace, 0));
                    int bits = Hash3(voxel.x, voxel.y, voxel.z, round(flip.w * 255));
                    if (flip.x && (bits & 1)) uv = uv.yx;
                    if (flip.y && (bits & 2)) uv.x = 1 - uv.x;
                    if (flip.z && (bits & 4)) uv.y = 1 - uv.y;
                }
                return UNITY_SAMPLE_TEX2DARRAY_LOD(_Atlas, float3((uv + tile) / 16.0, atlasFace), mipLevel);
            }

            float4 frag(v2f i) : SV_Target
            {
                uint data = (uint)round(i.faceData * 8.0);
                uint axis = data & 3u;
                bool face = (data & 4u) != 0u;
                int3 offset = axis == 0u ? int3(1,0,0) : axis == 1u ? int3(0,1,0) : int3(0,0,1);
                float3 p = i.inter;
                float2 faceUV = offset.x ? p.zy : offset.y ? p.xz : p.xy;
                float2 faceDx = ddx(faceUV);
                float2 faceDy = ddy(faceUV);
                int3 pos = (int3)floor(p);
                if (axis == 0u) pos.x = (int)round(p.x);
                else if (axis == 1u) pos.y = (int)round(p.y);
                else pos.z = (int)round(p.z);
                int3 outsideVoxel = face ? pos - offset : pos;
                int3 insideVoxel = face ? pos : pos - offset;
                uint outsideBlock = block(outsideVoxel);
                if (outsideBlock > 8u) discard;
                float2 uv = frac(faceUV);
                uint atlasFace = axis == 0u ? (face ? 0u : 1u) : axis == 1u ? (face ? 2u : 3u) : (face ? 4u : 5u);
                float footprint = max(length(faceDx), length(faceDy)) * 32.0;
                float mipLevel = clamp(log2(max(footprint, 1.0)), 0.0, 5.0);
                uint insideBlock = 0u;
                fixed4 c = 0;
                if (outsideBlock == 1u)
                {
                    insideBlock = block(insideVoxel);
                    if (insideBlock == 1u) discard;
                }
                if (outsideBlock != 0u)
                    c = SampleSurface(outsideBlock, outsideVoxel, uv, atlasFace ^ 1u, mipLevel);
                if (c.a < 0.25)
                {
                    if (outsideBlock != 1u) insideBlock = block(insideVoxel);
                    if (insideBlock == 0u) discard;
                    c = SampleSurface(insideBlock, insideVoxel, uv, atlasFace, mipLevel);
                    clip(c.a - 0.25);
                }
                int3 os1 = (uv.y>0.5?1:-1)*(offset.x?int3(0,1,0):offset.y?int3(0,0,1):int3(0,1,0));
                int3 os2 = (uv.x>0.5?1:-1)*(offset.x?int3(0,0,1):offset.y?int3(1,0,0):int3(1,0,0));
                if(face)
                pos-=offset;
                bool d1=block(pos + os1)>13;
                bool d2=block(pos + os2)>13;
                float ao = 0;
                fixed2 duv=abs(uv*2-1);
                duv*=duv;
                if(d1)
                {
                    if(d2)
                        ao = duv.y+duv.x;
                    else
                    {
                        d2 = block(pos + os1 + os2)>13;
                        if(d2)
                            ao = duv.y;
                        else
                            ao = duv.y*(1-duv.x);
                    }
                }
                else if(d2)
                {
                    d1 = block(pos + os1 + os2)>13;
                    if(d1)
                        ao = duv.x;
                    else
                        ao = (1-duv.y)*duv.x;
                }

                if (abs(c.a - 0.5) < 0.01) c.rgb = saturate(_BiomeLeafColor.rgb * c.g + float3(c.r, 0, c.b));
                c *= i.light;
                float shadow;
                if(i.light.a==0) shadow = 0.2;
                else
                {
                    shadow = i.shadow.x<0||i.shadow.x>1||i.shadow.y<0||i.shadow.y>1||i.shadow.z<0.05||i.shadow.z>0.95;
                    if(!shadow) shadow = 1-clamp(i.shadow.z-tex2D(_ShadowMap, i.shadow.xy).r,0,0.002)*400;
                    else
                    {
                        i.shadow-=_ShadowLodOffset.xyz;
                        i.shadow.xy=(i.shadow.xy-0.5)/10+0.5;
                        shadow = i.shadow.x<0||i.shadow.x>1||i.shadow.y<0||i.shadow.y>1||i.shadow.z<0.05||i.shadow.z>0.95;
                        if(!shadow) shadow = 1-clamp(i.shadow.z-tex2D(_ShadowMap1, i.shadow.xy).r,0,0.002)*400;
                    }
                }
                shadow = lerp(0.2,shadow,i.light.a);
                shadow *= 1-pow(ao,0.5)*0.5;

                float3 litColor = c.rgb * (shadow * _WorldBrightness);
                return float4(lerp(litColor, _FogColor.rgb, i.fog), 1.0);
            }

            ENDCG
        }
    }
}
