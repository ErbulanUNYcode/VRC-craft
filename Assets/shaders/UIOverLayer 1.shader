Shader "Unlit/UIBlockOverLayer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlockID ("Block ID", Range(1,68)) = 1
        _Side ("Side", Range(0,2)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Pass
        {
            ZTest Always
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            float _BlockID;
            float _Side;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float id = _BlockID*2;
                float2 uv = (i.uv + float2(id%32,floor(id/32)*3+_Side))/32;
                
                fixed4 texColor = tex2D(_MainTex, uv);
                return texColor * i.color;
            }
            ENDCG
        }
    }
}
