Shader "Custom/RainbowGradVideo"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineThickness ("Outline Thickness", Range(0.001, 0.01)) = 0.003
        _SilhouetteColor ("Silhouette Color", Color) = (0,0,0,1)
        _RainbowSamples ("Rainbow Samples", Range(3, 12)) = 7
        _RainbowSpread ("Rainbow Spread", Range(0.5, 100.0)) = 1.5
        _RainbowIntensity ("Rainbow Intensity", Range(0.5, 100.0)) = 1.0
        _EdgeSensitivity ("Edge Sensitivity", Range(0.01, 10.0)) = 0.2
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

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
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _OutlineThickness;
            float4 _SilhouetteColor;
            int _RainbowSamples;
            float _RainbowSpread;
            float _RainbowIntensity;
            float _EdgeSensitivity;

            // HSV to RGB conversion for rainbow colors
            float3 HSVtoRGB(float h, float s, float v)
            {
                float c = v * s;
                float x = c * (1.0 - abs(fmod(h * 6.0, 2.0) - 1.0));
                float m = v - c;
                
                float3 rgb;
                if (h < 0.166667) rgb = float3(c, x, 0);
                else if (h < 0.333333) rgb = float3(x, c, 0);
                else if (h < 0.5) rgb = float3(0, c, x);
                else if (h < 0.666667) rgb = float3(0, x, c);
                else if (h < 0.833333) rgb = float3(x, 0, c);
                else rgb = float3(c, 0, x);
                
                return rgb + m;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the center pixel
                float centerAlpha = tex2D(_MainTex, i.uv).a;
                
                // Detect internal edges (between fingers)
                float alphaRight = tex2D(_MainTex, i.uv + float2(_OutlineThickness, 0)).a;
                float alphaLeft = tex2D(_MainTex, i.uv + float2(-_OutlineThickness, 0)).a;
                float alphaUp = tex2D(_MainTex, i.uv + float2(0, _OutlineThickness)).a;
                float alphaDown = tex2D(_MainTex, i.uv + float2(0, -_OutlineThickness)).a;
                float alphaDiag1 = tex2D(_MainTex, i.uv + float2(_OutlineThickness, _OutlineThickness)).a;
                float alphaDiag2 = tex2D(_MainTex, i.uv + float2(-_OutlineThickness, _OutlineThickness)).a;
                float alphaDiag3 = tex2D(_MainTex, i.uv + float2(_OutlineThickness, -_OutlineThickness)).a;
                float alphaDiag4 = tex2D(_MainTex, i.uv + float2(-_OutlineThickness, -_OutlineThickness)).a;
                
                float avgAlpha = (alphaRight + alphaLeft + alphaUp + alphaDown + 
                                 alphaDiag1 + alphaDiag2 + alphaDiag3 + alphaDiag4) / 8.0;
                
                float alphaDifference = abs(centerAlpha - avgAlpha);
                bool isInternalEdge = (alphaDifference > _EdgeSensitivity && centerAlpha > 0.1);
                
                // If opaque and not an internal edge, return black silhouette
                if (centerAlpha > 0.5 && !isInternalEdge)
                {
                    return float4(_SilhouetteColor.rgb, 1.0);
                }
                
                // Rainbow outline effect with fixed unroll
                float3 rainbowColor = float3(0, 0, 0);
                int samples = min(_RainbowSamples, 12);
                
                [unroll(12)]
                for (int j = 0; j < 12; j++)
                {
                    if (j >= samples) break;
                    
                    float t = (float)j / (float)(samples - 1);
                    float angle = t * 6.283185; // 2 * PI for circular distribution
                    float offsetAmount = _OutlineThickness * _RainbowSpread * (1.0 - t * 0.5);
                    
                    float2 offset = float2(cos(angle), sin(angle)) * offsetAmount;
                    
                    // Sample alpha at this offset
                    float sampleAlpha = tex2D(_MainTex, i.uv + offset).a;
                    
                    // Detect edge at this sample
                    float edgeStrength = 0.0;
                    if (centerAlpha < 0.1)
                    {
                        // Outer edge detection (from transparent to opaque)
                        edgeStrength = saturate(sampleAlpha * 2.0);
                    }
                    else
                    {
                        // Internal edge detection
                        float sampleDiff = abs(centerAlpha - sampleAlpha);
                        edgeStrength = saturate(sampleDiff / _EdgeSensitivity);
                    }
                    
                    // Get rainbow color for this sample
                    float hue = t;
                    float3 hsvColor = HSVtoRGB(hue, 1.0, 1.0);
                    
                    // Accumulate rainbow color
                    rainbowColor += hsvColor * edgeStrength;
                }
                
                // Normalize and boost
                rainbowColor /= (float)samples;
                rainbowColor *= _RainbowIntensity;
                
                // If we have rainbow color, return it
                float rainbowAmount = max(max(rainbowColor.r, rainbowColor.g), rainbowColor.b);
                if (rainbowAmount > 0.1)
                {
                    return float4(rainbowColor, 1.0);
                }
                
                // Otherwise transparent
                return float4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
}