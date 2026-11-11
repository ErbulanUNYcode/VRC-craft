Shader "VRC_MINE/DDA"
{
    Properties
    {
        _VoxelWorld ("Voxel World", 3D) = "black" {}
        _LodTex1 ("LOD Texture 1", 3D) = "black" {}
        _LodTex2 ("LOD Texture 2", 3D) = "black" {}
        _LodTex3 ("LOD Texture 3", 3D) = "black" {}
        _Atlas ("Block Atlas", 2DArray) = "black" {}
        _AtlasFlipMap ("Atlas Flip Map", 2DArray) = "black" {}
        _BiomeLeafColor ("Biome Leaf Color", Color) = (0.45, 1.0, 0.45, 1.0)
        _FogColor ("Fog Color", Color) = (0.199, 0.436, 0.670, 1)
        _FogDistance ("Fog Distance", Range(300, 600)) = 333.0
        _UpLightColor ("Up Light Color", Color) = (1, 1, 1, 0.786)
        _DownLightColor ("Down Light Color", Color) = (0.680, 0.679, 0.679, 0)
        _RightLightColor ("Right Light Color", Color) = (0.849, 0.841, 0.877, 0.6)
        _LeftLightColor ("Left Light Color", Color) = (0.846, 0.843, 0.875, 0)
        _FrontLightColor ("Front Light Color", Color) = (1, 1, 1, 0.4)
        _BackLightColor ("Back Light Color", Color) = (0.833, 0.825, 0.805, 0)
        _ShadowMap ("Shadow Map", 2D) = "white" {}
        _ShadowMap1 ("Shadow Map LOD", 2D) = "white" {}
        [Toggle] _AndroidDepth ("Android Depth Conversion", Range(0, 1)) = 0
        [Toggle] _Special ("Special", Int) = 0
        [Toggle(DEBUG_READS)] _DebugReads ("Debug Texture Reads", Float) = 0
    } 
    SubShader
    {
        Tags { "RenderType"="VoxelWorld" "Queue"="Geometry" }
        Cull Off
        ZWrite On
        ZTest LEqual
        Blend Off
        Pass
        {
            CGPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local DEBUG_READS
            #include "UnityCG.cginc"

            Texture3D<uint> _VoxelWorld;
            Texture3D<uint> _LodTex1;
            Texture3D<uint> _LodTex2;
            Texture3D<uint> _LodTex3;
            UNITY_DECLARE_TEX2DARRAY(_Atlas);
            UNITY_DECLARE_TEX2DARRAY(_AtlasFlipMap);
            fixed4 _BiomeLeafColor;
            fixed4 _FogColor;
            float _FogDistance;
            fixed4 _UpLightColor;
            fixed4 _DownLightColor;
            fixed4 _RightLightColor;
            fixed4 _LeftLightColor;
            fixed4 _FrontLightColor;
            fixed4 _BackLightColor;
            sampler2D _ShadowMap;
            sampler2D _ShadowMap1;
            float4x4 _ShadowMatrix;
            float4 _ShadowLodOffset;
            bool _AndroidDepth;

            struct appdata
            {
                float4 vertex:POSITION;
                float4 color:COLOR;
            };
            struct v2f
            {
                float4 vertex:SV_POSITION;
                float3 ray:TEXCOORD0;
                nointerpolation float3 pos:TEXCOORD1;
            };
            struct FragmentOutput { fixed4 color:SV_Target; float depth:SV_Depth; };

#ifdef DEBUG_READS
            fixed4 DebugReadColor(uint reads)
            {
                float value = saturate((float)reads / 600.0);
                return fixed4(saturate(value * 2.0 - 1.0),
                              saturate(1.0 - abs(value * 2.0 - 1.0)),
                              saturate(1.0 - value * 2.0), 1.0);
            }
#endif

            v2f vert(appdata v)
            {
                v2f o;
                // Alpha 1 marks vertices that must not participate in the
                // visible DDA pass. Put them behind the clip volume.
                if(v.color.a==0)
                {
                    o.vertex=float4(2,2,0,1);
                    o.ray=0;
                    o.pos=0;
                    return o;
                }
                float4x4 view=UNITY_MATRIX_V;
                view._m03=0; view._m13=0; view._m23=0;
                o.vertex=mul(UNITY_MATRIX_P,mul(view,v.vertex));
                o.ray=v.vertex.xyz;
                o.pos=_WorldSpaceCameraPos;
                return o;
            }

            float WrapWorld(float v) { return frac(v/1024.0)*1024.0; }
            int3 WrapVoxel(int3 v) { v.xy&=1023; return v; }

            float3 CellExitDistances(float3 p,float3 dir,float3 invDir,float size)
            {
                float3 cell=floor(p/size);
                float3 boundary=(cell+(dir>0))*size;
                float3 d=(boundary-p)*invDir;
                return abs(dir)<0.0000001?1e30:d;
            }

            uint Hash3(int3 p,uint value)
            {
                uint h=(uint)p.x*73856093u^(uint)p.y*19349663u^(uint)p.z*83492791u^value*2654435761u;
                h^=h>>13u; h*=1274126177u; return h^(h>>16u);
            }

            int HitFace(int axis,float3 ray)
            {
                if(axis==0) return ray.x>0?0:1;
                if(axis==2) return ray.z>0?2:3;
                return ray.y<0?4:5;
            }

            float2 FaceUV(float3 p,int axis)
            {
                if(axis==0) return frac(float2(-p.y,p.z));
                if(axis==2) return frac(float2(p.x,-p.y));
                return frac(p.xz);
            }

            float2 FaceDerivative(float3 d,int axis)
            {
                if(axis==0) return float2(-d.y,d.z);
                if(axis==2) return float2(d.x,-d.y);
                return d.xz;
            }

            fixed4 FaceLight(int face)
            {
                if(face==0)return _LeftLightColor;
                if(face==1)return _RightLightColor;
                if(face==2)return _DownLightColor;
                if(face==3)return _UpLightColor;
                if(face==4)return _BackLightColor;
                return _FrontLightColor;
            }

            uint BlockAt(int3 voxel)
            {
                if(voxel.z<0||voxel.z>=256)return 0u;
                return _VoxelWorld.Load(int4(WrapVoxel(voxel),0));
            }

            int3 FaceVoxelNormal(int face)
            {
                if(face==0)return int3(-1,0,0);
                if(face==1)return int3(1,0,0);
                if(face==2)return int3(0,0,-1);
                if(face==3)return int3(0,0,1);
                if(face==4)return int3(0,1,0);
                return int3(0,-1,0);
            }

            float AmbientOcclusion(int3 voxel,float3 hitP,int axis,int face)
            {
                float2 uv;
                int3 tangent1;
                int3 tangent2;
                if(axis==0)
                {
                    uv=frac(hitP.yz);
                    tangent1=int3(0,0,uv.y>.5?1:-1);
                    tangent2=int3(0,uv.x>.5?1:-1,0);
                }
                else if(axis==1)
                {
                    uv=frac(hitP.xz);
                    tangent1=int3(0,0,uv.y>.5?1:-1);
                    tangent2=int3(uv.x>.5?1:-1,0,0);
                }
                else
                {
                    uv=frac(hitP.xy);
                    tangent1=int3(0,uv.y>.5?1:-1,0);
                    tangent2=int3(uv.x>.5?1:-1,0,0);
                }

                // AO neighbours live in the layer outside the rendered face,
                // not directly beside the current voxel in its own layer.
                int3 normal=FaceVoxelNormal(face);
                bool d1=BlockAt(voxel+normal+tangent1)>13u;
                bool d2=BlockAt(voxel+normal+tangent2)>13u;
                float2 duv=abs(uv*2.0-1.0);
                duv*=duv;
                float ao=0.0;
                if(d1)
                {
                    if(d2)ao=duv.y+duv.x;
                    else if(BlockAt(voxel+normal+tangent1+tangent2)>13u)ao=duv.y;
                    else ao=duv.y*(1.0-duv.x);
                }
                else if(d2)
                {
                    if(BlockAt(voxel+normal+tangent1+tangent2)>13u)ao=duv.x;
                    else ao=(1.0-duv.y)*duv.x;
                }
                return ao;
            }

            float ShadowFactor(float3 hitWorld,fixed shadowPower)
            {
                if(shadowPower<=0.0)return .2;
                float3 shadowCoord=mul(_ShadowMatrix,float4(hitWorld,1.0)).xyz*.5+.5;
                shadowCoord.z-=.0004;
                bool outside=shadowCoord.x<0||shadowCoord.x>1||shadowCoord.y<0||shadowCoord.y>1||shadowCoord.z<.05||shadowCoord.z>.95;
                float shadow=1.0;
                if(!outside)
                {
                    float shadowDepth=tex2Dlod(_ShadowMap,float4(shadowCoord.xy,0,0)).r;
                    shadow=1.0-clamp(shadowCoord.z-shadowDepth,0.0,.002)*400.0;
                }
                else
                {
                    shadowCoord-=_ShadowLodOffset.xyz;
                    shadowCoord.xy=(shadowCoord.xy-.5)/10.0+.5;
                    outside=shadowCoord.x<0||shadowCoord.x>1||shadowCoord.y<0||shadowCoord.y>1||shadowCoord.z<.05||shadowCoord.z>.95;
                    if(!outside)
                    {
                        float shadowDepth=tex2Dlod(_ShadowMap1,float4(shadowCoord.xy,0,0)).r;
                        shadow=1.0-clamp(shadowCoord.z-shadowDepth,0.0,.002)*400.0;
                    }
                }
                return lerp(.2,shadow,shadowPower);
            }

            fixed4 SampleBlock(uint value,int3 voxel,float3 hitP,int axis,float3 faceRay,float mip)
            {
                int face=HitFace(axis,faceRay);
                float2 blockUV=FaceUV(hitP,axis);
                uint2 tile=uint2(value&15u,value>>4u);
                // Air, Glass, all leaves and Debug (IDs 0..9) are fixed.
                if(value>9u)
                {
                    fixed4 flip=UNITY_SAMPLE_TEX2DARRAY_LOD(_AtlasFlipMap,float3((float2(tile)+.5)/16.0,face),0);
                    uint randomBits=Hash3(voxel,value);
                    if(flip.r>.5&&(randomBits&1u)!=0u)blockUV=blockUV.yx;
                    if(flip.g>.5&&(randomBits&2u)!=0u)blockUV.x=1-blockUV.x;
                    if(flip.b>.5&&(randomBits&4u)!=0u)blockUV.y=1-blockUV.y;
                }
                return UNITY_SAMPLE_TEX2DARRAY_LOD(_Atlas,float3((float2(tile)+blockUV)/16.0,face),mip);
            }

            FragmentOutput FinishHit(fixed4 color,int face,int3 voxel,float3 hitP,int axis,float sampleDistance,v2f i,float3 ray)
            {
                FragmentOutput o;
                if(abs(color.a-.5)<.01)
                {
                    float3 foliage=_BiomeLeafColor.rgb*color.g;
                    foliage.r+=color.r;
                    foliage.b+=color.b;
                    color.rgb=saturate(foliage);
                }
                fixed4 light=FaceLight(face);
                color.rgb*=light.rgb;
                float ao=AmbientOcclusion(voxel,hitP,axis,face);
                float3 hitWorld=i.pos+ray*sampleDistance;
                float shadow=ShadowFactor(hitWorld,light.a);
                shadow*=1.0-sqrt(ao)*.5;
                float2 fogOffset=(i.pos.xz-hitWorld.xz)/max(_FogDistance, 0.0001);
                float fog=saturate(dot(fogOffset,fogOffset));
                color.rgb=lerp(color.rgb*shadow,_FogColor.rgb,fog);
                color.a=1;
                float4 clipHit=mul(UNITY_MATRIX_VP,float4(hitWorld,1.0));
                o.color=color;
                o.depth=clipHit.z/clipHit.w;
                if(_AndroidDepth)
                    o.depth=(o.depth-UNITY_NEAR_CLIP_VALUE)/(1.0-UNITY_NEAR_CLIP_VALUE);
                return o;
            }

            FragmentOutput frag(v2f i)
            {
                FragmentOutput o;
                o.color = 0;
                o.depth = 1.0;
#ifdef DEBUG_READS
                uint debugReads = 0u;
#endif
                float3 ray=normalize(i.ray);
                float3 textureRay=float3(ray.x,-ray.z,ray.y);
                float3 rayDx=ddx(textureRay),rayDy=ddy(textureRay);
                float3 invRay=1.0/textureRay;
                float3 origin=float3(WrapWorld(i.pos.x),WrapWorld(-i.pos.z),i.pos.y);

                float t0=(0-origin.z)*invRay.z;
                float t1=(256-origin.z)*invRay.z;
                if(t0>t1){float swap=t0;t0=t1;t1=swap;}
                float t=max(t0,0.0),maxT=min(t1,490.0);
                if(abs(textureRay.z)<0.0000001)
                {
                    if(origin.z<0||origin.z>=256)
                    {
#ifdef DEBUG_READS
                        o.color=DebugReadColor(debugReads); o.depth=1.0; return o;
#else
                        clip(-1);
#endif
                    }
                    t=0; maxT=490;
                }
                if(t>=maxT)
                {
#ifdef DEBUG_READS
                    o.color=DebugReadColor(debugReads); o.depth=1.0; return o;
#else
                    clip(-1);
#endif
                }

                int entryAxis=2;

                // The eye voxel exists only at the start of the ray. Treat it as
                // one empty unit cell here so the main loop never tests for it.
                if(origin.z>=0&&origin.z<256)
                {
                    float3 eyeP=origin+textureRay*.0001;
                    int3 eyeVoxel=WrapVoxel((int3)floor(eyeP));
                    float3 eyeExits=CellExitDistances(eyeP,textureRay,invRay,1.0);
                    float eyeExit;
                    if(eyeExits.x<=eyeExits.y&&eyeExits.x<=eyeExits.z)
                    {
                        eyeExit=eyeExits.x;
                        entryAxis=0;
                    }
                    else if(eyeExits.y<=eyeExits.z)
                    {
                        eyeExit=eyeExits.y;
                        entryAxis=1;
                    }
                    else
                    {
                        eyeExit=eyeExits.z;
                        entryAxis=2;
                    }
                    float eyeDistance=.0001+max(eyeExit,.0001);
                    uint eyeValue=_VoxelWorld.Load(int4(eyeVoxel,0));
#ifdef DEBUG_READS
                    debugReads++;
#endif
                    if(eyeValue>0u&&eyeValue<=8u)
                    {
                        float3 eyeHitP=origin+textureRay*eyeDistance;
                        float2 eyeUvDx=FaceDerivative(rayDx,entryAxis)*eyeDistance;
                        float2 eyeUvDy=FaceDerivative(rayDy,entryAxis)*eyeDistance;
                        float eyeFootprint=max(length(eyeUvDx),length(eyeUvDy))*32.0;
                        float eyeMip=clamp(log2(max(eyeFootprint,1.0)),0.0,5.0);
                        fixed4 eyeColor=SampleBlock(eyeValue,eyeVoxel,eyeHitP,entryAxis,-textureRay,eyeMip);
                        if(eyeColor.a>=.25)
#ifdef DEBUG_READS
                            o.color=DebugReadColor(debugReads); o.depth=1.0; return o;
#else
                            return FinishHit(eyeColor,HitFace(entryAxis,textureRay),eyeVoxel,eyeHitP,entryAxis,eyeDistance,i,ray);
#endif
                    }
                    t=eyeDistance;
                }

                int3 cachedLodCoord=0;
                uint cachedLod1=0u;
                uint cachedLod2=0u;
                uint cachedLod3=0u;
                bool lodCacheValid=false;

                [loop]
                for(uint iteration=0u;iteration<852u;++iteration)
                {
                    if(t>=maxT)break;
                    float sampleDistance=t;
                    float3 hitP=origin+textureRay*sampleDistance;
                    float3 p=origin+textureRay*(sampleDistance+.0001);
                    int3 voxel=(int3)floor(hitP);
                    if(sampleDistance>.0001)
                    {
                        if(entryAxis==0)voxel.x=(int)round(hitP.x)-(textureRay.x<0?1:0);
                        else if(entryAxis==1)voxel.y=(int)round(hitP.y)-(textureRay.y<0?1:0);
                        else voxel.z=(int)round(hitP.z)-(textureRay.z<0?1:0);
                    }
                    voxel=WrapVoxel(voxel);
                    int hitFace=HitFace(entryAxis,textureRay);
                    uint value=0u;
                    bool testExitFace=false;
                    float cellSize;

                    int3 lodCoord=voxel>>2;
                        bool changedLod1=!lodCacheValid||any(lodCoord!=cachedLodCoord);
                        bool changedLod2=!lodCacheValid||any((lodCoord>>2)!=(cachedLodCoord>>2));
                        bool changedLod3=!lodCacheValid||any((lodCoord>>4)!=(cachedLodCoord>>4));

                        if(changedLod3)
                                    cachedLod3=_LodTex3.Load(int4(lodCoord>>4,0));
#ifdef DEBUG_READS
                                    debugReads++;
#endif

                        if(cachedLod3==0u)cellSize=64;
                        else
                        {
                            if(changedLod2)
                                cachedLod2=_LodTex2.Load(int4(lodCoord>>2,0));
#ifdef DEBUG_READS
                                debugReads++;
#endif

                            if(cachedLod2==0u)cellSize=16;
                            else
                            {
                                if(changedLod1)
                                    cachedLod1=_LodTex1.Load(int4(lodCoord,0));
#ifdef DEBUG_READS
                                    debugReads++;
#endif

                                if(cachedLod1==0u)cellSize=4;
                                else
                                {
                                    value=_VoxelWorld.Load(int4(voxel,0));
#ifdef DEBUG_READS
                                    debugReads++;
#endif
                                    if(value!=0u)
                                    {
                                        float2 uvDx=FaceDerivative(rayDx,entryAxis)*sampleDistance;
                                        float2 uvDy=FaceDerivative(rayDy,entryAxis)*sampleDistance;
                                        float footprint=max(length(uvDx),length(uvDy))*32.0;
                                        float mip=clamp(log2(max(footprint,1.0)),0.0,5.0);
                                        fixed4 color=SampleBlock(value,voxel,hitP,entryAxis,textureRay,mip);

                                        if(color.a>=.25)
#ifdef DEBUG_READS
                                            o.color=DebugReadColor(debugReads); o.depth=1.0; return o;
#else
                                            return FinishHit(color,hitFace,voxel,hitP,entryAxis,sampleDistance,i,ray);
#endif
                                        testExitFace=value<=8u;
                                    }
                                    cellSize=1;
                                }
                            }
                        }

                    cachedLodCoord=lodCoord;
                    lodCacheValid=true;

                    float3 exits=CellExitDistances(p,textureRay,invRay,cellSize);
                    float exitDistance;
                    if(exits.x<=exits.y&&exits.x<=exits.z){exitDistance=exits.x;entryAxis=0;}
                    else if(exits.y<=exits.z){exitDistance=exits.y;entryAxis=1;}
                    else{exitDistance=exits.z;entryAxis=2;}
                    if(testExitFace)
                    {
                        float backDistance=sampleDistance+.0001+max(exitDistance,.0001);
                        float3 backHitP=origin+textureRay*backDistance;
                        float2 backUvDx=FaceDerivative(rayDx,entryAxis)*backDistance;
                        float2 backUvDy=FaceDerivative(rayDy,entryAxis)*backDistance;
                        float backFootprint=max(length(backUvDx),length(backUvDy))*32.0;
                        float backMip=clamp(log2(max(backFootprint,1.0)),0.0,5.0);
                        fixed4 backColor=SampleBlock(value,voxel,backHitP,entryAxis,-textureRay,backMip);
                        if(backColor.a>=.25)
#ifdef DEBUG_READS
                            o.color=DebugReadColor(debugReads); o.depth=1.0; return o;
#else
                                            return FinishHit(backColor,hitFace,voxel,backHitP,entryAxis,backDistance,i,ray);
#endif
                    }
                    t=sampleDistance+.0001+max(exitDistance,.0001);
                }
#ifdef DEBUG_READS
                o.color=DebugReadColor(debugReads); o.depth=1.0; return o;
#else
                clip(-1);
                o.color=0; o.depth=1;return o;
#endif
            }
            ENDCG
        }
    }
}
