Shader "Unlit/VoxelView_UI_UV"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Cube ("Cubemap", Cube) = "" {}
        _CubeStrength ("Cubemap Strength", Range(0,2)) = 1
        _VoxelWidth  ("Voxel Width",  Float) = 128
        _VoxelHeight ("Voxel Height", Float) = 72
        _ReflectionRadius ("Reflection Radius", Float) = 5
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 100

        Pass
        {
            ZWrite Off
            ZTest On
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            samplerCUBE _Cube;

            float _CubeStrength;
            float _VoxelWidth;
            float _VoxelHeight;
            float _ReflectionRadius;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;

                float3 localToCam : TEXCOORD1;
                float3 worldPos : TEXCOORD2;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                float3 worldToCam = _WorldSpaceCameraPos - o.worldPos;

                o.localToCam = mul(unity_WorldToObject, float4(worldToCam, 0.0)).xyz;

                return o;
            }

            float intBound(float s, float ds)
            {
                if (ds == 0)
                    return 999999999;
                else
                {
                    float sOffset = ds > 0 ? ceil(s) - s : s - floor(s);
                    return sOffset / abs(ds);
                }
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float3 pos = float3(i.uv, 0);

                float2 vox = float2(_VoxelWidth, _VoxelHeight);

                float3 orig = pos;
                orig.xy *= vox - 0.01;

                int2 current = int2(orig.xy);

                float3 dir = -i.localToCam;

                float dirX = dir.z / dir.x / vox.x;
                float dirY = dir.z / dir.y / vox.y;

                dir.xy *= vox;

                int2 step = int2(dir.x > 0 ? 1 : -1, dir.y > 0 ? 1 : -1);

                float2 tMax = float2(
                    intBound(orig.x, dir.x),
                    intBound(orig.y, dir.y)
                );

                float2 tDelta = float2(
                    abs(1 / dir.x),
                    abs(1 / dir.y)
                );

                float currentZ = 0;

                float3 normal= float3(0,0,1);
                float3 offet = (i.worldPos - _WorldSpaceCameraPos)/_ReflectionRadius;
                float fade = dot(offet, offet);
                fixed4 refl = texCUBE(_Cube, reflect(-normalize(_WorldSpaceCameraPos - i.worldPos), normalize(UnityObjectToWorldNormal(float3(0,0,1)))))*0.6;
                if (fade > 1) return refl;
                fixed4 col;
                [loop]
                for (int k = 0; k < vox.x + vox.y; k++)
                {
                    if (current.x < 0 || current.x > vox.x - 1 ||
                        current.y < 0 || current.y > vox.y - 1)
                        break;

                    col = tex2D(_MainTex, (current.xy + 0.5) / vox);

                    if (col.a > 0.9999 - currentZ)
                    {
                        col.rgb *= 1 - currentZ;
                        col.a = 1;
                        if(normal.x==0 && normal.y==0 && normal.z==1)
                        {
                            col.rgb += refl.rgb*_CubeStrength;
                        }else
                        {
                            float3 normalWS = UnityObjectToWorldNormal(normal);
                            float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                            float3 refl = reflect(-viewDir, normalize(normalWS));
                            col.rgb += texCUBE(_Cube, refl).rgb*_CubeStrength;
                        }
                        break;
                    }

                    bool xStep = tMax.x < tMax.y;
                    tMax += xStep ? float2(tDelta.x, 0) : float2(0, tDelta.y);
                    current += xStep ? int2(step.x, 0) : int2(0, step.y);

                    float delta = xStep
                        ? current.x - orig.x + (step.x < 0 ? 1 : 0)
                        : current.y - orig.y + (step.y < 0 ? 1 : 0);

                    currentZ = (xStep ? dirX : dirY) * delta;

                    normal = xStep ? dirX>0? float3(1,0,0) : float3(-1,0,0) : dirY>0? float3(0,1,0) : float3(0,-1,0);
                    
                    if (col.a > 0.9999 - currentZ)
                    {
                        col.a=0;
                        break;
                    }
                }

                col *= 1-fade;
                col += refl*fade;
                return col;
            }
            ENDCG
        }
    }
}