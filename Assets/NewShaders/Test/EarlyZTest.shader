Shader "Unlit/EarlyZTest"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+1" }

        // The first pass marks pixels where the original mesh is visible.
        Pass
        {
            Name "GEOMETRY_MASK"
            Cull Back
            ZTest LEqual
            ZWrite Off
            ColorMask 0

            Stencil
            {
                Ref 1
                ReadMask 1
                WriteMask 1
                Comp Always
                Pass Replace
                ZFail Keep
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 position : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 frag(v2f i) : SV_Target { return 0; }
            ENDHLSL
        }

        // The second pass writes color and the alpha-controlled depth.
        Pass
        {
            Name "DYNAMIC_DEPTH"
            Cull Back
            ZTest LEqual
            ZWrite On

            Stencil
            {
                Ref 1
                ReadMask 1
                WriteMask 1
                Comp Equal
                Pass Zero
                ZFail Zero
            }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float4 color : COLOR;
            };

            struct FragmentOutput
            {
                float4 color : SV_Target;
                float depth : SV_Depth;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }

            FragmentOutput frag(v2f i)
            {
                clip(0.8 - i.color.r);

                FragmentOutput o;
                o.color = float4(i.color.rgb, 1.0);
#if defined(UNITY_REVERSED_Z)
                o.depth = lerp(i.position.z, 1.0, saturate(i.color.a));
#else
                o.depth = lerp(i.position.z, 0.0, saturate(i.color.a));
#endif
                return o;
            }
            ENDHLSL
        }
    }
}
