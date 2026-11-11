Shader "VRC_MINE/Lod2Gen"
{
    Properties { _PrevLodTex("World Texture",3D)="black"{} [IntRange] _OffsetX("Offset X",Range(0,63))=0 [IntRange] _OffsetY("Offset Y",Range(0,63))=0 }
    SubShader { Tags{"RenderType"="Opaque"} Pass {
        HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"
        #include "UnityCustomRenderTexture.cginc"
        Texture3D<uint> _PrevLodTex;
        int _OffsetX; int _OffsetY;
        struct V2F { float4 vertex:SV_POSITION; float3 localTexcoord:TEXCOORD0; float3 globalTexcoord:TEXCOORD1; nointerpolation uint primitiveID:TEXCOORD2; };
        V2F vert(appdata_customrendertexture v)
        {
            v2f_customrendertexture b=CustomRenderTextureVertexShader(v);
            V2F o; o.vertex=b.vertex; o.localTexcoord=b.localTexcoord; o.globalTexcoord=b.globalTexcoord; o.primitiveID=b.primitiveID;
            if(o.primitiveID==0)
            {
                float2 s=float2(_CustomRenderTextureWidth,_CustomRenderTextureHeight);
                float2 clipOffset=float2(_OffsetX,_OffsetY)*(2.0/s);
                #if !UNITY_UV_STARTS_AT_TOP
                clipOffset.y=-clipOffset.y;
                #endif
                o.vertex.xy+=clipOffset;
                o.globalTexcoord.xy=o.vertex.xy*0.5+0.5;
                #if UNITY_UV_STARTS_AT_TOP
                o.globalTexcoord.y=1.0-o.globalTexcoord.y;
                #endif
            }
            return o;
        }
        bool Block(int3 p){return _PrevLodTex.Load(int4(p,0))>0u;}
        uint frag(V2F i):SV_Target
        {
            if(i.primitiveID!=0)return 0u;
            int3 base=(int3)floor(saturate(i.globalTexcoord)*float3(64.0,64.0,16.0));
            bool occupied=false; int3 source=base*4;
            [unroll]for(int z=0;z<4;z++) [unroll]for(int y=0;y<4;y++) [unroll]for(int x=0;x<4;x++) occupied=occupied||Block(source+int3(x,y,z));
            return occupied ? 1u : 0u;
        }
        ENDHLSL
    } }
}
