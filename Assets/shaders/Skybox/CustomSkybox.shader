Shader "Unlit/CustomSkybox"
{
    Properties
    {
        _Clouds ("Clouds Texture", 2D) = "white" {}
        _StarsCube ("Stars Cubemap", Cube) = "" {}
        _Sun ("Sun Texture", 2D) = "white" {}
        _Moon ("Moon Phases", 2DArray) = "" {}
        _TopCol ("Top Color", Color) = (0.0, 0.5, 1.0, 1.0)
        _BottomCol ("Bottom Color", Color) = (0.0745, 0.306, 0.537, 1.0)
        _HorizonCol ("Horizon Color", Color) = (0.788, 0.992, 1.0, 1.0)
        _ReleCol ("Relief Color", Color) = (1.0, 0.0, 0.0, 1.0)
        _SunLightCol ("Sun Light Color", Color) = (1.0, 1.0, 0.0, 1.0)
        _Angle ("Angle", Range(-180, 180)) = 50
        _TimePower ("Time Power", Range(0, 8)) = 0
        [IntRange] _MoonDay ("Moon Day", Range(0, 7)) = 0
        [HideInInspector] _MoonRaysDirection ("Moon Rays Direction", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma require 2darray

            #include "UnityCG.cginc"
            
            sampler2D _Clouds;
            samplerCUBE _StarsCube;
            sampler2D _Sun;
            UNITY_DECLARE_TEX2DARRAY(_Moon);
            fixed4 _TopCol;
            fixed4 _BottomCol;
            fixed4 _HorizonCol;
            fixed4 _ReleCol;
            fixed4 _SunLightCol;
            float _Angle;
            float _TimePower;
            float _MoonDay;
            float3 _MoonRaysDirection;
            float2 rotate(float2 p, float angle)
            {
                float s;
                float c;
                sincos(angle, s, c);
                return float2(
                    c * p.x - s * p.y,
                    s * p.x + c * p.y
                );
            }
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 viewDir : TEXCOORD0;
                float3 viewDirRot : TEXCOORD1;
                nointerpolation float time : TEXCOORD2;
                nointerpolation float r : TEXCOORD3;
            };
            
            

            v2f vert (float4 vertex : POSITION)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);

                float4 worldPos = mul(unity_ObjectToWorld, vertex);
                float3 viewDirection = normalize(worldPos.xyz - _WorldSpaceCameraPos.xyz);
                o.viewDir = viewDirection;
                o.viewDirRot = viewDirection;
                o.viewDirRot.xy = rotate(o.viewDirRot.xy,radians(_Angle));
                o.r = radians(_TimePower*360);
                o.viewDirRot.yz = rotate(o.viewDirRot.yz,o.r);
                o.time = sin(o.r)*cos(radians(_Angle));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = i.viewDirRot;
                float r = i.r;
                float time = i.time;

                //top
                float alphaGradient = smoothstep(1-smoothstep(-0.3,0.5,time)*5, 5, dir.z);
                float3 col = _TopCol * alphaGradient * 1.2;
                
                if (time < 0.3)
                {
                    float3 stars = max(0.0, texCUBE(_StarsCube, dir).rgb - 0.008);
                    col += stars * max(0, 1 - alphaGradient * 8);
                }

                // + sun + moon
                col = max(col, 0);
                float2 uv = dir.xy / abs(dir.z);
                
                float lightChanel=1;
                if (abs(uv.x)<0.2 && abs(uv.y)<0.2)
                {
                    if (dir.z>0)
                    {
                        float2 sunUV = uv * 2.5 + 0.5;
                        col += tex2D(_Sun, sunUV);
                        lightChanel=length(sunUV-0.5)*2.3;
                    }
                    else
                    {
                        // Unity imports the top row first, but phase zero is on the bottom row.
                        float layer = fmod(round(_MoonDay) + 4.0, 8.0);
                        float2 moonUV = uv * float2(-2.5, 2.5) + 0.5;
                        float4 mo = UNITY_SAMPLE_TEX2DARRAY(_Moon, float3(moonUV, layer));
                        col *= mo.a;
                        col += mo.rgb * (1.0 - mo.a);
                        lightChanel = length(uv * 2.5) * 2.3;
                    }
                }

                if (dir.z < 0 && dot(_MoonRaysDirection, _MoonRaysDirection) > 0.5)
                {
                    float3 viewDirection = normalize(i.viewDir);
                    float forward = dot(viewDirection, _MoonRaysDirection);
                    float distanceFromCenter = length(cross(viewDirection, _MoonRaysDirection));
                    lightChanel = forward > 0 ? saturate(5.75 * distanceFromCenter / max(forward, 0.0001)) : 1.0;
                }

                // + sun
                col += _SunLightCol * smoothstep(1-smoothstep(-0.2,1,time)*0.45,1.2,dir.z);
                
                dir = i.viewDir;

                // + reflection
                col += max(0,pow(alphaGradient,2) * smoothstep(0.3,-0.05,time) * _ReleCol * 3 - dir.y/3);

                // + horizon
                alphaGradient = smoothstep(0.15,-0.15,dir.y) * alphaGradient;
                col += alphaGradient * _HorizonCol;// + (1-alphaGradient) * col;
                
                // + reflection
                col += alphaGradient * smoothstep(0.1,-0.1,time) * 5 * _ReleCol;
                
                // - bottom
                alphaGradient = smoothstep(-0.05,0,dir.y);
                lightChanel+=1-alphaGradient;
                col = alphaGradient * col + (1-alphaGradient) * _BottomCol * smoothstep(-0.1,0.5,time);
                
                // + clouds
                dir.y += pow(length(dir.xz), 2) /8;
                for(float j = 0; j < 20; j ++)
                {
                    dir.y+=0.004*dir.y;
                    if(dir.y<0.1) continue;
                    float c = tex2D(_Clouds,(dir.xz/dir.y/64)-0.5+float2(_TimePower/2,_TimePower/3)).r;
                    if(c<0.01) continue;
                    c = smoothstep(c,0.01,0.02)*(dir.y-0.1);
                    col *= 1-c;
                    col += c * (1-j/32) * (0.02+smoothstep(-0.1,0.4,time));
                    lightChanel+=c;
                }
                lightChanel=min(lightChanel,1);
                return fixed4(col.rgb, lightChanel);
            }
            ENDCG
        }
    }
    FallBack Off
}
