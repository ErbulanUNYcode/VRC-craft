Shader "Unlit/Tets"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_BiomeTex ("Biome", 2D) = "white" {}
		_PackTex ("Pack", 2D) = "white" {}
		_LightTex ("Light", 2D) = "white" {}
		_MesherTex ("Mesher", 2D) = "white" {}

		_Test ("Test", Float) = 0

		_Order ("Order", Float) = 0

		_InputTime ("ImputTime", Float) = 0
	}
	SubShader
	{
		Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
		LOD 100
		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog

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
				float3 worldPos : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};
			
			sampler2D _PackTex;
			Texture2D _MainTex;
			Texture2D _BiomeTex;
			sampler2D _LightTex;
			Texture2D _MesherTex;
			//float4 _MainTex_ST;
			//float _Test;
			float _Order;
			float _InputTime;

			v2f vert (appdata v)
			{
				v2f o;
				int3 pos = mul(unity_ObjectToWorld, v.vertex).xyz;
				pos.xz += 96;
				if(_Order==1)
				{
					pos.xz = (pos.xz%192+192)%192;
					if(v.color.r==1)
					{
						if(pos.z==0) pos.z = 192;
						int2 p = int2(pos.x,pos.z-32)/32;
						p.y = p.x + p.y*6;
						p.x = v.vertex.y;
						int2 data = round(_MesherTex.Load(int3(p,0)).ra*255);
						v.vertex.x += data.x;
						v.vertex.z -= data.y;
					}
					else if(v.color.g==1)
					{
						if(pos.x==0) pos.x = 192;
						if(pos.z==0) pos.z = 192;
						int2 p = int2(pos.x-32,pos.z-32)/32;
						p.y = p.x + p.y*6;
						p.x = v.vertex.y;
						int2 data = round(_MesherTex.Load(int3(p,0)).ga*255);
						v.vertex.x -= data.x;
						v.vertex.z -= data.y;
					}
					else if(v.color.b==1)
					{
						int2 p = int2(pos.x,pos.z)/32;
						p.y = p.x + p.y*6;
						p.x = v.vertex.y;
						int2 data = round(_MesherTex.Load(int3(p,0)).rb*255);
						v.vertex.x += data.x;
						v.vertex.z += data.y;
					}
					else if(v.color.a==1)
					{
						if(pos.x==0) pos.x = 192;
						int2 p = int2(pos.x-32,pos.z)/32;
						p.y = p.x + p.y*6;
						p.x = v.vertex.y;
						int2 data = round(_MesherTex.Load(int3(p,0)).gb*255);
						v.vertex.x -= data.x;
						v.vertex.z += data.y;
					}
				}
				else if(_Order==0)
				{
					pos.z = (pos.z%192+192)%192;
					pos.x = (pos.x%193+193)%193;
					if(v.color.r==1)
					{
						if(pos.z==0) pos.z = 192;
						int2 p = int2(pos.y-32,pos.z-32)/32;
						p.y = p.x*6 + p.y + 36;
						p.x = pos.x;
						int2 data = round(_MesherTex.Load(int3(p,0)).ag*255);
						v.vertex.y -= data.x;
						v.vertex.z -= data.y;
					}
					else if(v.color.g==1)
					{
						int2 p = int2(pos.y-32,pos.z)/32;
						p.y = p.x*6 + p.y + 36;
						p.x = pos.x;
						int2 data = round(_MesherTex.Load(int3(p,0)).ar*255);
						v.vertex.y -= data.x;
						v.vertex.z += data.y;
					}
					else if(v.color.b==1)
					{
						if(pos.z==0) pos.z = 192;
						int2 p = int2(pos.y,pos.z-32)/32;
						p.y = p.x*6 + p.y + 36;
						p.x = pos.x;
						int2 data = round(_MesherTex.Load(int3(p,0)).bg*255);
						v.vertex.y += data.x;
						v.vertex.z -= data.y;
					}
					else if(v.color.a==1)
					{
						int2 p = int2(pos.y,pos.z)/32;
						p.y = p.x*6 + p.y + 36;
						p.x = pos.x;
						int2 data = round(_MesherTex.Load(int3(p,0)).br*255);
						v.vertex.y += data.x;
						v.vertex.z += data.y;
					}
				}
				else if(_Order==2)
				{
					pos.x = (pos.x%192+192)%192;
					pos.z = (pos.z%193+193)%193;
					if(v.color.r==1)
					{
						int2 p = int2(pos.y-32,pos.x)/32;
						p.y = p.x*6 + p.y + 60;
						p.x = pos.z;
						int2 data = round(_MesherTex.Load(int3(p,0)).ra*255);
						v.vertex.x += data.x;
						v.vertex.y -= data.y;
					}
					else if(v.color.g==1)
					{
						if(pos.x==0) pos.x = 192;
						int2 p = int2(pos.y-32,pos.x-32)/32;
						p.y = p.x*6 + p.y + 60;
						p.x = pos.z;
						int2 data = round(_MesherTex.Load(int3(p,0)).ga*255);
						v.vertex.x -= data.x;
						v.vertex.y -= data.y;
					}
					else if(v.color.b==1)
					{
						int2 p = int2(pos.y,pos.x)/32;
						p.y = p.x*6 + p.y + 60;
						p.x = pos.z;
						int2 data = round(_MesherTex.Load(int3(p,0)).rb*255);
						v.vertex.x += data.x;
						v.vertex.y += data.y;
					}
					else if(v.color.a==1)
					{
						if(pos.x==0) pos.x = 192;
						int2 p = int2(pos.y,pos.x-32)/32;
						p.y = p.x*6 + p.y + 60;
						p.x = pos.z;
						int2 data = round(_MesherTex.Load(int3(p,0)).gb*255);
						v.vertex.x -= data.x;
						v.vertex.y += data.y;
					}
				}
				
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			int4 indexes(int3 p)
			{
				p.xz = p.xz & 255;

				int4 ids = 0;

				if (p.y == 128) ids.x = 0;
				else
				{
					int2 h = round(_MainTex.Load(int3(p.xz, 0)).ba * 255);

					if (h.x != 0)
					{
						int2 uv = p.xz + (int2(p.y & 15, p.y >> 4)<<8);

						int2 c = round(_MainTex.Load(int3(uv, 0)).rg * 255);

						if (c.x != 0)
							ids.x = c.x - 1;
						else if (p.y == 0)
							ids.x = 1;
						else
							ids.x = round(_BiomeTex.Load(int3(h.y,clamp(p.y-h.x+5,0,5), 0)).r * 255);

						ids.w = c.y;
					}
				}

				if (_Order == 0) p.x--;
				else if (_Order == 1) p.y--;
				else if (_Order == 2) p.z--;

				p.xz = p.xz & 255;

				if (p.y == -1) ids.y = 0;
				else
				{
					int2 h = round(_MainTex.Load(int3(p.xz, 0)).ba * 255);

					if (h.x != 0)
					{
						int2 uv = p.xz + (int2(p.y % 16, p.y >> 4)<<8);

						int2 c = round(_MainTex.Load(int3(uv, 0)).rg * 255);
						
						if (c.x != 0)
							ids.y = c.x - 1;
						else if (p.y == 0)
							ids.y = 1;
						else
							ids.y = round(_BiomeTex.Load(int3(h.y,clamp(p.y-h.x+5,0,5), 0)).r * 255);

						ids.w = c.y;
					}
				}

				return ids;
			}

			fixed4 frag(v2f i, fixed facing : VFACE) : SV_Target
			{
				int3 pos;

				if(_Order==0)
				{
					pos = int3(round(i.worldPos.x),floor(i.worldPos.y),floor(i.worldPos.z));
				}
				else if(_Order==1)
				{
					pos = int3(floor(i.worldPos.x),round(i.worldPos.y),floor(i.worldPos.z));
				}
				else if(_Order==2)
				{
					pos = int3(floor(i.worldPos.x),floor(i.worldPos.y),round(i.worldPos.z));
				}

				int4 id = indexes(pos);

				if(id.x==id.y)
				{
					clip(-0.5);
					return 0;
					/*float4 cc = float4(cos(i.worldPos.xyz*3.1415926535*2),1);
					if((cc.r<0.99||cc.r>0.999)&&(cc.g<0.99||cc.g>0.999)&&(cc.b<0.99||cc.b>0.999)) clip(-0.5);
					return cc;*/
				}

				id.xy<<=1;

				float2 u1;
				float2 u2;

				if(_Order==0)
				{
					u1 = (float2((id.x&31)+1,1+floor(id.x>>5)*3)+frac(float2(-i.worldPos.z,i.worldPos.y)))/32;
					u2 = (float2(id.y&31,1+floor(id.y>>5)*3)+frac(i.worldPos.zy))/32;
				}
				else if(_Order==1)
				{
					u1 = (float2((id.x&31)+1,0+floor(id.x>>5)*3)+frac(i.worldPos.zx))/32;
					u2 = (float2(id.y&31,0+floor(id.y>>5)*3)+frac(i.worldPos.zx))/32;
				}
				else if(_Order==2)
				{
					u1 = (float2((id.x&31)+1,2+floor(id.x>>5)*3)+frac(i.worldPos.xy))/32;
					u2 = (float2(id.y&31,2+floor(id.y>>5)*3)+frac(float2(-i.worldPos.x,i.worldPos.y)))/32;
				}
				
				fixed4 tex1 = tex2D(_PackTex, u1 );
				fixed4 tex2 = tex2D(_PackTex, u2 );
				fixed4 tex3;
				
				tex3.a = min(1,tex1.a+tex2.a);
				if(tex3.a<0.5)
				{
					clip(-1);
					return 0;
				}

				if(facing>0) tex3.rgb = tex1.rgb*(1-tex2.a) + tex2.rgb*tex2.a;
				else tex3.rgb = tex2.rgb*(1-tex1.a) + tex1.rgb*tex1.a;
				
				float timee = 0.49+_InputTime;

				if(facing<0)
				{
					if(_Order==1) tex3.rgb*=tex2D(_LightTex,float2(1.0/12,timee)).rgb;
					if(_Order==0) tex3.rgb*=tex2D(_LightTex,float2(7.0/12,timee)).rgb;
					if(_Order==2) tex3.rgb*=tex2D(_LightTex,float2(11.0/12,timee)).rgb;
				}
				else
				{
					if(_Order==1) tex3.rgb*=tex2D(_LightTex,float2(3.0/12,timee)).rgb;
					if(_Order==0) tex3.rgb*=tex2D(_LightTex,float2(5.0/12,timee)).rgb;
					if(_Order==2) tex3.rgb*=tex2D(_LightTex,float2(9.0/12,timee)).rgb;
				}

				return tex3*1.2;
			}
			ENDCG
		}
	}
}
