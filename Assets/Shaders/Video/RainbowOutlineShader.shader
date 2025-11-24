Shader "Custom/RainbowOutlineVideo"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineThickness ("Outline Thickness", Range(0.001, 0.01)) = 0.003
        _SilhouetteColor ("Silhouette Color", Color) = (0,0,0,1)
        _WhiteOutlineColor ("White Outline Color", Color) = (1,1,1,1)
        _RainbowSamples ("Rainbow Samples", Range(3, 12)) = 7
        _RainbowSpread ("Rainbow Spread", Range(0.5, 50.0)) = 2.0
        _RainbowIntensity ("Rainbow Intensity", Range(0.5, 10.0)) = 1.0
        _EdgeSensitivity ("Edge Sensitivity", Range(0.01, 1.0)) = 0.2
        _LineSharpness ("Line Sharpness", Range(0.1, 5.0)) = 2.0
        
        [Header(Dithering)]
        [Toggle] _UseDithering ("Use Dithering", Float) = 1
        _DitherStrength ("Dither Strength", Range(0, 1)) = 0.5
        _DitherScale ("Dither Scale", Range(1, 20)) = 8
        
        [Header(Color Adjustment)]
        _Desaturation ("Desaturation", Range(0, 1)) = 0
        _Contrast ("Contrast", Range(0, 3)) = 1
        _Brightness ("Brightness", Range(-1, 1)) = 0
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
                float4 screenPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _OutlineThickness;
            float4 _SilhouetteColor;
            float4 _WhiteOutlineColor;
            int _RainbowSamples;
            float _RainbowSpread;
            float _RainbowIntensity;
            float _EdgeSensitivity;
            float _LineSharpness;
            float _UseDithering;
            float _DitherStrength;
            float _DitherScale;
            float _Desaturation;
            float _Contrast;
            float _Brightness;

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

            // Apply color adjustments to texture
            float3 AdjustColor(float3 color)
            {
                // Apply brightness
                color += _Brightness;
                
                // Apply contrast (around middle gray)
                color = ((color - 0.5) * _Contrast) + 0.5;
                
                // Clamp to valid range
                color = saturate(color);
                
                // Apply desaturation
                float gray = dot(color, float3(0.299, 0.587, 0.114));
                color = lerp(color, float3(gray, gray, gray), _Desaturation);
                
                return color;
            }

            // Bayer matrix 4x4 dithering
            float BayerDither4x4(float2 screenPos, float brightness)
            {
                int x = int(fmod(screenPos.x, 4.0));
                int y = int(fmod(screenPos.y, 4.0));
                
                // 4x4 Bayer matrix
                float bayerMatrix[16] = {
                    0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
                    12.0/16.0, 4.0/16.0, 14.0/16.0,  6.0/16.0,
                    3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
                    15.0/16.0, 7.0/16.0, 13.0/16.0,  5.0/16.0
                };
                
                float threshold = bayerMatrix[y * 4 + x];
                return brightness > threshold ? 1.0 : 0.0;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the center pixel
                float4 originalColor = tex2D(_MainTex, i.uv);
                float centerAlpha = originalColor.a;
                
                // Apply color adjustments to original texture
                float3 adjustedColor = AdjustColor(originalColor.rgb);
                
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
                
                // If opaque and not an internal edge, render silhouette with optional dithering
                if (centerAlpha > 0.5 && !isInternalEdge)
                {
                    if (_UseDithering > 0.5)
                    {
                        // Calculate brightness from adjusted texture
                        float brightness = dot(adjustedColor, float3(0.299, 0.587, 0.114));
                        
                        // Get screen position for dithering
                        float2 screenPos = i.screenPos.xy / i.screenPos.w * _ScreenParams.xy;
                        screenPos *= _DitherScale;
                        
                        // Apply dithering
                        float dither = BayerDither4x4(screenPos, brightness);
                        
                        // Blend between solid black and dithered based on strength
                        // Use adjusted color for the dithered portions
                        float mixFactor = lerp(0.0, dither, _DitherStrength);
                        float3 finalColor = lerp(_SilhouetteColor.rgb, adjustedColor * 0.5, mixFactor);
                        
                        return float4(finalColor, 1.0);
                    }
                    else
                    {
                        return float4(_SilhouetteColor.rgb, 1.0);
                    }
                }
                
                // --- WHITE BASE OUTLINE ---
                float maxAlpha = max(max(max(alphaRight, alphaLeft), max(alphaUp, alphaDown)),
                                    max(max(alphaDiag1, alphaDiag2), max(alphaDiag3, alphaDiag4)));
                
                float baseEdge = 0.0;
                if (centerAlpha < 0.1)
                {
                    baseEdge = saturate(maxAlpha * 3.0);
                }
                else if (isInternalEdge)
                {
                    baseEdge = saturate(alphaDifference / _EdgeSensitivity * 2.0);
                }
                
                if (baseEdge > 0.5)
                {
                    return _WhiteOutlineColor;
                }
                
                // --- RAINBOW CHROMATIC LINES ---
                float3 bestColor = float3(0, 0, 0);
                float bestStrength = 0.0;
                int samples = min(_RainbowSamples, 12);
                
                [unroll(12)]
                for (int j = 0; j < 12; j++)
                {
                    if (j >= samples) break;
                    
                    float t = (float)j / (float)(samples - 1);
                    float angle = t * 6.283185;
                    
                    float lineOffset = _OutlineThickness * (1.5 + t * _RainbowSpread);
                    float2 offset = float2(cos(angle), sin(angle)) * lineOffset;
                    
                    float sampleAlpha = tex2D(_MainTex, i.uv + offset).a;
                    
                    float prevOffset = lineOffset - _OutlineThickness * 0.5;
                    float nextOffset = lineOffset + _OutlineThickness * 0.5;
                    float2 prevPos = float2(cos(angle), sin(angle)) * prevOffset;
                    float2 nextPos = float2(cos(angle), sin(angle)) * nextOffset;
                    
                    float prevAlpha = tex2D(_MainTex, i.uv + prevPos).a;
                    float nextAlpha = tex2D(_MainTex, i.uv + nextPos).a;
                    
                    float edgeStrength = 0.0;
                    
                    if (centerAlpha < 0.1)
                    {
                        float gradient = abs(nextAlpha - prevAlpha);
                        edgeStrength = saturate(gradient * _LineSharpness) * saturate(sampleAlpha * 3.0);
                    }
                    else
                    {
                        float centerDiff = abs(sampleAlpha - centerAlpha);
                        float gradient = abs(nextAlpha - prevAlpha);
                        edgeStrength = saturate(gradient * _LineSharpness) * saturate(centerDiff / _EdgeSensitivity);
                    }
                    
                    if (edgeStrength > bestStrength)
                    {
                        bestStrength = edgeStrength;
                        float hue = t;
                        bestColor = HSVtoRGB(hue, 1.0, 1.0) * edgeStrength;
                    }
                }
                
                bestColor *= _RainbowIntensity;
                
                float colorAmount = max(max(bestColor.r, bestColor.g), bestColor.b);
                if (colorAmount > 0.1)
                {
                    return float4(bestColor, 1.0);
                }
                
                return float4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
}