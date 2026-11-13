Shader "VRC_MINE/HybridDDA"
{
    Properties
    {
        _LodTex ("LOD Sections", 3D) = "black" {}
        _VoxelWorld ("Voxel World", 3D) = "black" {}
        _WireThickness ("Wire Thickness", Range(0.0005, 0.1)) = 0.015
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            Texture3D<uint4> _LodTex;
            Texture3D<uint> _VoxelWorld;
            float _WireThickness;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 side : TEXCOORD1;
                float3 edgeCoord : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                int3 objectPosition = (int3)round(unity_ObjectToWorld._m03_m13_m23);
                float3 worldVertex = v.vertex.xyz + objectPosition;
                int3 vertexPosition = (int3)round(worldVertex);
                float3 sectionOrigin = floor(worldVertex / 16.0) * 16.0;
                float3 marker = fmod(worldVertex - sectionOrigin, 16.0);
                o.side = float3(abs(marker.x - 1.0) < 0.1,
                                abs(marker.y - 1.0) < 0.1,
                                abs(marker.z - 1.0) < 0.1);
                o.edgeCoord = marker * 0.5;
                int3 sectionIndex = int3((vertexPosition.x >> 4) & 63,
                                         (vertexPosition.y >> 4) & 15,
                                         (vertexPosition.z >> 4) & 63);

                int3 lodCoord = int3(sectionIndex.x,
                                     63 - sectionIndex.z,
                                     sectionIndex.y);
                uint4 lodData = _LodTex.Load(int4(lodCoord, 0));
                if ((lodData.a & 1u) == 0u)
                {
                    o.vertex = float4(2.0, 2.0, 0.0, 1.0);
                    return o;
                }

                uint minX = (lodData.r & 15u) | (((lodData.a >> 1) & 1u) << 4);
                uint maxX = (lodData.r >> 4) | (((lodData.a >> 2) & 1u) << 4);
                uint minZ = (lodData.g & 15u) | (((lodData.a >> 4) & 1u) << 4);
                uint maxZ = (lodData.g >> 4) | (((lodData.a >> 3) & 1u) << 4);
                uint minY = (lodData.b & 15u) | (((lodData.a >> 5) & 1u) << 4);
                uint maxY = (lodData.b >> 4) | (((lodData.a >> 6) & 1u) << 4);

                float3 cameraLocal = _WorldSpaceCameraPos - sectionOrigin;
                bool discardPlane =
                    (o.side.x > 0.5 && cameraLocal.x >= minX && cameraLocal.x <= maxX) ||
                    (o.side.y > 0.5 && cameraLocal.y >= minY && cameraLocal.y <= maxY) ||
                    (o.side.z > 0.5 && cameraLocal.z >= minZ && cameraLocal.z <= maxZ);
                if (discardPlane)
                {
                    o.vertex = float4(2.0, 2.0, 0.0, 1.0);
                    return o;
                }

                float3 stretched = float3(
                    marker.x == 0.0 ? minX : marker.x == 2.0 ? maxX : (cameraLocal.x > maxX ? maxX : minX),
                    marker.y == 0.0 ? minY : marker.y == 2.0 ? maxY : (cameraLocal.y > maxY ? maxY : minY),
                    marker.z == 0.0 ? minZ : marker.z == 2.0 ? maxZ : (cameraLocal.z > maxZ ? maxZ : minZ)
                );
                float3 stretchedWorld = sectionOrigin + stretched;
                o.vertex = mul(UNITY_MATRIX_VP, float4(stretchedWorld, 1.0));
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return float4(1.0, 1.0, 1.0, 0.02);
                float2 edgeUV = i.side.x > 0.5 ? i.edgeCoord.yz
                              : i.side.y > 0.5 ? i.edgeCoord.xz
                              : i.edgeCoord.xy;
                float edgeDistance = min(min(edgeUV.x, 1.0 - edgeUV.x),
                                         min(edgeUV.y, 1.0 - edgeUV.y));
                float wire = 1.0 - smoothstep(0.0, _WireThickness, edgeDistance);
                float3 baseColor = i.side;
                return float4(lerp(baseColor, float3(1.0, 1.0, 1.0), wire), 1.0);
            }
            ENDHLSL
        }
    }
}
