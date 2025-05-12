Shader "Custom/Item"
{
    Properties
    {
        _Tex ("Texture", 2D) = "white" {}
        _StartX ("Start X", Float) = 0
        _StartY ("Start Y", Float) = 0
        _SizeX ("Size X", Float) = 16
        _SizeY ("Size Y", Float) = 16
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        LOD 200
        ZWrite On
        CGPROGRAM
        #pragma surface surf Standard

        sampler2D _Tex;
        float _StartX;
        float _StartY;
        float _SizeX;
        float _SizeY;

        struct Input
        {
            float3 worldNormal;
            float2 uv_Tex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 n = mul(unity_WorldToObject, float4(IN.worldNormal, 0)).xyz;
            fixed4 col;

            if (n.y > 0.5)        
                col = tex2D(_Tex, IN.uv_Tex / 16 * float2(_SizeX, _SizeY) + float2(_StartX, _StartY) / 16);
            else if (n.y < -0.5)  
                col = tex2D(_Tex, (IN.uv_Tex * float2(1, -1) + float2(0, 1)) / 16 * float2(_SizeX, _SizeY) + float2(_StartX, _StartY) / 16);
            else if (n.z < -0.5)
                col = tex2D(_Tex, IN.uv_Tex / 16 * float2(_SizeX, 1) + float2(_StartX, _StartY + _SizeY - 1) / 16);
            else if (n.z > 0.5)
                col = tex2D(_Tex, IN.uv_Tex / 16 * float2(_SizeX, 1) + float2(_StartX, _StartY) / 16);
            else if (n.x < -0.5)
                col = tex2D(_Tex, float2(IN.uv_Tex.y, IN.uv_Tex.x) / 16 * float2(1, _SizeY) + float2(_StartX + _SizeX - 1, _StartY) / 16);
            else if (n.x > 0.5)
                col = tex2D(_Tex, float2(IN.uv_Tex.y, 1 - IN.uv_Tex.x) / 16 * float2(1, _SizeY) + float2(_StartX, _StartY) / 16);

            o.Albedo = col.rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
