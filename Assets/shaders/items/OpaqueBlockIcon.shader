Shader "Custom/OpaqueBlockIcon"
{
    Properties
    {
        _TopTex ("Top Texture", 2D) = "white" {}
        _SideTex ("Side Texture", 2D) = "white" {}
        _BottomTex ("Bottom Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        CGPROGRAM
        #pragma surface surf Standard alpha:fade 
        
        sampler2D _TopTex;
        sampler2D _SideTex;
        sampler2D _BottomTex;
        float _TileX, _TileY, _TileZ;

        struct Input
        {
            float3 worldNormal;
            float2 uv_TopTex;
            float2 uv_SideTex;
            float2 uv_BottomTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 n = mul(unity_WorldToObject, float4(IN.worldNormal, 0)).xyz;
            fixed4 col;

            if (n.y > 0.5)        
                col = tex2D(_TopTex, IN.uv_TopTex);
            else if (n.y < -0.5)  
                col = tex2D(_BottomTex, IN.uv_BottomTex);
            else if (n.z < -0.5)
                col = tex2D(_SideTex, 1 - IN.uv_SideTex);
            else
                col = tex2D(_SideTex, IN.uv_SideTex);

            o.Albedo = col.rgb;
            o.Alpha = col.a;
        }
        ENDCG
    }
    FallBack "Transparent"
}
