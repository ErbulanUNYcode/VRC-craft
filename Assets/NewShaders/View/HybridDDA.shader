Shader "VRC_MINE/HybridDDA"
{
    Properties
    {
        _LodTex ("LOD Sections", 3D) = "black" {}
        _VoxelWorld ("Voxel World", 3D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderType" = "VoxelWorld" "Queue" = "Geometry" }
        Cull Off
        ZWrite On
        Blend Off
        ZTest LEqual
        ZClip Off

        Pass
        {
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            Texture3D<uint4> _LodTex;
            Texture3D<uint> _VoxelWorld;
            #define LOD_MINIMUM(d) int3((d).r & 15u | ((d).a >> 1 & 1) << 4, \
                                        (d).b & 15u | ((d).a >> 5 & 1) << 4, \
                                        (d).g & 15u | ((d).a >> 4 & 1) << 4)
            #define LOD_MAXIMUM(d) int3((d).r >> 4 | ((d).a >> 2 & 1) << 4, \
                                        (d).b >> 4 | ((d).a >> 6 & 1) << 4, \
                                        (d).g >> 4 | ((d).a >> 3 & 1) << 4)
            uint ReadWorldVoxel(int3 worldPosition)
            {
                if (worldPosition.y < 0 || worldPosition.y >= 256) return 0u;
                return _VoxelWorld.Load(int4(worldPosition.x & 1023, 1023 - (worldPosition.z & 1023), worldPosition.y, 0));
            }

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPosition : TEXCOORD0;
                nointerpolation fixed faceData : TEXCOORD1;
                float surfaceDepth : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o = (v2f)0;

                int3 vertexPosition = (int3)(v.vertex.xyz + unity_ObjectToWorld._m03_m13_m23);

                int3 sectionIndex = vertexPosition >> 4;

                uint4 lodData = _LodTex.Load(int4(sectionIndex.x&63, 63 - (sectionIndex.z&63), sectionIndex.y, 0));
                if (lodData.a == 0u)
                {
                    o.vertex = float4(2.0, 2.0, 0.0, 1.0);
                    return o;
                }
                int3 cameraPosition = (int3)floor(_WorldSpaceCameraPos);
                int3 cameraPosition1024delta = cameraPosition;
                cameraPosition1024delta.xz&=1023;
                cameraPosition1024delta.xz-=cameraPosition.xz;
                cameraPosition.xz&=1023;
                vertexPosition.xz+=cameraPosition1024delta.xz;
                sectionIndex = vertexPosition >> 4;
                sectionIndex<<=4;
                int3 minimum = LOD_MINIMUM(lodData) + sectionIndex;
                int3 maximum = LOD_MAXIMUM(lodData) + sectionIndex;
                float3 worldPosition;
                uint face = 0u;
                if ((vertexPosition.x & 1) == 1)
                {
                    face = 1;
                    if (cameraPosition.x < minimum.x && minimum.x < sectionIndex.x + 16)
                    {
                        worldPosition.x = minimum.x;
                    }
                    else if (cameraPosition.x >= maximum.x && maximum.x > sectionIndex.x)
                    {
                        face |= 8u;
                        worldPosition.x = maximum.x;
                    }
                    else
                    {
                        o.vertex = float4(2.0, 2.0, 0.0, 1.0);
                        return o;
                    }
                    worldPosition.y = (vertexPosition.y & 2) == 0 ? minimum.y : maximum.y;
                    worldPosition.z = (vertexPosition.z & 2) == 0 ? minimum.z : maximum.z;
                }
                else if ((vertexPosition.y & 1) == 1)
                {
                    face = 2;
                    if (cameraPosition.y < minimum.y && minimum.y < sectionIndex.y + 16)
                    {
                        worldPosition.y = minimum.y;
                    }
                    else if (cameraPosition.y >= maximum.y && maximum.y > sectionIndex.y)
                    {
                        face |= 8u;
                        worldPosition.y = maximum.y;
                    }
                    else
                    {
                        o.vertex = float4(2.0, 2.0, 0.0, 1.0);
                        return o;
                    }
                    worldPosition.x = (vertexPosition.x & 2) == 0 ? minimum.x : maximum.x;
                    worldPosition.z = (vertexPosition.z & 2) == 0 ? minimum.z : maximum.z;
                }
                else
                {
                    face = 3;
                    if (cameraPosition.z < minimum.z && minimum.z < sectionIndex.z + 16)
                    {
                        worldPosition.z = minimum.z;
                    }
                    else if (cameraPosition.z >= maximum.z && maximum.z > sectionIndex.z)
                    {
                        face |= 8u;
                        worldPosition.z = maximum.z;
                    }
                    else if (cameraPosition.z >= minimum.z && cameraPosition.z < maximum.z && cameraPosition.x >= minimum.x && cameraPosition.x < maximum.x && cameraPosition.y >= minimum.y && cameraPosition.y < maximum.y)
                    {
                        float3 cameraWorldPosition = _WorldSpaceCameraPos;
                        cameraWorldPosition.xz += cameraPosition1024delta.xz;
                        face = 0;
                        #if defined(USING_STEREO_MATRICES)
                        o.faceData = (unity_StereoEyeIndex << 2) * 0.0625;
                        #else
                        o.faceData = 0;
                        #endif
                        float3 corner = float3(((vertexPosition.x & 2) - 1) * 4, ((vertexPosition.y & 2) - 1) * 4, -1);
                        corner = mul(corner, (float3x3)UNITY_MATRIX_V);
                        worldPosition = cameraWorldPosition + corner;
                        o.worldPosition = worldPosition;
                        float3 viewPosition = mul((float3x3)UNITY_MATRIX_V, corner);
                        o.vertex = mul(UNITY_MATRIX_P, float4(viewPosition, 1.0));
                        o.surfaceDepth = -viewPosition.z - _ProjectionParams.y;
                        o.vertex.z = 0.5 * o.vertex.w;
                        return o;
                    }
                    else
                    {
                        o.vertex = float4(2.0, 2.0, 0.0, 1.0);
                        return o;
                    }
                    worldPosition.x = (vertexPosition.x & 2) == 0 ? minimum.x : maximum.x;
                    worldPosition.y = (vertexPosition.y & 2) == 0 ? minimum.y : maximum.y;
                }
                {
                    float3 cameraWorldPosition = _WorldSpaceCameraPos;
                    #if defined(USING_STEREO_MATRICES)
                    o.faceData = (face | unity_StereoEyeIndex << 2) * 0.0625;
                    #else
                    o.faceData = face * 0.0625;
                    #endif
                    o.worldPosition = worldPosition;
                    cameraWorldPosition.xz += cameraPosition1024delta.xz;
                    float3 relativePosition = worldPosition - cameraWorldPosition;
                    float3 viewPosition = mul((float3x3)UNITY_MATRIX_V, relativePosition);
                    o.vertex = mul(UNITY_MATRIX_P, float4(viewPosition, 1.0));
                    o.surfaceDepth = -viewPosition.z - _ProjectionParams.y;
                    o.vertex.z = 0.5 * o.vertex.w;
                    return o;
                }
            }

            struct FragmentOutput
            {
                float4 color : SV_Target;
                float depth : SV_Depth;
            };

            FragmentOutput FinishHit(float3 color, float surfaceDepth, float t, uint eye)
            {
                FragmentOutput output;
                #if defined(USING_STEREO_MATRICES)
                float projectionA = -unity_StereoMatrixP[eye]._m22;
                float projectionB = unity_StereoMatrixP[eye]._m23;
                #else
                float projectionA = -UNITY_MATRIX_P._m22;
                float projectionB = UNITY_MATRIX_P._m23;
                #endif
                float hitViewDepth = (surfaceDepth + _ProjectionParams.y) * t;
                if (hitViewDepth <= 0.0)
                {
                    discard;
                    return (FragmentOutput)0;
                }
                output.color = float4(color, 1);
                output.depth = projectionA + projectionB / hitViewDepth;
                #if defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
                output.depth = output.depth * 0.5 + 0.5;
                #endif
                output.depth = saturate(output.depth);
                return output;
            }

            FragmentOutput frag(v2f input)
            {
                uint data = (uint)round(input.faceData * 16.0);
                float3 ray;
                float3 stepRay;
                float3 distance;
                int3 areaBoundary;
                int3 sectionOutsideBlock;
                uint3 voxel;
                uint block;
                float startT = 1.0;
                if (data & 1u)
                {
                    data >>= 1;
                    if (data & 1u)
                    { // Z
                        data >>= 1;
                        voxel = (uint3)(int3)float3(floor(input.worldPosition.xy), round(input.worldPosition.z));
                        uint block1 = ReadWorldVoxel((int3)voxel + int3(0, 0, data & 2u ? 0 : -1));
                        if (block1 > 8u) discard;
                        if (data & 2u) voxel.z--;
                        block = ReadWorldVoxel((int3)voxel);
                        if (block > 0u) return FinishHit(float3(0.1, 0.1, 0.4), input.surfaceDepth, 1.0, data & 1u);
                        #if defined(USING_STEREO_MATRICES)
                        float4x4 projection = unity_StereoMatrixP[data & 1u];
                        float3x3 viewRotation = (float3x3)unity_StereoMatrixV[data & 1u];
                        #else
                        float4x4 projection = UNITY_MATRIX_P;
                        float3x3 viewRotation = (float3x3)UNITY_MATRIX_V;
                        #endif
                        float2 screenUV = input.vertex.xy / _ScreenParams.xy;
                        #if defined(UNITY_SINGLE_PASS_STEREO)
                        float4 eyeViewport = unity_StereoScaleOffset[data & 1u];
                        screenUV = (screenUV - eyeViewport.zw) / eyeViewport.xy;
                        #endif
                        float2 ndc = screenUV * 2.0 - 1.0;
                        #if UNITY_UV_STARTS_AT_TOP
                        ndc.y = -ndc.y;
                        #endif
                        float3 viewRay = float3((ndc.x + projection._m02) / projection._m00,
                                               (ndc.y + projection._m12) / projection._m11, -1.0);
                        ray = mul(viewRay, viewRotation) * (input.surfaceDepth + _ProjectionParams.y);
                        stepRay = 1.0 / max(abs(ray), 1e-20);
                        float3 surfaceFraction = frac(input.worldPosition);
                        distance = -(ray > 0 ? surfaceFraction : 1.0 - surfaceFraction) * stepRay;
                        distance.z = 0;
                        int3 sectionPosition = (int3)voxel >> 4;
                        uint4 lodData = _LodTex.Load(int4(sectionPosition.x & 63, 63 - (sectionPosition.z & 63), sectionPosition.y, 0));
                        sectionPosition <<= 4;
                        int3 minimum = LOD_MINIMUM(lodData) + sectionPosition;
                        int3 maximum = LOD_MAXIMUM(lodData) + sectionPosition;
                        areaBoundary = ray > 0 ? maximum : minimum;
                        sectionOutsideBlock = ray > 0 ? sectionPosition + 16 : sectionPosition - 1;
                    }
                    else
                    { // X
                        data >>= 1;
                        voxel = (uint3)(int3)float3(round(input.worldPosition.x), floor(input.worldPosition.yz));
                        uint block1 = ReadWorldVoxel((int3)voxel + int3(data & 2u ? 0 : -1, 0, 0));
                        if (block1 > 8u) discard;
                        if (data & 2u) voxel.x--;
                        block = ReadWorldVoxel((int3)voxel);
                        if (block > 0u) return FinishHit(float3(0.4, 0.1, 0.1), input.surfaceDepth, 1.0, data & 1u);
                        #if defined(USING_STEREO_MATRICES)
                        float4x4 projection = unity_StereoMatrixP[data & 1u];
                        float3x3 viewRotation = (float3x3)unity_StereoMatrixV[data & 1u];
                        #else
                        float4x4 projection = UNITY_MATRIX_P;
                        float3x3 viewRotation = (float3x3)UNITY_MATRIX_V;
                        #endif
                        float2 screenUV = input.vertex.xy / _ScreenParams.xy;
                        #if defined(UNITY_SINGLE_PASS_STEREO)
                        float4 eyeViewport = unity_StereoScaleOffset[data & 1u];
                        screenUV = (screenUV - eyeViewport.zw) / eyeViewport.xy;
                        #endif
                        float2 ndc = screenUV * 2.0 - 1.0;
                        #if UNITY_UV_STARTS_AT_TOP
                        ndc.y = -ndc.y;
                        #endif
                        float3 viewRay = float3((ndc.x + projection._m02) / projection._m00,
                                               (ndc.y + projection._m12) / projection._m11, -1.0);
                        ray = mul(viewRay, viewRotation) * (input.surfaceDepth + _ProjectionParams.y);
                        stepRay = 1.0 / max(abs(ray), 1e-20);
                        float3 surfaceFraction = frac(input.worldPosition);
                        distance = -(ray > 0 ? surfaceFraction : 1.0 - surfaceFraction) * stepRay;
                        distance.x = 0;
                        int3 sectionPosition = (int3)voxel >> 4;
                        uint4 lodData = _LodTex.Load(int4(sectionPosition.x & 63, 63 - (sectionPosition.z & 63), sectionPosition.y, 0));
                        sectionPosition <<= 4;
                        int3 minimum = LOD_MINIMUM(lodData) + sectionPosition;
                        int3 maximum = LOD_MAXIMUM(lodData) + sectionPosition;
                        areaBoundary = ray > 0 ? maximum : minimum;
                        sectionOutsideBlock = ray > 0 ? sectionPosition + 16 : sectionPosition - 1;
                    }
                }
                else
                {
                    data >>= 1;
                    if (data & 1u)
                    { // Y
                        data >>= 1;
                        voxel = (uint3)(int3)float3(floor(input.worldPosition.x), round(input.worldPosition.y), floor(input.worldPosition.z));
                        uint block1 = ReadWorldVoxel((int3)voxel + int3(0, data & 2u ? 0 : -1, 0));
                        if (block1 > 8u) discard;
                        if (data & 2u) voxel.y--;
                        block = ReadWorldVoxel((int3)voxel);
                        if (block > 0u) return FinishHit(float3(0.1, 0.4, 0.1), input.surfaceDepth, 1.0, data & 1u);
                        #if defined(USING_STEREO_MATRICES)
                        float4x4 projection = unity_StereoMatrixP[data & 1u];
                        float3x3 viewRotation = (float3x3)unity_StereoMatrixV[data & 1u];
                        #else
                        float4x4 projection = UNITY_MATRIX_P;
                        float3x3 viewRotation = (float3x3)UNITY_MATRIX_V;
                        #endif
                        float2 screenUV = input.vertex.xy / _ScreenParams.xy;
                        #if defined(UNITY_SINGLE_PASS_STEREO)
                        float4 eyeViewport = unity_StereoScaleOffset[data & 1u];
                        screenUV = (screenUV - eyeViewport.zw) / eyeViewport.xy;
                        #endif
                        float2 ndc = screenUV * 2.0 - 1.0;
                        #if UNITY_UV_STARTS_AT_TOP
                        ndc.y = -ndc.y;
                        #endif
                        float3 viewRay = float3((ndc.x + projection._m02) / projection._m00,
                                               (ndc.y + projection._m12) / projection._m11, -1.0);
                        ray = mul(viewRay, viewRotation) * (input.surfaceDepth + _ProjectionParams.y);
                        stepRay = 1.0 / max(abs(ray), 1e-20);
                        float3 surfaceFraction = frac(input.worldPosition);
                        distance = -(ray > 0 ? surfaceFraction : 1.0 - surfaceFraction) * stepRay;
                        distance.y = 0;
                        int3 sectionPosition = (int3)voxel >> 4;
                        uint4 lodData = _LodTex.Load(int4(sectionPosition.x & 63, 63 - (sectionPosition.z & 63), sectionPosition.y, 0));
                        sectionPosition <<= 4;
                        int3 minimum = LOD_MINIMUM(lodData) + sectionPosition;
                        int3 maximum = LOD_MAXIMUM(lodData) + sectionPosition;
                        areaBoundary = ray > 0 ? maximum : minimum;
                        sectionOutsideBlock = ray > 0 ? sectionPosition + 16 : sectionPosition - 1;
                    }
                    else
                    { // Fullscreen
                        data >>= 1;
                        startT = 0.0;
                        #if defined(USING_STEREO_MATRICES)
                        float3 cameraWorldPosition = unity_StereoWorldSpaceCameraPos[data];
                        #else
                        float3 cameraWorldPosition = _WorldSpaceCameraPos;
                        #endif
                        int2 cameraIntegerPosition = (int2)floor(cameraWorldPosition.xz);
                        cameraWorldPosition.xz += (cameraIntegerPosition & 1023) - cameraIntegerPosition;
                        voxel = (uint3)(int3)floor(cameraWorldPosition);
                        block = ReadWorldVoxel((int3)voxel);
                        ray = input.worldPosition - cameraWorldPosition;
                        stepRay = 1.0 / max(abs(ray), 1e-20);
                        float3 cameraFraction = frac(cameraWorldPosition);
                        distance = -(ray > 0 ? cameraFraction : 1.0 - cameraFraction) * stepRay;
                        int3 sectionPosition = (int3)floor(cameraWorldPosition) >> 4;
                        uint4 lodData = _LodTex.Load(int4(sectionPosition.x & 63, 63 - (sectionPosition.z & 63), sectionPosition.y, 0));
                        sectionPosition <<= 4;
                        int3 minimum = LOD_MINIMUM(lodData) + sectionPosition;
                        int3 maximum = LOD_MAXIMUM(lodData) + sectionPosition;
                        areaBoundary = ray > 0 ? maximum : minimum;
                        sectionOutsideBlock = ray > 0 ? sectionPosition + 16 : sectionPosition - 1;
                    }
                }
                int3 step = ray > 0 ? 1 : -1;
                [loop] for (int iteration = 0; iteration < 49; iteration++)
                {
                    float3 next = distance + stepRay;
                    uint axis;
                    if (next.x < next.y && next.x < next.z)
                    {
                        distance.x = next.x;
                        voxel.x += step.x;
                        axis = 0u;
                    }
                    else if (next.y < next.z)
                    {
                        distance.y = next.y;
                        voxel.y += step.y;
                        axis = 1u;
                    }
                    else
                    {
                        distance.z = next.z;
                        voxel.z += step.z;
                        axis = 2u;
                    }

                    if ((int)voxel[axis] == sectionOutsideBlock[axis]) break;
                    uint nextBlock = ReadWorldVoxel((int3)voxel);
                    if (nextBlock > 0u)
                    {
                        float3 color = axis == 0u ? float3(0.1, 0.3, 0.3) : axis == 1u ? float3(0.3, 0.1, 0.3) : float3(0.3, 0.3, 0.1);
                        return FinishHit(color, input.surfaceDepth, startT + distance[axis], data & 1u);
                    }
                    if (step[axis] > 0 ? (int)voxel[axis] >= areaBoundary[axis] : (int)voxel[axis] < areaBoundary[axis]) break;
                    block = nextBlock;
                }
                discard;
                return (FragmentOutput)0;
            }
            ENDHLSL
        }
    }
}
