Shader "Unlit/Sky2"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            //ZTest Always
            //ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _DomeStrength;
            float _DomePower;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                float2 uv = v.uv * 2.0 - 1.0;
                float dist = dot(uv, uv);
                v.vertex.y += pow(2.5 * dist, 2) * 20.7 - 70;

                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uvOffsets[20];
                for (int j = 19; j >-1; j--)
                {
                    float scale = 0.15 + j * 0.0007;
                    uvOffsets[j] = (i.uv - 0.5) * scale + 0.5;
                    uvOffsets[j].x += _Time.y * 0.0001;
                }

                fixed4 col = fixed4(1,1,1, 0);
                for (int j = 0; j < 20; j++)
                {
                    float c = tex2D(_MainTex, uvOffsets[j]).r * (25-j)/100.0;
                    col.a +=c;
                    col.rgb *= 1-c;
                    col.rgb += (1-c)*c;
                }

                //UNITY_APPLY_FOG(i.fogCoord, col);
                float2 uv = i.uv * 2.0 - 1.0;
                float dist = max(0.6-dot(uv, uv),0);
                return col*dist;
            }
            ENDCG
        }
    }
}
