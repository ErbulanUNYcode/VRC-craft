Shader "VRC_MINE/Lod3Gen"
{
    Properties { _PrevLodTex("World Texture",3D)="black"{} }
    SubShader { Tags{"RenderType"="Opaque"} Pass {
        HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"
        #include "UnityCustomRenderTexture.cginc"
        Texture3D<uint> _PrevLodTex;
        struct V2F { float4 vertex:SV_POSITION; float3 localTexcoord:TEXCOORD0; float3 globalTexcoord:TEXCOORD1; nointerpolation uint primitiveID:TEXCOORD2; };
        V2F vert(appdata_customrendertexture v){ v2f_customrendertexture b=CustomRenderTextureVertexShader(v); V2F o; o.vertex=b.vertex; o.localTexcoord=b.localTexcoord; o.globalTexcoord=b.globalTexcoord; o.primitiveID=b.primitiveID; return o; }
        bool Block(int3 p){return _PrevLodTex.Load(int4(p,0))>0u;}
        uint frag(V2F i):SV_Target
        {
            if(i.primitiveID!=0)return 0u;
            int3 base=(int3)floor(saturate(i.globalTexcoord)*float3(16.0,16.0,4.0));
            bool occupied=false; int3 source=base*4;
            [unroll]for(int z=0;z<4;z++) [unroll]for(int y=0;y<4;y++) [unroll]for(int x=0;x<4;x++) occupied=occupied||Block(source+int3(x,y,z));
            return occupied ? 1u : 0u;
        }
        ENDHLSL
    } }
}
