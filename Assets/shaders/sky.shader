Shader "Custom/NewSurfaceShader"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _DomeStrength ("Dome Strength", Range(0, 100)) = 0.5
		_DomePower ("Dome Power", Range(0, 100)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        //Blend One One   // Аддитивное смешение
        ZWrite Off      // Отключаем запись в Z-буфер
        //LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light typesCGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        
        float _DomeStrength;
        float _DomePower;

        struct Input
        {
            float2 uv_MainTex;
        };

        /*half _Glossiness;
        half _Metallic;
        fixed4 _Color;*/

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert(inout appdata_full v)
        {
            float2 uv = v.texcoord.xy * 2.0 - 1.0; // Переводим в диапазон -1..1
            float dist = dot(uv, uv); // Квадрат расстояния от центра
            v.vertex.y += pow(_DomeStrength * dist,2)*_DomePower-100; // Опускаем края
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
{
            float2 uv = (IN.uv_MainTex-0.5)*0.15+0.5;
            uv.x += _Time.y * 0.0001;
            float2 uv01 = (IN.uv_MainTex-0.5)*0.151+0.5;
            uv01.x += _Time.y * 0.0001;
            float2 uv1 = (IN.uv_MainTex-0.5)*0.152+0.5;
            uv1.x += _Time.y * 0.0001;
            float2 uv15 = (IN.uv_MainTex-0.5)*0.153+0.5;
            uv15.x += _Time.y * 0.0001;
            float2 uv2 = (IN.uv_MainTex-0.5)*0.154+0.5;
            uv2.x += _Time.y * 0.0001;
			float2 uv25 = (IN.uv_MainTex-0.5)*0.155+0.5;
			uv25.x += _Time.y * 0.0001;
            float2 uv3 = (IN.uv_MainTex-0.5)*0.156+0.5;
            uv3.x += _Time.y * 0.0001;
			float2 uv35 = (IN.uv_MainTex-0.5)*0.157+0.5;
			uv35.x += _Time.y * 0.0001;
            float2 uv4 = (IN.uv_MainTex-0.5)*0.158+0.5;
            uv4.x += _Time.y * 0.0001;
            float2 uv45 = (IN.uv_MainTex-0.5)*0.159+0.5;
			uv45.x += _Time.y * 0.0001;
    
            fixed4 c =
              tex2D (_MainTex, uv)*0.025 + 
              tex2D (_MainTex, uv01)*0.05  + 
              tex2D (_MainTex,  uv1)*0.075 + 
              tex2D (_MainTex, uv15)*0.1  + 
              tex2D (_MainTex,  uv2)*0.125 + 
              tex2D (_MainTex, uv25)*0.125  + 
			  tex2D (_MainTex,  uv3)*0.1 +
			  tex2D (_MainTex, uv35)*0.075  +
			  tex2D (_MainTex,  uv4)*0.05 +
			  tex2D (_MainTex, uv45)*0.025;
            o.Albedo = c.rgb;
            o.Metallic = 1;
            o.Smoothness = 1;
            o.Alpha = 1;
}
        ENDCG
    }
    FallBack "Diffuse"
}
