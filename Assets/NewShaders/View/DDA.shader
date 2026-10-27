Shader "VRC_MINE/DDA"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
        _MainTex0 ("World Texture 0", 2D) = "black" {}
        _MainTex1 ("World Texture 1", 2D) = "black" {}
        _MainTex2 ("World Texture 2", 2D) = "black" {}
        _MainTex3 ("World Texture 3", 2D) = "black" {}
        _MainTex4 ("World Texture 4", 2D) = "black" {}
        _MainTex5 ("World Texture 5", 2D) = "black" {}
        _MainTex6 ("World Texture 6", 2D) = "black" {}
        _MainTex7 ("World Texture 7", 2D) = "black" {}
        _MainTex8 ("World Texture 8", 2D) = "black" {}
        _MainTex9 ("World Texture 9", 2D) = "black" {}
        _MainTex10 ("World Texture 10", 2D) = "black" {}
        _MainTex11 ("World Texture 11", 2D) = "black" {}
        _MainTex12 ("World Texture 12", 2D) = "black" {}
        _MainTex13 ("World Texture 13", 2D) = "black" {}
        _MainTex14 ("World Texture 14", 2D) = "black" {}
        _MainTex15 ("World Texture 15", 2D) = "black" {}
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

            Texture2D<fixed> _MainTex;
            Texture2D<fixed> _MainTex0;
            Texture2D<fixed> _MainTex1;
            Texture2D<fixed> _MainTex2;
            Texture2D<fixed> _MainTex3;
            Texture2D<fixed> _MainTex4;
            Texture2D<fixed> _MainTex5;
            Texture2D<fixed> _MainTex6;
            Texture2D<fixed> _MainTex7;
            Texture2D<fixed> _MainTex8;
            Texture2D<fixed> _MainTex9;
            Texture2D<fixed> _MainTex10;
            Texture2D<fixed> _MainTex11;
            Texture2D<fixed> _MainTex12;
            Texture2D<fixed> _MainTex13;
            Texture2D<fixed> _MainTex14;
            Texture2D<fixed> _MainTex15;
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
                nointerpolation int3 step : TEXCOORD1;
                nointerpolation float3 pos : TEXCOORD2;
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
                o.step = (int3)round(v.color.xyz * 2.0 - 1.0);

                return o;
            }

            bool block(int3 pos)
            {
                if(pos.y > 255 || pos.y < 0) return 0;
                int2 ch = ((pos.xz>>4)&63)<<8;
                pos.xz&=15;
                pos.x+=pos.z<<4;
                return _MainTex.Load(int3(pos.xy+ch,0));
            }

            FragmentOutput frag(v2f i)
            {
                FragmentOutput o;

                float3 ray = normalize(i.ray);

                float3 antiwall = i.step == 1
                    ? -frac(i.pos)
                    : -1.0 + frac(i.pos);

                float3 starts = -abs(antiwall / ray);
                float3 depth = starts;

                float3 stepRay = abs(1.0 / ray);

                int3 vox = (int3)floor(i.pos);
                int3 step = i.step;

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
                            //o.depth = (o.depth-UNITY_NEAR_CLIP_VALUE)/(1-UNITY_NEAR_CLIP_VALUE);
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
                            //o.depth = (o.depth-UNITY_NEAR_CLIP_VALUE)/(1-UNITY_NEAR_CLIP_VALUE);
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
                            //o.depth = (o.depth-UNITY_NEAR_CLIP_VALUE)/(1-UNITY_NEAR_CLIP_VALUE);
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
