Shader "VRC_MINE/Gen/LodGen"
{
    Properties
    {
        _VoxelWorld ("Voxel World", 3D) = "black" {}
        [IntRange] _OffsetX ("Chunk X", Range(0, 63)) = 0
        [IntRange] _OffsetY ("Chunk Z", Range(0, 63)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCustomRenderTexture.cginc"

            int _OffsetX;
            int _OffsetY;
            Texture3D<uint> _VoxelWorld;
            static const int2 ZoneOffsets[5] = {
                int2(0, 0), int2(0, -1), int2(1, 0), int2(0, 1), int2(-1, 0)
            };

            uint ReadTextureVoxel(int3 texturePosition)
            {
                if (texturePosition.z < 0 || texturePosition.z >= 256)
                    return 0u;
                texturePosition.xy &= 1023;
                return _VoxelWorld.Load(int4(texturePosition, 0));
            }

            bool ShouldDrawFaceBidirectional(uint firstID, uint secondID)
            {
                if (firstID == 0u && secondID == 0u) return false; // air
                if (firstID > 8u && secondID > 8u) return false; // block
                if ((firstID >= 2u && firstID <= 8u) || (secondID >= 2u && secondID <= 8u)) return true; // leaf
                if (firstID == 1u && secondID == 1u) return false; // glass
                return true;
            }

            bool ShouldDrawFaceOneWay(uint firstID, uint secondID)
            {
                if (firstID == 0u) return secondID != 0u; // air
                if (firstID > 8u) return false; // block
                if (firstID >= 2u && firstID <= 8u) return true; // leaf
                if (firstID == 1u) return secondID != 1u; // glass
                return firstID == 0u;
            }

            struct V2F
            {
                float4 vertex : SV_POSITION;
                float3 globalTexcoord : TEXCOORD0;
                float3 localTexcoord : TEXCOORD1;
                nointerpolation uint primitiveID : TEXCOORD2;
            };

            V2F vert(appdata_customrendertexture v)
            {
                v2f_customrendertexture baseVertex = CustomRenderTextureVertexShader(v);
                V2F o;
                o.vertex = baseVertex.vertex;
                o.globalTexcoord = baseVertex.globalTexcoord;
                o.localTexcoord = baseVertex.localTexcoord;
                o.primitiveID = baseVertex.primitiveID;
                return o;
            }

            uint frag(V2F input) : SV_Target
            {
                uint2 pixel = (uint2)floor(saturate(input.localTexcoord.xy) * 16.0);
                pixel&=15u;
                uint zoneId = min(input.primitiveID, 4u);
                int2 zoneOffset = ZoneOffsets[zoneId];
                int sectionY = min((int)floor(saturate(input.globalTexcoord.z) * 16.0), 15);
                int3 textureSectionPosition = int3(((_OffsetX+zoneOffset.x)&63)<<4, 1023 - (((_OffsetY+zoneOffset.y)&63)<<4), sectionY<<4);
                uint3 minSideOffset = 16u;
                uint3 maxSideOffset = 0u;
                uint3 oldBlockID;

                uint i = pixel.x;
                uint j = pixel.y;

                oldBlockID.x = ReadTextureVoxel(textureSectionPosition + int3(-1, -(int)j, i));
                oldBlockID.y = ReadTextureVoxel(textureSectionPosition + int3(i, -(int)j, -1));
                oldBlockID.z = ReadTextureVoxel(textureSectionPosition + int3(i, 1, j));

                uint blockID = ReadTextureVoxel(textureSectionPosition + int3(0, -(int)j, i));
                if (ShouldDrawFaceOneWay(oldBlockID.x, blockID))
                {
                    minSideOffset.x = 0u;
                    minSideOffset.y = min(minSideOffset.y, (uint)i);
                    minSideOffset.z = min(minSideOffset.z, (uint)j);
                    maxSideOffset.y = max(maxSideOffset.y, (uint)(i + 1));
                    maxSideOffset.z = max(maxSideOffset.z, (uint)(j + 1));
                }
                oldBlockID.x = blockID;

                blockID = ReadTextureVoxel(textureSectionPosition + int3(i, -(int)j, 0));
                if (ShouldDrawFaceOneWay(oldBlockID.y, blockID))
                {
                    minSideOffset.y = 0u;
                    minSideOffset.x = min(minSideOffset.x, (uint)i);
                    minSideOffset.z = min(minSideOffset.z, (uint)j);
                    maxSideOffset.x = max(maxSideOffset.x, (uint)(i + 1));
                    maxSideOffset.z = max(maxSideOffset.z, (uint)(j + 1));
                }
                oldBlockID.y = blockID;

                blockID = ReadTextureVoxel(textureSectionPosition + int3(i, 0, j));
                if (ShouldDrawFaceOneWay(oldBlockID.z, blockID))
                {
                    minSideOffset.z = 0u;
                    minSideOffset.x = min(minSideOffset.x, (uint)i);
                    minSideOffset.y = min(minSideOffset.y, (uint)j);
                    maxSideOffset.x = max(maxSideOffset.x, (uint)(i + 1));
                    maxSideOffset.y = max(maxSideOffset.y, (uint)(j + 1));
                }
                oldBlockID.z = blockID;

                [loop] for (int k = 1; k < 16; k++)
                {
                    blockID = ReadTextureVoxel(textureSectionPosition + int3(k, -(int)j, i));
                    if (ShouldDrawFaceBidirectional(oldBlockID.x, blockID))
                    {
                        minSideOffset.x = min(minSideOffset.x, (uint)k);
                        maxSideOffset.x = max(maxSideOffset.x, (uint)k);
                        minSideOffset.y = min(minSideOffset.y, (uint)i);
                        minSideOffset.z = min(minSideOffset.z, (uint)j);
                        maxSideOffset.y = max(maxSideOffset.y, (uint)(i + 1));
                        maxSideOffset.z = max(maxSideOffset.z, (uint)(j + 1));
                    }
                    oldBlockID.x = blockID;

                    blockID = ReadTextureVoxel(textureSectionPosition + int3(i, -(int)j, k));
                    if (ShouldDrawFaceBidirectional(oldBlockID.y, blockID))
                    {
                        minSideOffset.y = min(minSideOffset.y, (uint)k);
                        maxSideOffset.y = max(maxSideOffset.y, (uint)k);
                        minSideOffset.x = min(minSideOffset.x, (uint)i);
                        minSideOffset.z = min(minSideOffset.z, (uint)j);
                        maxSideOffset.x = max(maxSideOffset.x, (uint)(i + 1));
                        maxSideOffset.z = max(maxSideOffset.z, (uint)(j + 1));
                    }
                    oldBlockID.y = blockID;

                    blockID = ReadTextureVoxel(textureSectionPosition + int3(i, -k, j));
                    if (ShouldDrawFaceBidirectional(oldBlockID.z, blockID))
                    {
                        minSideOffset.z = min(minSideOffset.z, (uint)k);
                        maxSideOffset.z = max(maxSideOffset.z, (uint)k);
                        minSideOffset.x = min(minSideOffset.x, (uint)i);
                        minSideOffset.y = min(minSideOffset.y, (uint)j);
                        maxSideOffset.x = max(maxSideOffset.x, (uint)(i + 1));
                        maxSideOffset.y = max(maxSideOffset.y, (uint)(j + 1));
                    }
                    oldBlockID.z = blockID;
                }

                blockID = ReadTextureVoxel(textureSectionPosition + int3(16, -(int)j, i));
                if (ShouldDrawFaceOneWay(blockID, oldBlockID.x))
                {
                    maxSideOffset.x = 16u;
                    minSideOffset.y = min(minSideOffset.y, (uint)i);
                    minSideOffset.z = min(minSideOffset.z, (uint)j);
                    maxSideOffset.y = max(maxSideOffset.y, (uint)(i + 1));
                    maxSideOffset.z = max(maxSideOffset.z, (uint)(j + 1));
                }

                blockID = ReadTextureVoxel(textureSectionPosition + int3(i, -(int)j, 16));
                if (ShouldDrawFaceOneWay(blockID, oldBlockID.y))
                {
                    maxSideOffset.y = 16u;
                    minSideOffset.x = min(minSideOffset.x, (uint)i);
                    minSideOffset.z = min(minSideOffset.z, (uint)j);
                    maxSideOffset.x = max(maxSideOffset.x, (uint)(i + 1));
                    maxSideOffset.z = max(maxSideOffset.z, (uint)(j + 1));
                }

                blockID = ReadTextureVoxel(textureSectionPosition + int3(i, -16, j));
                if (ShouldDrawFaceOneWay(blockID, oldBlockID.z))
                {
                    maxSideOffset.z = 16u;
                    minSideOffset.x = min(minSideOffset.x, (uint)i);
                    minSideOffset.y = min(minSideOffset.y, (uint)j);
                    maxSideOffset.x = max(maxSideOffset.x, (uint)(i + 1));
                    maxSideOffset.y = max(maxSideOffset.y, (uint)(j + 1));
                }


                bool hasSurface = minSideOffset.x <= maxSideOffset.x &&
                                  minSideOffset.y <= maxSideOffset.y &&
                                  minSideOffset.z <= maxSideOffset.z;
                if (!hasSurface)
                    return 0u;

                return 1u | minSideOffset.x << 1 | maxSideOffset.x << 6 |
                       minSideOffset.y << 11 | maxSideOffset.y << 16 |
                       minSideOffset.z << 21 | maxSideOffset.z << 26;
            }
            ENDHLSL
        }
    }
}
