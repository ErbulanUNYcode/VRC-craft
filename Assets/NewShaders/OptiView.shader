Shader "VRC_MINE/Editor/UintTexView"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ShowDetails ("Show Details", Int) = 0
        [IntRange] _BiomeIndex ("Debug Biome Index (-1: all)", Range(-1,21)) = -1
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

            Texture2D<uint> _MainTex;
            float4 _MainTex_TexelSize;

            //base
            bool _ShowDetails;
            int _BiomeIndex;
            static const fixed4 biomeColors[22] =
            {
                fixed4(0.0, 0.5, 1.0, 1.0), // 0 Ocean
                fixed4(0.39716092, 0.41731593, 0.7075472, 1.0), // 1 Tundra
                fixed4(0.6084906, 1.0, 0.6358052, 1.0), // 2 SnowForest
                fixed4(0.5036935, 0.7264151, 0.6618002, 1.0), // 3 SnowTaiga
                fixed4(1.0, 1.0, 1.0, 1.0), // 4 SnowPlain
                fixed4(0.08957815, 0.5754717, 0.32994238, 1.0), // 5 Taiga
                fixed4(0.0, 0.31132078, 0.008894861, 1.0), // 6 DarkForest
                fixed4(0.26650554, 0.3301887, 0.1759968, 1.0), // 7 Swamp
                fixed4(0.1852401, 0.41509432, 0.0763617, 1.0), // 8 DenseForest
                fixed4(0.0, 0.6603774, 0.0, 1.0), // 9 Plain
                fixed4(0.4745098, 0.88235295, 0.0, 1.0), // 10 Forest
                fixed4(0.0, 0.8113208, 0.5024658, 1.0), // 11 BirchForest
                fixed4(1.0, 0.40392157, 0.5803922, 1.0), // 12 SakuraForest
                fixed4(0.9433962, 0.92364377, 0.3693485, 1.0), // 13 Desert
                fixed4(1.0, 0.55655974, 0.0, 1.0), // 14 Savanna
                fixed4(0.0, 1.0, 0.0, 1.0), // 15 Jungle
                fixed4(1.0, 0.38475227, 0.0, 1.0), // 16 Wasteland
                fixed4(0.0, 0.0, 1.0, 1.0), // 17 Polar
                fixed4(0.53333336, 0.0, 0.9019608, 1.0), // 18 Cold
                fixed4(0.62352943, 0.0, 0.29803923, 1.0), // 19 Temp
                fixed4(1.0, 0.0, 0.0, 1.0), // 20 Warm
                fixed4(0.5, 0.5, 0.5, 1.0) // 21 Continent
            };

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

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * _MainTex_TexelSize.z;
                return o;
            }

            float hash21(int2 p)
			{
				int n = p.x * 374761393 + p.y * 668265263;
				n = (n ^ (n >> 13)) * 1274126177;
				return frac(n * 0.00000000023283064365386963);
			}

            fixed4 frag(v2f i) : SV_Target
            {
                if(dot(i.uv-_MainTex_TexelSize.z/2,i.uv-_MainTex_TexelSize.z/2)<_MainTex_TexelSize.z*_MainTex_TexelSize.z/400000) return fixed4(1,0,0,1);

                uint r = _MainTex.Load(int3(i.uv, 0));

                if(_ShowDetails)
                {

                    uint rc = r;
                    uint rr = _MainTex.Load(int3(i.uv+int2(1,0), 0));
                    uint rl = _MainTex.Load(int3(i.uv+int2(-1,0), 0));
                    uint ru = _MainTex.Load(int3(i.uv+int2(0,1), 0));
                    uint rd = _MainTex.Load(int3(i.uv+int2(0,-1), 0));
                    
                    if((r&63)<2)
                    {
                        rc=(rc&63)>1;
                        rr=(rr&63)>1;
                        rl=(rl&63)>1;
                        ru=(ru&63)>1;
                        rd=(rd&63)>1;
                        if(rc+rr+rl+ru+rd>0) return biomeColors[13];
                        else
                        {
                            bool debugOceanChecker =
                                _BiomeIndex == 0 &&
                                ((((int)i.uv.x >> 3) + ((int)i.uv.y >> 3)) & 1) != 0;
                            bool debugTundraChecker =
                                _BiomeIndex == 1 &&
                                ((((int)i.uv.x >> 3) + ((int)i.uv.y >> 3)) & 1) != 0;
                            return (r&63)==0
                                ? biomeColors[0] * (debugOceanChecker ? 0.72 : 1.0)
                                : biomeColors[1] * (debugTundraChecker ? 0.72 : 1.0);
                        }
                    }

                    {
                        rc>>=6;
                        rr>>=6;
                        rl>>=6;
                        ru>>=6;
                        rd>>=6;

                        if(rc!=0)
                        {
                            if(rr!=rc) r=0;
                            if(rl!=rc) r=0;
                            if(ru!=rc) r=0;
                            if(rd!=rc) r=0;
                        }
                    }
                }

                uint biome = r & 31;
                fixed4 result = biomeColors[biome];

                if (biome == 17 && _ShowDetails)
                    result = fixed4(biomeColors[16].rgb / 2, 1);

                float shoreline = (r & 63) >> 5 ? 0.5 : 1;
                result *= shoreline;
                bool debugChecker =
                    _BiomeIndex >= 0 &&
                    biome == (uint)_BiomeIndex &&
                    (_BiomeIndex != 0 || (r >> 6) == 0) &&
                    ((((int)i.uv.x >> 3) + ((int)i.uv.y >> 3)) & 1) != 0;
                result *= debugChecker ? 0.72 : 1.0;
                return result;
            }

            ENDCG
        }
    }
}
