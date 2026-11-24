Shader "VRC_MINE/Utility/ClearCustomRenderTexture"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex InitCustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCustomRenderTexture.cginc"

            uint frag(v2f_init_customrendertexture i) : SV_Target
            {
                return 0u;
            }
            ENDHLSL
        }
    }
}
