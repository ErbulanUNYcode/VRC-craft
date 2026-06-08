Shader "VRC_MINE/Editor/OptimizatorView"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            Texture2D<uint4> _MainTex;
            float _Scale;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                int2 size;
                _MainTex.GetDimensions(size.x, size.y);
                o.uv = v.uv*size;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                int2 p = int2(i.uv);

                float4 d = float4(_MainTex.Load(int3(p, 0)))/255;

                return d;
            }

            ENDCG
        }
    }
}