Shader "VRC_MINE/GodRays"
{
    Properties
    {
        _SunMask ("Left Sun Mask", 2D) = "black" {}
        _RightSunMask ("Right Sun Mask", 2D) = "black" {}
        _PhotoMask ("Handheld Camera Mask", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "Queue"="Transparent+50" "RenderType"="Transparent" "DisableBatching"="True" }
        Pass
        {
            Cull Off
            ZTest Always
            ZWrite Off
            Blend One One
            ColorMask RGB

            CGPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            sampler2D _SunMask;
            sampler2D _RightSunMask;
            sampler2D _PhotoMask;
            float4 _PhotoMask_TexelSize;
            float4x4 _LeftLightProjection;
            float4x4 _RightLightProjection;
            float4x4 _PhotoLightProjection;
            float4 _MoonMask;
            float _PhotoMaskReady;
            float _VRChatMirrorMode;
            float _VRChatCameraMode;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 sunRay : TEXCOORD0;
                float4 screen : TEXCOORD1;
                nointerpolation float4 lightScreen : TEXCOORD2;
                nointerpolation float flareVisibility : TEXCOORD3;
                nointerpolation float3 rayTint : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float ReadFlareVisibility()
            {
                float2 halfTexel = 0.5 * _PhotoMask_TexelSize.xy;
                float4 offsets = float4(halfTexel, -halfTexel);
                float mask = tex2Dlod(_PhotoMask, float4(0.5 + offsets.xy, 0, 0)).a;
                mask += tex2Dlod(_PhotoMask, float4(0.5 + offsets.xw, 0, 0)).a;
                mask += tex2Dlod(_PhotoMask, float4(0.5 + offsets.zy, 0, 0)).a;
                mask += tex2Dlod(_PhotoMask, float4(0.5 + offsets.zw, 0, 0)).a;
                return saturate(1.0 - mask * 0.25);
            }

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.flareVisibility = 0;
                o.rayTint = 1;

                // VoxelCanvas also contains helper triangles marked with zero alpha.
                if (v.color.a == 0 || _VRChatMirrorMode != 0 || _VRChatCameraMode == 3)
                {
                    o.position = float4(2, 2, 0, 1);
                    o.sunRay = 0;
                    o.screen = o.lightScreen = 0;
                    return o;
                }

                float3 direction = v.vertex.xyz;
                float3 viewDirection = mul((float3x3)UNITY_MATRIX_V, direction);
                o.position = mul(UNITY_MATRIX_P, float4(viewDirection, 1));
                o.position.z = 0.5 * o.position.w;
                bool photo = _VRChatCameraMode == 1 || _VRChatCameraMode == 2;
                float4x4 lightProjection = _LeftLightProjection;
                float moon = _MoonMask.x;
                if (photo)
                {
                    lightProjection = _PhotoLightProjection;
                    moon = _MoonMask.z;
                }
                else if (unity_StereoEyeIndex != 0)
                {
                    lightProjection = _RightLightProjection;
                }
                float4 lightClip = mul(lightProjection, float4(direction, 0));
                o.sunRay = float3(lightClip.xy, lightClip.w);
                o.rayTint = lerp(float3(1, 1, 1), float3(0.22, 0.28, 0.38), moon);

                o.screen = 0;
                o.lightScreen = 0;
                [branch]
                if (photo && _PhotoMaskReady != 0)
                {
                    o.screen = ComputeNonStereoScreenPos(o.position);
                    float3 lightDirection = lightProjection[3].xyz;
                    float3 lightView = mul((float3x3)UNITY_MATRIX_V, lightDirection);
                    o.lightScreen = ComputeNonStereoScreenPos(mul(UNITY_MATRIX_P, float4(lightView, 0)));
                    if (o.lightScreen.w > 0)
                        o.flareVisibility = ReadFlareVisibility();
                }
                return o;
            }

            float3 PhotoFlare(v2f i)
            {
                if (i.lightScreen.w <= 0) return 0;
                float2 source = i.lightScreen.xy / i.lightScreen.w;
                float2 offCenter = abs(source - 0.5);
                float edge = 1.0 - smoothstep(0.42, 0.55, max(offCenter.x, offCenter.y));
                if (edge == 0) return 0;

                float2 aspect = float2(_ScreenParams.x / _ScreenParams.y, 1);
                float2 pixel = (i.screen.xy / i.screen.w - 0.5) * aspect;
                float2 axis = (source - 0.5) * aspect;
                // Warm bloom centred on the source.
                float core = 1.0 - smoothstep(0.0, 0.075, length(pixel - axis));
                float3 flare = float3(0.25, 0.19, 0.12) * core * core;

                // Seven side normals, folded across the horizontal symmetry axis.
                float2 heptPoint = pixel - axis * 0.36;
                heptPoint.y = abs(heptPoint.y);
                float heptagon = max(heptPoint.x, max(
                    dot(heptPoint, float2(0.6234898, 0.7818315)),
                    max(dot(heptPoint, float2(-0.2225209, 0.9749279)),
                        dot(heptPoint, float2(-0.9009689, 0.4338837)))));
                float heptRim = 1.0 - smoothstep(0.002, 0.010, abs(heptagon - 0.053));
                float heptFill = 1.0 - smoothstep(0.012, 0.053, heptagon);
                flare += float3(0.018, 0.07, 0.085) * (0.45 * heptFill + 0.55 * heptRim);

                // Small magenta diamond, brightest through its centre.
                float2 diamondPoint = abs(pixel + axis * 0.22);
                float diamond = 1.0 - smoothstep(0.003, 0.038, diamondPoint.x + diamondPoint.y);
                flare += float3(0.10, 0.031, 0.069) * diamond;

                // Thin ring with different radii for the three colour channels.
                float ringRadius = length(pixel + axis * 1.04);
                float3 ring = 1.0 - smoothstep(0.002, 0.012, abs(ringRadius - float3(0.084, 0.079, 0.074)));
                flare += ring * float3(0.061, 0.037, 0.053);

                // Wide, defocused amber oval.
                float ovalRadius = length((pixel + axis * 1.5) * float2(0.64, 1.25));
                float oval = 1.0 - smoothstep(0.0, 0.098, ovalRadius);
                flare += float3(0.075, 0.044, 0.012) * oval * oval;

                // Narrow horizontal blue streak across the source.
                float2 streakPoint = abs(pixel - axis);
                float streak = (1.0 - smoothstep(0.025, 0.24, streakPoint.x))
                             * (1.0 - smoothstep(0.001, 0.012, streakPoint.y));
                flare += float3(0.023, 0.065, 0.11) * streak;

                return flare * (i.flareVisibility * edge);
            }

            float IntegrateRays(sampler2D mask, float3 ray)
            {
                if (ray.z <= 0) return 0;
                float2 radial = 0.5 * ray.xy / ray.z;
                const float start = 0.3;
                float radius = max(abs(radial.x), abs(radial.y));
                float end = min(1.0, 0.5 / max(radius, 0.0001));
                if (end <= start) return 0;

                const int taps = 35;
                float stepSize = (end - start) / taps;
                float brightness = 0;
                [loop]
                for (int tap = 0; tap < taps; tap++)
                {
                    float distanceAlongRay = start + (tap + 0.5) * stepSize;
                    float2 uv = radial * distanceAlongRay + 0.5;
                    float light = 1.0 - tex2Dlod(mask, float4(uv, 0, 0)).a;
                    brightness += light * (distanceAlongRay - start);
                }
                return brightness * stepSize * 2.5;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float brightness;
                [branch]
                if (_VRChatCameraMode == 1 || _VRChatCameraMode == 2)
                {
                    if (_PhotoMaskReady == 0) return 0;
                    return half4((IntegrateRays(_PhotoMask, i.sunRay) + PhotoFlare(i)) * i.rayTint, 0);
                }
                else if (unity_StereoEyeIndex == 0)
                    brightness = IntegrateRays(_SunMask, i.sunRay);
                else
                    brightness = IntegrateRays(_RightSunMask, i.sunRay);
                return half4(brightness * i.rayTint, 0);
            }
            ENDCG
        }
    }
}
