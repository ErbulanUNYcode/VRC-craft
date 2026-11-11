Shader "VRC_MINE/Editor/LodView3D"
{
    Properties
    {
        _MainTex ("Texture", 3D) = "black" {}
        [IntRange] _Slice ("Slice", Range(0, 255)) = 0
        _Size ("Size", Float) = 256
        _Depth ("Depth", Float) = 64
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

            Texture3D<uint> _MainTex;
            int _Slice;
            float _Size;
            float _Depth;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o;
            }

            fixed4 frag(v2f i):SV_Target
            {
                int2 p=clamp((int2)floor(saturate(i.uv)*_Size),0,max(0,(int)_Size-1));
                int z=clamp(_Slice,0,max(0,(int)_Depth-1));
                return _MainTex.Load(int4(p.x,p.y,z,0))>0u ? 1.0 : 0.0;
            }
            ENDHLSL
        }
    }
}
