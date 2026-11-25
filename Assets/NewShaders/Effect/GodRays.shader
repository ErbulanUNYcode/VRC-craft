Shader "VRC_MINE/GodRays"
{
    Properties
    {
        _SunMask ("Left Sun Mask", 2D) = "black" {}
        _RightSunMask ("Right Sun Mask", 2D) = "black" {}
        _PhotoMask ("Handheld Camera Mask", 2D) = "black" {}
        [HideInInspector] _EyeLightColor ("Eye Light Color", Color) = (1,1,1,1)
        [HideInInspector] _PhotoLightColor ("Photo Light Color", Color) = (1,1,1,1)
        [ToggleUI] _EditorFlarePreview ("Editor Flare Preview", Float) = 0
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
            float4 _SunMask_TexelSize;
            sampler2D _RightSunMask;
            float4 _RightSunMask_TexelSize;
            sampler2D _PhotoMask;
            float4 _PhotoMask_TexelSize;
            float4x4 _LeftLightProjection;
            float4x4 _RightLightProjection;
            float4x4 _PhotoLightProjection;
            float4 _EyeLightColor;
            float4 _PhotoLightColor;
            float4 _MoonFlareCameras;
            float _PhotoMaskReady;
            float _EditorFlarePreview;
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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            static const float2 FlareMaskOffsets[7] =
            {
                float2(0, 0),
                float2(3, 0), float2(1.5, 2.5980762), float2(-1.5, 2.5980762),
                float2(-3, 0), float2(-1.5, -2.5980762), float2(1.5, -2.5980762)
            };

            float ReadFlareVisibility(sampler2D mask, float2 texelSize)
            {
                float occlusion = 0;
                [unroll]
                for (int tap = 0; tap < 7; tap++)
                {
                    float2 uv = 0.5 + FlareMaskOffsets[tap] * texelSize;
                    occlusion += tex2Dlod(mask, float4(uv, 0, 0)).a;
                }
                return saturate(1.0 - occlusion / 7.0);
            }

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);


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
                if (photo)
                {
                    lightProjection = _PhotoLightProjection;
                }
                else if (unity_StereoEyeIndex != 0)
                {
                    lightProjection = _RightLightProjection;
                }
                float4 lightClip = mul(lightProjection, float4(direction, 0));
                o.sunRay = float3(lightClip.xy, lightClip.w);

                o.screen = 0;
                o.lightScreen = 0;
                [branch]
                if (photo ? _PhotoMaskReady != 0 : _EditorFlarePreview != 0)
                {
                    o.screen = ComputeNonStereoScreenPos(o.position);
                    float3 lightDirection = lightProjection[3].xyz;
                    float3 lightView = mul((float3x3)UNITY_MATRIX_V, lightDirection);
                    o.lightScreen = ComputeNonStereoScreenPos(mul(UNITY_MATRIX_P, float4(lightView, 0)));
                }
                return o;
            }

            float3 PhotoFlare(v2f i, float isMoon)
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
                float3 flare = float3(0.25, 0.19, 0.12) * (core * core * (1.0 - isMoon));

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

                // A larger circle softens the edge farthest from the screen center.
                const float circleRadius = 0.079;
                float2 circlePoint = pixel + axis * 1.04;
                float circleDistanceSquared = dot(circlePoint, circlePoint);
                [branch]
                if (circleDistanceSquared <= circleRadius * circleRadius)
                {
                    float2 maskOffset = axis * (1.67 - 1.04);
                    float2 maskPoint = circlePoint + maskOffset;
                    // Internal tangency at the edge opposite the mask center.
                    float maskRadius = circleRadius + length(maskOffset);
                    float remaining = saturate(3.0 * length(maskPoint) / maskRadius - 2.0);
                    float hue = (1.0 - remaining) * 0.78;
                    float3 rainbow = saturate(abs(frac(hue + float3(0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0) - 1.0);
                    float edgeFade = 1.0 - smoothstep(0.7, 1.0, sqrt(circleDistanceSquared) / circleRadius);
                    float rainbowLuminance = dot(rainbow, float3(0.2126, 0.7152, 0.0722));
                    rainbow = lerp(rainbow, rainbowLuminance.xxx, 0.5 * isMoon);
                    flare += rainbow * (0.05 * remaining * edgeFade);
                }

                // Wide, defocused amber oval.
                float ovalRadius = length((pixel + axis * 1.5) * float2(0.64, 1.25));
                float oval = 1.0 - smoothstep(0.0, 0.098, ovalRadius);
                flare += float3(0.075, 0.044, 0.012) * oval * oval;

                // Narrow horizontal blue streak across the source.
                float2 streakPoint = abs(pixel - axis);
                float streak = (1.0 - smoothstep(0.0375, 0.36, streakPoint.x))
                             * (1.0 - smoothstep(0.001, 0.012, streakPoint.y));
                flare += float3(0.023, 0.065, 0.11) * streak;

                return flare * edge;
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
                [branch]
                if (_VRChatCameraMode == 1 || _VRChatCameraMode == 2)
                {
                    if (_PhotoMaskReady == 0) return 0;
                    float3 flare = PhotoFlare(i, _MoonFlareCameras.y) * (_PhotoLightColor.a * ReadFlareVisibility(_PhotoMask, _PhotoMask_TexelSize.xy));
                    return half4(IntegrateRays(_PhotoMask, i.sunRay) * _PhotoLightColor.rgb + flare, 0);
                }

                bool leftEye = unity_StereoEyeIndex == 0;
                float3 eyeFlare = 0;
                [branch]
                if (_EditorFlarePreview != 0)
                {
                    float visibility = leftEye ? ReadFlareVisibility(_SunMask, _SunMask_TexelSize.xy) : ReadFlareVisibility(_RightSunMask, _RightSunMask_TexelSize.xy);
                    eyeFlare = PhotoFlare(i, _MoonFlareCameras.x) * (_EyeLightColor.a * visibility);
                }

                float brightness = leftEye ? IntegrateRays(_SunMask, i.sunRay) : IntegrateRays(_RightSunMask, i.sunRay);
                return half4(brightness * _EyeLightColor.rgb + eyeFlare, 0);
            }
            ENDCG
        }
    }
}
