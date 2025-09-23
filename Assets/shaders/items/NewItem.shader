Shader "Unlit/NewItem"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_LightTex ("Light", 2D) = "white" {}
		_InputTime ("ImputTime", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO
            #include "UnityCG.cginc"


            sampler2D _MainTex;
			sampler2D _LightTex;
			float _InputTime;

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
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
                float3 worldToCam = _WorldSpaceCameraPos - i.worldPos;
                float3 localToCam = mul(unity_WorldToObject, float4(worldToCam, 0.0)).xyz;
                float3 pos = mul(unity_WorldToObject, float4(i.worldPos, 1.0)).xyz;

                float3 orig = pos;
                orig.z *= 0.99;
                orig.z += 0.5;
                orig.xy *= 15.99;
                orig.xy += 8;

                int3 current = int3(orig);

                float3 dir = -localToCam;
                dir.xy *= 16;
                int3 step = int3(dir.x>0?1:-1, dir.y>0?1:-1, dir.z>0?1:-1);

                float3 tMax = float3(
                    intBound(orig.x, dir.x),
                    intBound(orig.y, dir.y),
                    intBound(orig.z, dir.z)
                );

                float3 tDelta = float3(
                    abs(1 / dir.x),
                    abs(1 / dir.y),
                    abs(1 / dir.z)
                );

                fixed4 c = fixed4(0,0,1,1);

                float3 normals = 0;
                float3 absPos = abs(pos);
                if(absPos.x>absPos.y && absPos.x>absPos.z) normals.x = step.x;
                if(absPos.y>absPos.x && absPos.y>absPos.z) normals.y = step.y;
                if(absPos.z>absPos.x && absPos.z>absPos.y) normals.z = step.z;

                for (int k = 0; k < 32; k++)
                {
                    if (k!=0&&(current.x < 0 || current.x > 15 ||
                         current.y < 0 || current.y > 15 ||
                         current.z != 0))
                        break;

                    fixed4 col = tex2D(_MainTex, (current.xy+0.5) / 16);

                    if(col.a > 0.5)
                    {
                        normals = UnityObjectToWorldNormal(normals);
                        float3 absNormals = abs(normals);
                        float timee = 0.49+_InputTime;
                        float4 light = float4(0,0,0,1);
                        light += tex2D(_LightTex, float2(normals.x>0?1.0/12*5:1.0/12*7,timee))*absNormals.x;
                        light += tex2D(_LightTex, float2(normals.y>0?1.0/12*3:1.0/12,timee))*absNormals.y;
                        light += tex2D(_LightTex, float2(normals.z>0?1.0/12*9:1.0/12*11,timee))*absNormals.z;
                        light.a = 1;
                        return col*light;
                    }

                    if (tMax.x < tMax.y && tMax.x < tMax.z)
                    {
                        tMax.x += tDelta.x;
                        current.x += step.x;
                        c = fixed4(1,0,0,1);
                        normals = float3(step.x,0,0);
                    }
                    else if (tMax.y < tMax.z)
                    {
                        tMax.y += tDelta.y;
                        current.y += step.y;
                        c = fixed4(0,1,0,1);
                        normals = float3(0,step.y,0);
                    }
                    else
                    {
                        break;
                    }
                }

                return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }
}
