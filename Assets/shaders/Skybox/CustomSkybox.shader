Shader "Unlit/CustomSkybox"
{
    Properties
    {
        _Clouds ("Clouds Texture", 2D) = "white" {}
        _StarsXP ("Stars X+ Texture", 2D) = "white" {}
        _StarsXM ("Stars X- Texture", 2D) = "white" {}
        _StarsYP ("Stars Y+ Texture", 2D) = "white" {}
        _StarsYM ("Stars Y- Texture", 2D) = "white" {}
        _StarsZP ("Stars Z+ Texture", 2D) = "white" {}
        _StarsZM ("Stars Z- Texture", 2D) = "white" {}
        _Sun ("Sun Texture", 2D) = "white" {}
        _Moon ("Moon Texture", 2D) = "white" {}
        _TopCol ("Top Color", Color) = (0.0, 0.5, 1.0, 1.0)
        _BottomCol ("Bottom Color", Color) = (0.0745, 0.306, 0.537, 1.0)
        _HorizonCol ("Horizon Color", Color) = (0.788, 0.992, 1.0, 1.0)
        _ReleCol ("Relief Color", Color) = (1.0, 0.0, 0.0, 1.0)
        _SunLightCol ("Sun Light Color", Color) = (1.0, 1.0, 0.0, 1.0)
        _Angle ("Angle", Range(-180, 180)) = 50
        _TimePower ("Time Power", Range(0, 8)) = 0
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

            #include "UnityCG.cginc"
            
            sampler2D _Clouds;
            sampler2D _StarsXP;
            sampler2D _StarsXM;
            sampler2D _StarsYP;
            sampler2D _StarsYM;
            sampler2D _StarsZP;
            sampler2D _StarsZM;
            sampler2D _Sun;
            sampler2D _Moon;
            fixed4 _TopCol;
            fixed4 _BottomCol;
            fixed4 _HorizonCol;
            fixed4 _ReleCol;
            fixed4 _SunLightCol;
            float _Angle;
            float _TimePower;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 viewDir : TEXCOORD0;
            };

            v2f vert (float4 vertex : POSITION)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);

                float4 worldPos = mul(unity_ObjectToWorld, vertex);
                float3 viewDirection = normalize(worldPos.xyz - _WorldSpaceCameraPos.xyz);
                o.viewDir = viewDirection;

                return o;
            }

            float2 rotate(float2 p, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                return float2(
                    c * p.x - s * p.y,
                    s * p.x + c * p.y
                );
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = i.viewDir;

                dir.xy = rotate(dir.xy,radians(_Angle));
                float r = radians(_TimePower*360);
                dir.yz = rotate(dir.yz,r);

                float time = sin(r)*cos(radians(_Angle));

                //top
                float alphaGradient = smoothstep(1-smoothstep(-0.3,0.5,time)*5, 5, dir.z);
                float3 col = _TopCol * alphaGradient * 1.2;
                
                float2 uv;
                // + stars
                if(time<0.3)
                {
                    float3 absDir = abs(dir);
                    float maxAxis = max(max(absDir.x, absDir.y), absDir.z);
                    
                    if (maxAxis == absDir.x) {
                        uv = dir.zy / absDir.x;
                        if (dir.x > 0)
                            col += tex2D(_StarsXP, uv * float2(-0.5,0.5) + 0.5)*max(0,1-alphaGradient*8);
                        else
                            col += tex2D(_StarsXM, uv * 0.5 + 0.5)*max(0,1-alphaGradient*8);
                    }
                    else if (maxAxis == absDir.y) {
                        uv = dir.xz / absDir.y;
                        if (dir.y > 0)
                            col += tex2D(_StarsYP, uv.yx * 0.5 + 0.5)*max(0,1-alphaGradient*8);
                        else
                            col += tex2D(_StarsYM, uv.yx * float2(-0.5,0.5) + 0.5)*max(0,1-alphaGradient*8);
                    }
                    else {
                        uv = dir.xy / absDir.z;
                        if (dir.z > 0)
                            col += tex2D(_StarsZP, uv * 0.5 + 0.5)*max(0,1-alphaGradient*8);
                        else
                            col += tex2D(_StarsZM, uv * float2(-0.5,0.5) + 0.5)*max(0,1-alphaGradient*8);
                    }
                }
                
                // + sun + moon
                col = max(col, 0);
                uv = dir.xy / abs(dir.z);
                
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
                        float day = round(_TimePower+0.25);
                        float4 mo = tex2D(_Moon, (uv * float2(-2.5,2.5) + 0.5 + float2(day%4,floor(day/4)))/float2(4,2));
                        col *= mo.a;
                        col += mo.rgb;
                    }
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
