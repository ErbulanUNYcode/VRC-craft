Shader "VRC_MINE/WorldBiomesGeneratorRiverToSea"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Size ("Size", Int) = 0
        _Orient ("Orient", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            HLSLPROGRAM

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
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Texture2D<uint> _MainTex;
            int _Size;
            int4 _Orient;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                o.uv = v.uv * _Size;

                return o;
            }

            uint frag(v2f i) : SV_Target
            {
                int2 pos = floor(i.uv);
                
                uint result = _MainTex.Load(int3(pos, 0));

                uint o = result>>6;
                if(o==0)
                {
                    result&=63;
                    uint
                    a = _MainTex.Load(int3(pos + _Orient.xy, 0))>>6;
                    if(a!=0) return result+(a<<6);
                    a = _MainTex.Load(int3(pos - _Orient.xy, 0))>>6;
                    if(a!=0) return result+(a<<6);
                    a = _MainTex.Load(int3(pos + _Orient.yx, 0))>>6;
                    if(a!=0) return result+(a<<6);
                    a = _MainTex.Load(int3(pos - _Orient.yx, 0))>>6;
                    if(a!=0) return result+(a<<6);
                }

                return result;
            }

            ENDHLSL
        }
    }
}