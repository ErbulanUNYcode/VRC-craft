Shader "VRC_MINE/Editor/UintTexView"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OceanColor ("Ocean Color", Color) = (0,0.5,1,1)
        _ContinentColor ("Continent Color", Color) = (0.5,0.5,0.5,1)
        _PolarColor ("Polar Color", Color) = (0.5,0.5,0.5,1)
        _ColdColor ("Cold Color", Color) = (0.5,0.5,0.5,1)
        _TempColor ("Temp Color", Color) = (0.5,0.5,0.5,1)
        _WarmColor ("Warm Color", Color) = (0.5,0.5,0.5,1)
        _TundraColor ("Tundra Color", Color) = (0.5,0.5,0.5,1)
        _SnowForestColor ("Snow Forest Color", Color) = (0.5,0.5,0.5,1)
        _SnowTaigaColor ("Snow Taiga Color", Color) = (0.5,0.5,0.5,1)
        _SnowPlainColor ("Snow Plain Color", Color) = (0.5,0.5,0.5,1)
        _TaigaColor ("Taiga Color", Color) = (0.5,0.5,0.5,1)
        _DarkForestColor ("Dark Forest Color", Color) = (0.5,0.5,0.5,1)
        _SwampColor ("Swamp Color", Color) = (0.5,0.5,0.5,1)
        _DenseForestColor ("Dense Forest Color", Color) = (0.5,0.5,0.5,1)
        _PlainColor ("Plain Color", Color) = (0.5,0.5,0.5,1)
        _ForestColor ("Forest Color", Color) = (0.5,0.5,0.5,1)
        _BrichForestColor ("Brich Forest Color", Color) = (0.5,0.5,0.5,1)
        _SakuraForestColor ("Sakura Forest Color", Color) = (0.5,0.5,0.5,1)
        _DesertColor ("Desert Color", Color) = (0.5,0.5,0.5,1)
        _SavannaColor ("Savanna Color", Color) = (0.5,0.5,0.5,1)
        _JungleColor ("Jungle Color", Color) = (0.5,0.5,0.5,1)
        _WastelandColor ("Wasteland Color", Color) = (0.5,0.5,0.5,1)
        _ShowDetails ("Show Details", Int) = 0
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
            fixed4 _OceanColor;
            fixed4 _ContinentColor;

            //climates
            fixed4 _PolarColor;
            fixed4 _ColdColor;
            fixed4 _TempColor;
            fixed4 _WarmColor;

            //biomes
            //Polar
            fixed4 _TundraColor;
            fixed4 _SnowForestColor;
            fixed4 _SnowTaigaColor;
            fixed4 _SnowPlainColor;
            //Cold
            fixed4 _TaigaColor;
            fixed4 _DarkForestColor;
            fixed4 _SwampColor;
            fixed4 _DenseForestColor;
            //Temp
            fixed4 _PlainColor;
            fixed4 _ForestColor;
            fixed4 _BrichForestColor;
            fixed4 _SakuraForestColor;
            //Warm
            fixed4 _DesertColor;
            fixed4 _SavannaColor;
            fixed4 _JungleColor;
            fixed4 _WastelandColor;

            bool _ShowDetails;

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
                o.uv = v.uv * _MainTex_TexelSize.zw;
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
                uint r = _MainTex.Load(int3(i.uv, 0));

                if(_ShowDetails)
                {
                    uint rc = r;
                    uint rr = _MainTex.Load(int3(i.uv+int2(1,0), 0));
                    uint rl = _MainTex.Load(int3(i.uv+int2(-1,0), 0));
                    uint ru = _MainTex.Load(int3(i.uv+int2(0,1), 0));
                    uint rd = _MainTex.Load(int3(i.uv+int2(0,-1), 0));

                    if (r == 0 && (rr != 0 || rl != 0 || ru != 0 || rd != 0)) r=18;
                    rc>>=6;
                    rr>>=6;
                    rl>>=6;
                    ru>>=6;
                    rd>>=6;
                    if (rc)
                    {
                        if(rr&&rr!=rc) r=0;
                        if(rl&&rl!=rc) r=0;
                        if(ru&&ru!=rc) r=0;
                        if(rd&&rd!=rc) r=0;
                    }
                }

                switch (r&63)
                {
                    case 0: return _OceanColor;
                    case 1: return _ContinentColor;
                    case 2: return _PolarColor;
                    case 3: return _ColdColor;
                    case 4: return _TempColor;
                    case 5: return _WarmColor;
                    case 6: return _TundraColor;
                    case 7: return _SnowForestColor;
                    case 8: return _SnowTaigaColor;
                    case 9: return _SnowPlainColor;
                    case 10: return _TaigaColor;
                    case 11: return _DarkForestColor;
                    case 12: return _SwampColor;
                    case 13: return _DenseForestColor;
                    case 14: return _PlainColor;
                    case 15: return _ForestColor;
                    case 16: return _BrichForestColor;
                    case 17: return _SakuraForestColor;
                    case 18: return _DesertColor;
                    case 19: return _SavannaColor;
                    case 20: return _JungleColor;
                    case 21: return _WastelandColor;
                }

                return 0;
            }

            ENDCG
        }
    }
}