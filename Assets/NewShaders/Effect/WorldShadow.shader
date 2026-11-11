Shader "VRC_MINE/WorldShadow"
{
    Properties
    {
        _VoxelWorld ("Voxel World", 3D) = "black" {}
        _LodTex1 ("LOD Texture 1", 3D) = "black" {}
        _LodTex2 ("LOD Texture 2", 3D) = "black" {}
        _LodTex3 ("LOD Texture 3", 3D) = "black" {}
        _Atlas ("Block Atlas", 2DArray) = "black" {}
        [Toggle] _Special ("Special", Int) = 0
    }
    SubShader
    {
        Tags { "RenderType"="VoxelWorld" "Queue"="Geometry" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend One One
        BlendOp Min
        Pass
        {
            CGPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            Texture3D<uint> _VoxelWorld;
            Texture3D<uint> _LodTex1;
            Texture3D<uint> _LodTex2;
            Texture3D<uint> _LodTex3;
            UNITY_DECLARE_TEX2DARRAY(_Atlas);
            int _Special;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                noperspective float4 vertex : SV_POSITION;
                nointerpolation float depth : TEXCOORD0;
                nointerpolation float3 ray : TEXCOORD1;
                noperspective float3 position : TEXCOORD2;
            };

            float ShadowLinearDepth(float3 worldPosition)
            {
                return -mul(UNITY_MATRIX_V, float4(worldPosition, 1.0)).z / _ProjectionParams.z;
            }

            v2f vert(appdata v)
            {
                v2f o;
                // Alpha 0 marks vertices that must not participate in the
                // shadow pass. Put them behind the clip volume.
                if (_Special == 0)
                {
                    // Ordinary geometry path.
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.depth = ShadowLinearDepth(mul(unity_ObjectToWorld, v.vertex).xyz);
                    o.ray = 0.0;
                    o.position = 0.0;
                    return o;
                }
                if (v.color.a)
                {
                    o.vertex = float4(0, 0, -1, 1);
                    o.depth = 1.0;
                    o.ray = 0.0;
                    o.position = 0.0;
                    return o;
                }
                if (unity_OrthoParams.y > 30.0 && v.color.r)
                {
                    o.vertex = float4(2, 2, 0, 1);
                    o.depth = 1.0;
                    o.ray = 0.0;
                    o.position = 0.0;
                    return o;
                }
                // Special path: pass the received vertex position directly.
                o.vertex = v.vertex;
                o.depth = 0.0;
                // Exact vertex setup used by the working DDA renderer.
                float2 cameraUV = v.uv;
                cameraUV *= unity_OrthoParams.xy;
                o.position = _WorldSpaceCameraPos
                    + UNITY_MATRIX_I_V._m01_m11_m21 * cameraUV.y
                    + UNITY_MATRIX_I_V._m00_m10_m20 * cameraUV.x;
                o.ray = -UNITY_MATRIX_I_V._m02_m12_m22;
                o.depth = ShadowLinearDepth(o.position);
                return o;
            }

            int3 WrapVoxel(int3 voxel)
            {
                voxel.xy &= 1023;
                return voxel;
            }

            float3 CellExitDistances(float3 p, float3 direction, float3 inverseDirection, float cellSize)
            {
                float3 cell = floor(p / cellSize);
                float3 boundary = (cell + (direction > 0.0)) * cellSize;
                float3 distance = (boundary - p) * inverseDirection;
                return abs(direction) < 0.0000001 ? 1e30 : distance;
            }

            int HitFace(int axis, float3 ray)
            {
                if (axis == 0) return ray.x > 0.0 ? 0 : 1;
                if (axis == 2) return ray.z > 0.0 ? 2 : 3;
                return ray.y < 0.0 ? 4 : 5;
            }

            float2 FaceUV(float3 p, int axis)
            {
                if (axis == 0) return frac(float2(-p.y, p.z));
                if (axis == 2) return frac(float2(p.x, -p.y));
                return frac(p.xz);
            }

            float frag(v2f i) : SV_Target
            {
                if (_Special == 0) return saturate(i.depth);

                float3 worldOrigin = i.position;
                float3 worldDirection = normalize(i.ray);
                // Voxel World stores world X, inverted world Z, then world Y.
                float3 origin = float3(frac(worldOrigin.x / 1024.0) * 1024.0,
                                      frac(-worldOrigin.z / 1024.0) * 1024.0,
                                      worldOrigin.y);
                float3 direction = float3(worldDirection.x, -worldDirection.z, worldDirection.y);
                float3 inverseDirection = 1.0 / direction;
                float t0 = (0.0 - origin.z) * inverseDirection.z;
                float t1 = (256.0 - origin.z) * inverseDirection.z;
                if (t0 > t1) { float swap = t0; t0 = t1; t1 = swap; }
                float distanceAlongRay = max(t0, 0.0);
                float maxDistance = min(t1, 655.36);
                if (abs(direction.z) < 0.0000001)
                {
                    if (origin.z < 0.0 || origin.z >= 256.0) discard;
                    distanceAlongRay = 0.0;
                    maxDistance = 490.0;
                }
                if (distanceAlongRay >= maxDistance) discard;

                // Mandatory first step: resolve the voxel containing the ray
                // origin before entering the regular LOD traversal. This is
                // required when the camera starts inside a voxel.
                if (origin.z >= 0.0 && origin.z < 256.0)
                {
                    float3 eyePoint = origin + direction * 0.0001;
                    int3 eyeVoxel = WrapVoxel((int3)floor(eyePoint));
                    float3 eyeExits = CellExitDistances(
                        eyePoint, direction, inverseDirection, 1.0);
                    float eyeExit;
                    int eyeAxis;
                    if (eyeExits.x <= eyeExits.y && eyeExits.x <= eyeExits.z)
                    {
                        eyeExit = eyeExits.x;
                        eyeAxis = 0;
                    }
                    else if (eyeExits.y <= eyeExits.z)
                    {
                        eyeExit = eyeExits.y;
                        eyeAxis = 1;
                    }
                    else
                    {
                        eyeExit = eyeExits.z;
                        eyeAxis = 2;
                    }

                    float eyeDistance = 0.0001 + max(eyeExit, 0.0001);
                    uint eyeValue = _VoxelWorld.Load(int4(eyeVoxel, 0));
                    if (eyeValue != 0u)
                    {
                        bool eyeOpaque = eyeValue > 8u;
                        if (!eyeOpaque)
                        {
                            float3 eyeHit = eyePoint + direction * eyeDistance;
                            int eyeFace = HitFace(eyeAxis, direction);
                            float2 eyeUV = FaceUV(eyeHit, eyeAxis);
                            uint2 eyeTile = uint2(eyeValue & 15u, eyeValue >> 4u);
                            float2 eyeAtlasUV = (float2(eyeTile) + eyeUV) / 16.0;
                            eyeOpaque = UNITY_SAMPLE_TEX2DARRAY_LOD(
                                _Atlas, float3(eyeAtlasUV, eyeFace), 0).a >= 0.25;
                        }
                        if (eyeOpaque)
                        {
                            float3 eyeWorldHit = worldOrigin + worldDirection * eyeDistance;
                            return saturate(ShadowLinearDepth(eyeWorldHit));
                        }
                    }
                    distanceAlongRay = eyeDistance;
                }

                int3 cachedLodCoord = 0;
                uint cachedLod1 = 0u;
                uint cachedLod2 = 0u;
                uint cachedLod3 = 0u;
                bool lodCacheValid = false;
                int entryAxis = 2;

                [loop]
                for (uint iteration = 0u; iteration < 1140u; ++iteration)
                {
                    if (distanceAlongRay >= maxDistance) break;
                    float3 p = origin + direction * (distanceAlongRay + 0.001);
                    // The first sample can round back onto z=256 at slab entry.
                    // Clamp only that entry sample into the valid voxel volume.
                    if (iteration == 0u) p.z = clamp(p.z, 0.001, 255.999);
                    int3 voxel = WrapVoxel((int3)floor(p));
                    if (voxel.z < 0 || voxel.z >= 256) break;

                    int3 lodCoord = voxel >> 2;
                    bool changedLod1 = !lodCacheValid || any(lodCoord != cachedLodCoord);
                    bool changedLod2 = !lodCacheValid || any((lodCoord >> 2) != (cachedLodCoord >> 2));
                    bool changedLod3 = !lodCacheValid || any((lodCoord >> 4) != (cachedLodCoord >> 4));
                    float cellSize;

                    if (changedLod3) cachedLod3 = _LodTex3.Load(int4(lodCoord >> 4, 0));
                    if (cachedLod3 == 0u) cellSize = 64.0;
                    else
                    {
                        if (changedLod2) cachedLod2 = _LodTex2.Load(int4(lodCoord >> 2, 0));
                        if (cachedLod2 == 0u) cellSize = 16.0;
                        else
                        {
                            if (changedLod1) cachedLod1 = _LodTex1.Load(int4(lodCoord, 0));
                            if (cachedLod1 == 0u) cellSize = 4.0;
                            else
                            {
                                uint value = _VoxelWorld.Load(int4(voxel, 0));
                                if (value != 0u)
                                {
                                    // IDs 1..8 are Glass and the seven leaf blocks.
                                    // They are never randomly flipped. Every later ID
                                    // is opaque and therefore needs no atlas read here.
                                    bool castsShadow = value > 8u;
                                    if (!castsShadow)
                                    {
                                        float3 hitP = origin + direction * distanceAlongRay;
                                        int face = HitFace(entryAxis, direction);
                                        float2 blockUV = FaceUV(hitP, entryAxis);
                                        uint2 tile = uint2(value & 15u, value >> 4u);
                                        float2 atlasUV = (float2(tile) + blockUV) / 16.0;
                                        castsShadow = UNITY_SAMPLE_TEX2DARRAY_LOD(
                                            _Atlas, float3(atlasUV, face), 0).a >= 0.25;
                                    }
                                    if (castsShadow)
                                    {
                                        float3 hitWorld = worldOrigin + worldDirection * distanceAlongRay;
                                        return saturate(ShadowLinearDepth(hitWorld));
                                    }
                                }
                                cellSize = 1.0;
                            }
                        }
                    }

                    cachedLodCoord = lodCoord;
                    lodCacheValid = true;
                    float3 exits = CellExitDistances(p, direction, inverseDirection, cellSize);
                    float exitDistance;
                    if (exits.x <= exits.y && exits.x <= exits.z)
                    {
                        exitDistance = exits.x;
                        entryAxis = 0;
                    }
                    else if (exits.y <= exits.z)
                    {
                        exitDistance = exits.y;
                        entryAxis = 1;
                    }
                    else
                    {
                        exitDistance = exits.z;
                        entryAxis = 2;
                    }
                    distanceAlongRay += 0.0001 + max(exitDistance, 0.0001);
                }

                discard;
                return 1.0;
            }
            ENDCG
        }
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend One One
        BlendOp Min
        Pass
        {
            CGPROGRAM
            #pragma vertex ordinaryVert
            #pragma fragment ordinaryFrag
            #include "UnityCG.cginc"

            struct ordinaryAppdata { float4 vertex : POSITION; };
            struct ordinaryV2F
            {
                float4 vertex : SV_POSITION;
                nointerpolation float depth : TEXCOORD0;
            };

            float OrdinaryShadowLinearDepth(float3 worldPosition)
            {
                return -mul(UNITY_MATRIX_V, float4(worldPosition, 1.0)).z / _ProjectionParams.z;
            }

            ordinaryV2F ordinaryVert(ordinaryAppdata v)
            {
                ordinaryV2F o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.depth = OrdinaryShadowLinearDepth(mul(unity_ObjectToWorld, v.vertex).xyz);
                return o;
            }

            float ordinaryFrag(ordinaryV2F i) : SV_Target
            {
                return saturate(i.depth);
            }
            ENDCG
        }
    }
}
