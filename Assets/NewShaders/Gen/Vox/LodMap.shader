Shader "VRC_MINE/Gen/LodMap"
{
    Properties
    {
        _LodGen ("LOD Generator", 3D) = "black" {}
        [IntRange] _OffsetX ("Chunk X", Range(0, 63)) = 0
        [IntRange] _OffsetY ("Chunk Z", Range(0, 63)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Cull Off
        ZWrite Off
        ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCustomRenderTexture.cginc"

            int _OffsetX;
            int _OffsetY;
            Texture3D<uint> _LodGen;
            static const int2 ZoneOffsets[5] = {
                int2(0, 0), int2(0, -1), int2(1, 0), int2(0, 1), int2(-1, 0)
            };
            static const int2 SourceOffsets[5] = {
                int2(0, 16), int2(16, 16), int2(32, 16), int2(0, 0), int2(16, 0)
            };

            struct V2F
            {
                float4 vertex : SV_POSITION;
                nointerpolation uint zoneId : TEXCOORD0;
                nointerpolation uint layer : TEXCOORD1;
            };

            V2F vert(appdata_customrendertexture v)
            {
                v2f_customrendertexture b = CustomRenderTextureVertexShader(v);
                V2F o;
                uint zoneId = b.primitiveID;
                o.zoneId = min(zoneId, 4u);
                o.layer = min((uint)(saturate(b.globalTexcoord.z) * 16.0), 15u);
                int2 chunk = (int2(_OffsetX, _OffsetY) + ZoneOffsets[o.zoneId]) & 63;
                float2 pixel = float2(chunk.x, 63 - chunk.y);
                float2 uv = (pixel + b.localTexcoord.xy) / 64.0;
                o.vertex = float4(uv * 2.0 - 1.0, b.vertex.z, 1.0);
                #if UNITY_UV_STARTS_AT_TOP
                o.vertex.y = -o.vertex.y;
                #endif
                if (zoneId >= 5u) o.vertex = float4(2.0, 2.0, 0.0, 1.0);
                return o;
            }

            uint frag(V2F input) : SV_Target
            {
                uint3 minimum = 16u; 
                uint3 maximum = 0u;
                int2 source = SourceOffsets[input.zoneId];
                [loop] for (int i = 0; i < 16; i++)
                [loop] for (int j = 0; j < 16; j++)
                {
                    uint data = _LodGen.Load(int4(source + int2(i, j), input.layer, 0));
                    if (!(data & 1u)) continue;
                    uint3 low = uint3(data >> 1 & 31u, data >> 11 & 31u, data >> 21 & 31u);
                    uint3 high = uint3(data >> 6 & 31u, data >> 16 & 31u, data >> 26 & 31u);
                    minimum = min(minimum, low);
                    maximum = max(maximum, high);
                }
                if (minimum.x > maximum.x) return 0u;
                return 1u | minimum.x << 1 | maximum.x << 6 |
                       minimum.y << 11 | maximum.y << 16 |
                       minimum.z << 21 | maximum.z << 26;
            }
            ENDHLSL
        }
    }
}
