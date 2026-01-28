Shader "Custom/UnlitBlockIcon_FlatNormalsCustomUV"
{
	Properties
	{
		_MainTex ("MainTex", 2D) = "white" {}
		_LightTex ("Light", 2D) = "white" {}
		_InputTime ("InputTime", Float) = 0
		//_BlockID ("Block ID", Range(0,500)) = 1
	}
	SubShader
	{
		Tags {"DisableBatching" = "True" "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
		LOD 100
		Cull Off
		ZWrite On
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _LightTex;
			float _InputTime;
			//float _BlockID;

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float tileIndex : TEXCOORD1;
				float3 localPos : TEXCOORD0;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.localPos = v.color.rgb-0.5;
				o.tileIndex = round(v.color.a * 255);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float3 n = -normalize(cross(ddx(i.localPos), ddy(i.localPos)));

				float3 texN =
				round(n.y*2)!=0?(i.localPos.y>0?float3(0,1,0):float3(0,-1,0)):
				round(n.x*2)!=0?(i.localPos.x>0?float3(1,0,0):float3(-1,0,0)):
				round(n.z*2)!=0?(i.localPos.z>0?float3(0,0,1):float3(0,0,-1)):
				round(n);
				i.localPos /= 1.01;
				float2 uv= 0.5 +
				(texN.y ==  1? i.localPos.zx :
				texN.y == -1? i.localPos.zx+float2(1,0) :
				texN.x ==  1? i.localPos.zy+float2(0,1) :
				texN.x == -1? i.localPos.zy*float2(-1,1)+1 :
				texN.z ==  1? i.localPos.xy*float2(-1,1)+float2(0,2) :
							  i.localPos.xy+float2(1,2));

				float2 blockOffset = float2(fmod(i.tileIndex*2,32), floor(i.tileIndex/16) * 3);
				uv = (uv + blockOffset) / 32.0;

				fixed4 col = tex2D(_MainTex, frac(uv));

				clip(col.a - 0.5);

				n = UnityObjectToWorldNormal(-n);
				float3 absNormals = abs(n);
				float timee = 0.49+_InputTime;
				float4 light = float4(0,0,0,1);
				light += tex2D(_LightTex, float2(n.x>0?1.0/12*5:1.0/12*7,timee))*absNormals.x;
				light += tex2D(_LightTex, float2(n.y>0?1.0/12*3:1.0/12,timee))*absNormals.y;
				light += tex2D(_LightTex, float2(n.z>0?1.0/12*9:1.0/12*11,timee))*absNormals.z;
				light.a = 1;


				return col*light;
			}
			ENDCG
		}
	}
	FallBack "Unlit/Transparent Cutout"
}
