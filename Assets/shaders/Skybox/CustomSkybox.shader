Shader "Unlit/CustomSkybox"
{
    Properties
    {
        _TopCol ("Top Color", Color) = (0.0, 0.5, 1.0, 1.0)
        _BottomCol ("Bottom Color", Color) = (0.0, 0.0, 0.2, 1.0)
        _HorizonCol ("Horizon Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _ReleCol ("Relief Color", Color) = (1.0, 0.8, 0.5, 1.0)
        _SunLightCol ("Sun Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Angle ("Angle", Range(-180, 180)) = -180
        _TimePower ("Time Power", Range(-1, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            fixed4 _TopCol;
            fixed4 _BottomCol;
            fixed4 _HorizonCol;
            fixed4 _ReleCol;
            fixed4 _SunLightCol;
            float _Angle;
            float _TimePower;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 viewDir : TEXCOORD0;
            };

            v2f vert (float4 vertex : POSITION)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);

                float4 worldPos = mul(unity_ObjectToWorld, vertex);
                float3 viewDirection = normalize(worldPos.xyz - _WorldSpaceCameraPos.xyz);
                o.viewDir = viewDirection;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = i.viewDir;
                float h = max(0,dir.y-0.4);

                float a = atan2(dir.z, dir.x);
                float d= length(dir.xz);
                dir.x = sin(a+radians(_Angle))*d;

                a = atan2(dir.x, dir.y);
                d = length(dir.xy);
                dir.x = sin(a+_TimePower)*d;

                float alphaGradient = smoothstep(0.15,-0.15,dir.y);

                float3 col = alphaGradient * _HorizonCol*2 + (1-alphaGradient) * _TopCol;

                /*alphaGradient = smoothstep(0.15,0.25,);
                float4 sunRef = */

                col += _SunLightCol * smoothstep(-1,2,dir.x);

                alphaGradient = smoothstep(-0.05,0,dir.y);

                col = alphaGradient * col + (1-alphaGradient) * _BottomCol;

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
