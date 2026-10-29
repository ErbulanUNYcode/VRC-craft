Shader "VRC_MINE/DDA"
{
    Properties
    {
        _WorldTex0 ("World Texture 0", 2D) = "black" {}
        _WorldTex1 ("World Texture 1", 2D) = "black" {}
        _WorldTex2 ("World Texture 2", 2D) = "black" {}
        _WorldTex3 ("World Texture 3", 2D) = "black" {}
        _WorldTex4 ("World Texture 4", 2D) = "black" {}
        _WorldTex5 ("World Texture 5", 2D) = "black" {}
        _WorldTex6 ("World Texture 6", 2D) = "black" {}
        _WorldTex7 ("World Texture 7", 2D) = "black" {}
        _WorldTex8 ("World Texture 8", 2D) = "black" {}
        _WorldTex9 ("World Texture 9", 2D) = "black" {}
        _WorldTex10 ("World Texture 10", 2D) = "black" {}
        _WorldTex11 ("World Texture 11", 2D) = "black" {}
        _WorldTex12 ("World Texture 12", 2D) = "black" {}
        _WorldTex13 ("World Texture 13", 2D) = "black" {}
        _WorldTex14 ("World Texture 14", 2D) = "black" {}
        _WorldTex15 ("World Texture 15", 2D) = "black" {}
        _LodTex1 ("LOD Texture 1", 2D) = "black" {}
        _LodTex2 ("LOD Texture 2", 2D) = "black" {}
        _LodTex3 ("LOD Texture 3", 2D) = "black" {}
        _Offset ("Depth Scale", Float) = 1.0
        _Mul ("Mul", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Cull Off
        ZWrite On
        ZTest LEqual
        Blend Off

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            Texture2D<fixed> _WorldTex0;
            Texture2D<fixed> _WorldTex1;
            Texture2D<fixed> _WorldTex2;
            Texture2D<fixed> _WorldTex3;
            Texture2D<fixed> _WorldTex4;
            Texture2D<fixed> _WorldTex5;
            Texture2D<fixed> _WorldTex6;
            Texture2D<fixed> _WorldTex7;
            Texture2D<fixed> _WorldTex8;
            Texture2D<fixed> _WorldTex9;
            Texture2D<fixed> _WorldTex10;
            Texture2D<fixed> _WorldTex11;
            Texture2D<fixed> _WorldTex12;
            Texture2D<fixed> _WorldTex13;
            Texture2D<fixed> _WorldTex14;
            Texture2D<fixed> _WorldTex15;
            Texture2D<uint> _LodTex1;
            Texture2D<uint> _LodTex2;
            Texture2D<uint> _LodTex3;
            float _Offset;
            float _Mul;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 ray : TEXCOORD0;
                nointerpolation float3 pos : TEXCOORD1;
                nointerpolation uint packedAntiWall : TEXCOORD2;
            };

            struct FragmentOutput
            {
                float4 color : SV_Target;
                float depth : SV_Depth;
            };

            v2f vert(appdata v)
            {
                v2f o;

                float4x4 view = UNITY_MATRIX_V;

                o.pos = _WorldSpaceCameraPos;

                view._m03 = 0;
                view._m13 = 0;
                view._m23 = 0;

                o.vertex = mul(UNITY_MATRIX_P, mul(view, v.vertex));
                o.ray = v.vertex.xyz;

                int3 voxel = (int3)floor(o.pos);
                int3 step = (int3)round(v.color.xyz) * 2 - 1;
                uint packedAntiWall =
                    (uint)(voxel.x & 63) |
                    (uint)(voxel.y & 63) << 6 |
                    (uint)(voxel.z & 63) << 12;

                // X: 0b000000000000111111
                // Y: 0b000000111111000000
                // Z: 0b111111000000000000
                if (step.x < 0) packedAntiWall ^= 63;
                if (step.y < 0) packedAntiWall ^= 63 << 6;
                if (step.z < 0) packedAntiWall ^= 63 << 12;

                uint currentLod;
                if (voxel.y > 255 || voxel.y < 0)
                    currentLod = 7;
                else
                {
                    int3 pullVox = voxel&1023;
                    uint lodVoxel = _LodTex3.Load(int3(((pullVox.xz)>>6)+int2((pullVox.y>>6&1)<<4,pullVox.y>>7<<4), 0));
                    if(lodVoxel == 0) currentLod = 7;
                    else if(lodVoxel >> 
                        ( pullVox.x >> 5 & 1) + 
                        ((pullVox.y >> 5 & 1) << 1) + 
                        ((pullVox.z >> 5 & 1) << 2)
                        & 1)
                    {
                        lodVoxel = _LodTex2.Load(int3(((pullVox.xz)>>4)+int2((pullVox.y>>4&3)<<6,pullVox.y>>6<<6), 0));
                        if(lodVoxel == 0) currentLod = 5;
                        else if(lodVoxel >>
                            ( pullVox.x >> 3 & 1) +
                            ((pullVox.y >> 3 & 1) << 1) +
                            ((pullVox.z >> 3 & 1) << 2)
                            & 1)
                        {
                            lodVoxel = _LodTex1.Load(int3(((pullVox.xz)>>2)+int2((pullVox.y>>2&7)<<8,pullVox.y>>5<<8), 0));
                            if(lodVoxel == 0) currentLod = 3;
                            else if(lodVoxel >>
                                ( pullVox.x >> 1 & 1) +
                                ((pullVox.y >> 1 & 1) << 1) +
                                ((pullVox.z >> 1 & 1) << 2)
                                & 1)
                                currentLod = 1;
                            else currentLod = 2;
                        }
                        else currentLod = 4;
                    }
                    else currentLod = 6;
                }

                packedAntiWall |= (currentLod & 7) << 18;
                o.packedAntiWall = packedAntiWall;

                return o;
            }

            bool block(int3 pos)
            {
                if(pos.y > 255 || pos.y < 0) return 0;
                int2 ch = ((pos.xz>>4)&63)<<8;
                pos.xz&=15;
                pos.x+=pos.z<<4;
                return _WorldTex0.Load(int3(pos.xy+ch,0));
            }

            FragmentOutput frag(v2f i)
            {
                FragmentOutput o;

                float3 ray = normalize(i.ray);
                int3 step = ray > 0 ? 1 : -1;

                float3 antiWall = step == 1
                    ? -frac(i.pos)
                    : -1.0 + frac(i.pos);

                uint packedAntiWall = i.packedAntiWall;
                int3 vox = (int3)floor(i.pos);
                float3 stepRay = abs(1.0 / ray);
                float3 starts0 = -abs(antiWall / ray);
                float3 starts1 = starts0 + float3((packedAntiWall >> 0) & 1, (packedAntiWall >> 6) & 1, (packedAntiWall >> 12) & 1) * stepRay;
                float3 starts2 = starts1 + float3((packedAntiWall >> 1) & 1, (packedAntiWall >> 7) & 1, (packedAntiWall >> 13) & 1) * (stepRay * 2);
                float3 starts3 = starts2 + float3((packedAntiWall >> 2) & 1, (packedAntiWall >> 8) & 1, (packedAntiWall >> 14) & 1) * (stepRay * 4);
                float3 starts4 = starts3 + float3((packedAntiWall >> 3) & 1, (packedAntiWall >> 9) & 1, (packedAntiWall >> 15) & 1) * (stepRay * 8);
                float3 starts5 = starts4 + float3((packedAntiWall >> 4) & 1, (packedAntiWall >> 10) & 1, (packedAntiWall >> 16) & 1) * (stepRay * 16);
                float3 starts6 = starts5 + float3((packedAntiWall >> 5) & 1, (packedAntiWall >> 11) & 1, (packedAntiWall >> 17) & 1) * (stepRay * 32);

                uint currentLod = (packedAntiWall >> 18) & 7;

                // Debug-only output: place the debug surface at the farthest depth.
                o.depth = 1;
                if (currentLod == 1) o.color = float4(1, 1, 1, 1);
                else if (currentLod == 2) o.color = float4(1, 0, 0, 1);
                else if (currentLod == 3) o.color = float4(0, 1, 0, 1);
                else if (currentLod == 4) o.color = float4(0, 0, 1, 1);
                else if (currentLod == 5) o.color = float4(1, 0, 1, 1);
                else if (currentLod == 6) o.color = float4(1, 1, 0, 1);
                else o.color = float4(0, 0, 0, 1);
                return o;

                float3 starts = starts0;
                float3 depth = starts;

                [loop]
                for (int j = 0; j < 100; j++)
                {
                    float3 next = depth + stepRay;

                    if (next.x < next.y && next.x < next.z)
                    {
                        depth.x = next.x;
                        vox.x += step.x;

                        if (block(vox))
                        {
                            float3 hit = ray*depth.x;
                            float3 view = mul((float3x3)UNITY_MATRIX_V, hit);
                            float4 clip = mul(UNITY_MATRIX_P, float4(view, 1.0));

                            float dist = depth.x / 50.0;
                            o.color = float4(dist, dist, dist, 1.0);
                            o.depth = clip.z / clip.w;
#if defined(SHADER_API_MOBILE)
                            o.depth = (o.depth-UNITY_NEAR_CLIP_VALUE)/(1-UNITY_NEAR_CLIP_VALUE);
#endif
                            return o;
                        }
                    }
                    else if (next.y < next.z)
                    {
                        depth.y = next.y;
                        vox.y += step.y;

                        if (block(vox))
                        {
                            float3 hit = ray * depth.y;
                            float3 view = mul((float3x3)UNITY_MATRIX_V, hit);
                            float4 clip = mul(UNITY_MATRIX_P, float4(view, 1.0));

                            float dist = depth.y / 50.0;
                            o.color = float4(dist, dist, dist, 1.0);
                            o.depth = clip.z / clip.w;
#if defined(SHADER_API_MOBILE)
                            o.depth = (o.depth-UNITY_NEAR_CLIP_VALUE)/(1-UNITY_NEAR_CLIP_VALUE);
#endif
                            return o;
                        }
                    }
                    else
                    {
                        depth.z = next.z;
                        vox.z += step.z;

                        if (block(vox))
                        {
                            float3 hit = ray * depth.z;
                            float3 view = mul((float3x3)UNITY_MATRIX_V, hit);
                            float4 clip = mul(UNITY_MATRIX_P, float4(view, 1.0));

                            float dist = depth.z / 50.0;
                            o.color = float4(dist, dist, dist, 1.0);
                            o.depth = clip.z / clip.w;
#if defined(SHADER_API_MOBILE)
                            o.depth = (o.depth-UNITY_NEAR_CLIP_VALUE)/(1-UNITY_NEAR_CLIP_VALUE);
#endif
                            return o;
                        }
                    }
                }

                clip(-1);

                o.color = 0;
                o.depth = 1;

                return o;
            }

            ENDCG
        }
    }
}
