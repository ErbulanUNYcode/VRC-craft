Shader "Unlit/GROU_Additive"
{
    Properties
    {
        _LeftTex ("Texture", 2D) = "white" {}
        _RightTex ("Texture", 2D) = "white" {}
        _Angle ("Angle", Range(-180, 180)) = 50
        _TimePower ("Time Power", Range(0, 8)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Blend One One
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float4 vertex    : SV_POSITION;
                float4 color     : COLOR;
                float3 viewDir   : TEXCOORD2;   // добавили
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _LeftTex;
            sampler2D _RightTex;
            float _Angle;
            float _TimePower;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color  = v.color;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(worldPos - _WorldSpaceCameraPos.xyz);

                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            
            float2 rotate(float2 p, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                return float2(
                    c * p.x - s * p.y,
                    s * p.x + c * p.y
                );
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float3 dir = i.viewDir; // как в skybox
                

                dir.xy = rotate(dir.xy,radians(_Angle));
                float r = radians(_TimePower*360);
                dir.yz = rotate(dir.yz,r);
                dir.xy/=dir.z;
                if(abs(dir.x)>0.5||abs(dir.y)>0.5 || dir.z<0) return 0;

                int eyeIndex = unity_StereoEyeIndex;

                float col = 0;
                if(eyeIndex == 0)
                {
                    for (float j = 1; j > 0.3; j -= 0.02)
                    {
                        float2 uv = (dir.xy) * 2.85 * j + 0.5;
                        col += (1 - tex2D(_LeftTex, uv)).a * 0.05 * (j - 0.3);
                    }
                }
                else
                {
                    for (float j = 1; j > 0.3; j -= 0.02)
                    {
                        float2 uv = (dir.xy) * 2.85 * j + 0.5;
                        col += (1 - tex2D(_RightTex, uv)).a * 0.05 * (j - 0.3);
                    }
                }
                return fixed4(col, col, col, 1);
            }
            ENDCG
        }
    }
}
