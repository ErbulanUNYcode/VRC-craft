Shader "VRC_MINE/Lod1Gen"
{
    Properties
    {
        _VoxelWorld ("Voxel World", 3D) = "black" {}
        [IntRange] _OffsetX ("Offset X", Range(0, 63)) = 0
        [IntRange] _OffsetY ("Offset Y", Range(0, 63)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityCustomRenderTexture.cginc"

            Texture3D<float> _VoxelWorld;
            int _OffsetX;
            int _OffsetY;

            struct V2F { float4 vertex:SV_POSITION; float3 localTexcoord:TEXCOORD0; float3 globalTexcoord:TEXCOORD1; nointerpolation uint primitiveID:TEXCOORD2; };
            V2F vert(appdata_customrendertexture v)
            {
                v2f_customrendertexture b = CustomRenderTextureVertexShader(v);
                V2F o; o.vertex=b.vertex; o.localTexcoord=b.localTexcoord; o.globalTexcoord=b.globalTexcoord; o.primitiveID=b.primitiveID;
                if (o.primitiveID == 0)
                {
                    float2 textureSize=float2(_CustomRenderTextureWidth,_CustomRenderTextureHeight);
                    o.vertex.xy += float2(_OffsetX,_OffsetY)*4.0*(2.0/textureSize);
                    o.globalTexcoord.xy=o.vertex.xy*0.5+0.5;
                    #if UNITY_UV_STARTS_AT_TOP
                    o.globalTexcoord.y=1.0-o.globalTexcoord.y;
                    #endif
                }
                return o;
            }

            bool Block(int3 p)
            {
                // LOD texture axes already match VoxelWorld: X=X, Y=world Z, Z=world Y.
                return _VoxelWorld.Load(int4(p, 0)) > 0.0;
            }

            fixed4 frag(V2F i):SV_Target
            {
                fixed3 debugRgb=saturate(i.globalTexcoord);
                if(i.primitiveID!=0)return fixed4(debugRgb,0.0);
                int3 base=(int3)floor(saturate(i.globalTexcoord)*float3(256.0,256.0,64.0));
                bool occupied=false;
                int3 sourceBase=base*4;
                [unroll]
                for(int z=0; z<4; z++)
                {
                    [unroll]
                    for(int y=0; y<4; y++)
                    {
                        [unroll]
                        for(int x=0; x<4; x++)
                        {
                            if(Block(sourceBase+int3(x,y,z)))
                                occupied=true;
                        }
                    }
                }
                return fixed4(debugRgb,occupied?1.0:0.0);
            }
            ENDHLSL
        }
    }
}
