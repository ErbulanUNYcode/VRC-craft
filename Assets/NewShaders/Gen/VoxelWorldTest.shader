Shader "VRC_MINE/Gen/VoxelWorld"
{
    Properties
    {
        [IntRange] _OffsetX ("Offset X", Range(0, 63)) = 1
        [IntRange] _OffsetY ("Offset Y", Range(0, 63)) = 1
        [Toggle] _ShowRed ("Red", Float) = 1
        [Toggle] _ShowGreen ("Green", Float) = 0
        [Toggle] _ShowBlue ("Blue", Float) = 0
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

            struct VoxelWorldV2F
            {
                float4 vertex : SV_POSITION;
                float3 localTexcoord : TEXCOORD0;
                float3 globalTexcoord : TEXCOORD1;
                nointerpolation uint primitiveID : TEXCOORD2;
            };

            int _OffsetX;
            int _OffsetY;
            float _ShowRed;
            float _ShowGreen;
            float _ShowBlue;

            VoxelWorldV2F vert(appdata_customrendertexture v)
            {
                v2f_customrendertexture base = CustomRenderTextureVertexShader(v);
                VoxelWorldV2F o;
                o.vertex = base.vertex;
                o.localTexcoord = base.localTexcoord;
                o.globalTexcoord = base.globalTexcoord;
                o.primitiveID = base.primitiveID;

                // The horizontal chunk plane is represented by texture X/Y.
                if (o.primitiveID == 0)
                {
                    float2 textureSize = float2(
                        _CustomRenderTextureWidth,
                        _CustomRenderTextureHeight
                    );
                    float2 pixelOffset = float2(_OffsetX, _OffsetY) * 16.0;
                    o.vertex.xy += pixelOffset * (2.0 / textureSize);

                    // Rebuild UV from the final vertex position, as in the working
                    // world-generator shader. This keeps UV synchronized with the
                    // actually moved update zone.
                    o.globalTexcoord.xy = o.vertex.xy * 0.5 + 0.5;
                    #if UNITY_UV_STARTS_AT_TOP
                        o.globalTexcoord.y = 1.0 - o.globalTexcoord.y;
                    #endif
                }

                return o;
            }

            fixed4 frag(VoxelWorldV2F i) : SV_Target
            {
                if (i.primitiveID != 0)
					return fixed4(0.0, 0.0, 0.0, 1.0);

				float3 uv = saturate(i.globalTexcoord.xyz);
				uv.y = 1.0 - uv.y;

				float3 channelMask = float3(_ShowRed, _ShowGreen, _ShowBlue);
				float3 remappedUV = float3(uv.x, uv.z, uv.y);
				float3 color = remappedUV * channelMask;
				return fixed4(color, 1.0);
            }

            ENDHLSL
        }
    }
}
