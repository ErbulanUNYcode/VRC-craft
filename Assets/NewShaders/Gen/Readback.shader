Shader "VRC_MINE/Gen/Readback"
{
    Properties
    {
        _VoxelWorld ("Voxel World", 3D) = "black" {}
        _ChunkPosX ("Chunk Position X", Int) = 0
        _ChunkPosY ("Chunk Position Z", Int) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCustomRenderTexture.cginc"

            struct ReadbackV2F
            {
                float4 vertex : SV_POSITION;
                float3 localTexcoord : TEXCOORD0;
            };

            Texture3D<uint> _VoxelWorld;
            int _ChunkPosX;
            int _ChunkPosY;

            ReadbackV2F vert(appdata_customrendertexture v)
            {
                v2f_customrendertexture base = CustomRenderTextureVertexShader(v);
                ReadbackV2F o;
                o.vertex = base.vertex;
                o.localTexcoord = base.localTexcoord;
                return o;
            }

            float frag(ReadbackV2F i) : SV_Target
            {
                uint2 outputPixel = (uint2)floor(saturate(i.localTexcoord.xy) * float2(256.0, 130.0));
                uint localX = outputPixel.x & 15u;
                uint localZ = outputPixel.x >> 4u;
                int worldX = (_ChunkPosX * 16 + (int)localX) & 511;
                int textureY = 511 - ((_ChunkPosY * 16 + (int)localZ) & 511);
                return _VoxelWorld.Load(int4(worldX, textureY, (int)outputPixel.y, 0)) / 255.0;
            }
            ENDHLSL
        }
    }
}
