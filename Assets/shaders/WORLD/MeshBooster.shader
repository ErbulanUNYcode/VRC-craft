Shader "Unlit/MeshBooster"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_WorldTex ("World", 2D) = "white" {}
		_ControllerTex ("Controller", 2D) = "white" {}
		_OffsetX ("OffsetX", int) = 0
		_OffsetZ ("OffsetZ", int) = 0
    }
    SubShader
    {
		Blend One Zero
		ZWrite Off
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

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
			
            Texture2D _MainTex;
            Texture2D _WorldTex;
			Texture2D _ControllerTex;
			int _OffsetX;
			int _OffsetZ;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            bool isDrawable(int4 p)
			{
				p.xz = (int2(p.x, p.z) + 8192) % 256;

				float2 ids = 0;

				if (p.y == 128) ids.x = 0;
				else
				{
					int2 coordXZ = int2(p.x, p.z);
					int h = round(_WorldTex.Load(int3(coordXZ, 0)).b * 255);

					if (h != 0)
					{
						int2 uv = coordXZ + int2(p.y % 16, p.y / 16)*256;

						int2 c = round(_WorldTex.Load(int3(uv, 0)).rg * 255);

						if (c.x != 0)
							ids.x = c.x - 1;
						else if (p.y == 0)
							ids.x = 1;
						else if (p.y < h)
							ids.x = 2;
						else
						{
							coordXZ.x += 256;
							h = round(_WorldTex.Load(int3(coordXZ, 0)).b * 255);

							if(p.y> h-1) ids.x = 0;
							else if(p.y>h-2) ids.x = 4;
							else if(p.y>h-4) ids.x = 3;
							else if(p.y>0) ids.x = 2;
							else ids.x = 1;
						}
					}
				}

				if (p.w == 0) p.x--;
				else if (p.w == 1) p.y--;
				else if (p.w == 2) p.z--;

				p.xz = (int2(p.x, p.z) + 8192) % 256;

				if (p.y == -1) ids.y = 0;
				else
				{
					int2 coordXZ = int2(p.x, p.z);
					int h = round(_WorldTex.Load(int3(coordXZ, 0)).b * 255);

					if (h != 0)
					{
						int2 uv = coordXZ + int2(p.y % 16, p.y / 16)*256;

						int2 c = round(_WorldTex.Load(int3(uv, 0)).rg * 255);

						if (c.x != 0)
							ids.y = c.x - 1;
						else if (p.y == 0)
							ids.y = 1;
						else if (p.y < h)
							ids.y = 2;
						else
						{
							coordXZ.x += 256;
							h = round(_WorldTex.Load(int3(coordXZ, 0)).b * 255);

							if(p.y> h-1) ids.y = 0;
							else if(p.y>h-2) ids.y = 4;
							else if(p.y>h-4) ids.y = 3;
							else if(p.y>0) ids.y = 2;
							else ids.y = 1;
						}
					}
				}

				return ids.x!=ids.y; 
			}

            fixed4 frag (v2f i) : SV_Target
            {
				int2 p = int2(i.uv * float2(193,84));
				fixed4 ctrl = _ControllerTex.Load(int3(p.x, p.y,0));
				if (ctrl.r==0) return fixed4(16,16,0,0)/255;
                float4 col = _MainTex.Load(int3(p,0));
				if (ctrl.g==0) return col;
				//if(col.a==0) return fixed4(16,16,0,0)/255;
				//if(col.a<60.0/255) return col;

				if(p.y<36)//y
				{
					if(p.x>=129) return col;
					
					float id = p.y;
					int3 pos = int3(id%6,p.x,floor(id/6));
					int2 val = floor(float2(_OffsetX,_OffsetZ)/6)*6;
					pos.x += val.x + (_OffsetX-val.x>pos.x?6:0);
					pos.z += val.y + (_OffsetZ-val.y>pos.z?6:0);
					pos.x *=32;
					pos.z *=32;
					pos.x -=96;
					pos.z -=96;

					fixed4 minMaxes = fixed4(-1,0,0,0);

					for(int j=0;j<32;j++)
					{
						for(int k=0;k<32;k++)
						{
							if(isDrawable(int4(pos.x+j,pos.y,pos.z+k,1)))
							{
								if(minMaxes.x==-1)
								{
									minMaxes.x = j;
									minMaxes.y = 31-j;
									minMaxes.z = k;
									minMaxes.w = 31-k;
								}
								else
								{
									minMaxes.y = 31-j;
									minMaxes.z = min(minMaxes.z,k);
									minMaxes.w = min(minMaxes.w,31-k);
								}
							}
						}
					}
					if(minMaxes.x==-1) minMaxes = fixed4(16,16,0,0);
					col = minMaxes / 255;
				}
				else if(p.y<60)//x
				{
					float id = p.y-36;
					int3 pos = int3(p.x,floor(id/6),id%6);


					float val = floor(_OffsetZ/6)*6;
					pos.z += val + (_OffsetZ-val>pos.z?6:0);

					pos.z *=32;
					pos.z -=96;
					pos.y *=32;
					pos.x -=96;

					fixed4 minMaxes = fixed4(-1,0,0,0);

					for(int j=0;j<32;j++)
					{
						for(int k=0;k<32;k++)
						{
							if(isDrawable(int4(pos.x,pos.y+k,pos.z+j,0)))
							{
								if(minMaxes.x==-1)
								{
									minMaxes.x = j;
									minMaxes.y = 31-j;
									minMaxes.z = k;
									minMaxes.w = 31-k;
								}
								else
								{
									minMaxes.y = 31-j;
									minMaxes.z = min(minMaxes.z,k);
									minMaxes.w = min(minMaxes.w,31-k);
								}
							}
						}
					}

					if(minMaxes.x==-1) minMaxes = fixed4(16,16,0,0);
					col = minMaxes / 255;
				}
				else//z
				{
					float id = p.y-60;
					int3 pos = int3(id%6,floor(id/6),p.x);

					float val = floor(_OffsetX/6)*6;
					pos.x += val + (_OffsetX-val>pos.x?6:0);
					pos.x *=32;
					pos.x -=96;
					pos.y *=32;
					pos.z -=96;

					fixed4 minMaxes = fixed4(-1,0,0,0);

					for(int j=0;j<32;j++)
					{
						for(int k=0;k<32;k++)
						{
							if(isDrawable(int4(pos.x+j,pos.y+k,pos.z,2)))
							{
								if(minMaxes.x==-1)
								{
									minMaxes.x = j;
									minMaxes.y = 31-j;
									minMaxes.z = k;
									minMaxes.w = 31-k;
								}
								else
								{
									minMaxes.y = 31-j;
									minMaxes.z = min(minMaxes.z,k);
									minMaxes.w = min(minMaxes.w,31-k);
								}
							}
						}
					}

					if(minMaxes.x==-1) minMaxes = fixed4(16,16,0,0);
					col = minMaxes / 255;
				}
				
                return col;
            }
            ENDCG
        }
    }
}