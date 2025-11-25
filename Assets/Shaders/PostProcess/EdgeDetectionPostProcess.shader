Shader "Fullscreen/EdgeDetection"
{
    Properties
    {
        [HideInInspector] _BlitTexture ("Blit Texture", 2D) = "white" {}
        
        [Header(Edge Detection)]
        _EdgeThickness ("Edge Thickness", Range(0, 5)) = 1.0
        _DepthSensitivity ("Depth Sensitivity", Float) = 10.0
        _NormalSensitivity ("Normal Sensitivity", Float) = 1.0
        _EdgeColor ("Edge Color", Color) = (1, 1, 1, 1)
        
        [Header(Chromatic Aberration)]
        [Toggle] _UseRainbowGradient ("Use Rainbow Gradient", Float) = 0
        _ChromaticSpread ("Chromatic Spread", Range(0, 100)) = 3.5
        _ChromaticIntensity ("Chromatic Intensity", Range(0, 100)) = 0.8
        _ChromaticDistance ("Chromatic Distance", Float) = 10.0
        _ChromaticFalloff ("Chromatic Falloff", Float) = 5.0
        _RainbowSamples ("Rainbow Samples", Range(3, 12)) = 7
    }
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        
        Pass
        {
            Name "EdgeDetectionPass"
            
            ZTest Always
            ZWrite Off
            Cull Off
            
Stencil
{
    Ref 1
    Comp NotEqual
    ReadMask 1
}

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            
            float _EdgeThickness;
            float _DepthSensitivity;
            float _NormalSensitivity;
            float4 _EdgeColor;
            float _UseRainbowGradient;
            float _ChromaticSpread;
            float _ChromaticIntensity;
            float _ChromaticDistance;
            float _ChromaticFalloff;
            float _RainbowSamples;
            
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
            
            float4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                
                // Sample scene color
                float4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                
                // Get depth and reconstruct world position for distance calculation
                float depth = SampleSceneDepth(uv);
                float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float distanceToCamera = length(_WorldSpaceCameraPos - worldPos);
                
                // Calculate distance factor for chromatic aberration
                float chromaDistanceFactor = saturate(1.0 - (distanceToCamera - _ChromaticDistance) / _ChromaticFalloff);
                chromaDistanceFactor = chromaDistanceFactor * chromaDistanceFactor;
                
                // Calculate texel size with edge thickness multiplier
                float2 texelSize = (_ScreenParams.zw - 1.0) * _EdgeThickness;
                
                // Roberts Cross sampling pattern for main edge detection
                float2 uvOffsets[4] = {
                    uv,
                    uv + float2(texelSize.x, 0),
                    uv + float2(0, texelSize.y),
                    uv + texelSize
                };
                
                // Sample depth and normals for main edge
                float depth0 = SampleSceneDepth(uvOffsets[0]);
                float depth1 = SampleSceneDepth(uvOffsets[1]);
                float depth2 = SampleSceneDepth(uvOffsets[2]);
                float depth3 = SampleSceneDepth(uvOffsets[3]);
                
                float3 normal0 = SampleSceneNormals(uvOffsets[0]);
                float3 normal1 = SampleSceneNormals(uvOffsets[1]);
                float3 normal2 = SampleSceneNormals(uvOffsets[2]);
                float3 normal3 = SampleSceneNormals(uvOffsets[3]);
                
                // Roberts Cross for main edge
                float depthDiff1 = depth1 - depth2;
                float depthDiff2 = depth0 - depth3;
                float depthEdge = sqrt(depthDiff1 * depthDiff1 + depthDiff2 * depthDiff2) * _DepthSensitivity;
                
                float3 normalDiff1 = normal1 - normal2;
                float3 normalDiff2 = normal0 - normal3;
                float normalEdge = sqrt(dot(normalDiff1, normalDiff1) + dot(normalDiff2, normalDiff2)) * _NormalSensitivity;
                
                float edge = saturate(max(depthEdge, normalEdge));
                
                // --- Chromatic Aberration ---
                float3 chromaticColor = float3(0, 0, 0);
                
                if (_UseRainbowGradient > 0.5)
                {
                    // Rainbow gradient mode - sample multiple colors across the spectrum
                    int samples = (int)_RainbowSamples;
                    float chromaOffset = texelSize.x * _ChromaticSpread;
                    
                    for (int i = 0; i < samples; i++)
                    {
                        // Calculate offset for this rainbow band
                        float t = (float)i / (float)(samples - 1);
                        float angleOffset = chromaOffset * (1.0 - t);
                        float angle = t * 6.283185; // 2 * PI for circular distribution
                        
                        float2 offset = float2(cos(angle), sin(angle)) * angleOffset;
                        
                        float2 sampleOffsets[4] = {
                            uv + offset,
                            uv + offset + float2(texelSize.x, 0),
                            uv + offset + float2(0, texelSize.y),
                            uv + offset + texelSize
                        };
                        
                        // Sample edge at this offset
                        float d0 = SampleSceneDepth(sampleOffsets[0]);
                        float d1 = SampleSceneDepth(sampleOffsets[1]);
                        float d2 = SampleSceneDepth(sampleOffsets[2]);
                        float d3 = SampleSceneDepth(sampleOffsets[3]);
                        
                        float3 n0 = SampleSceneNormals(sampleOffsets[0]);
                        float3 n1 = SampleSceneNormals(sampleOffsets[1]);
                        float3 n2 = SampleSceneNormals(sampleOffsets[2]);
                        float3 n3 = SampleSceneNormals(sampleOffsets[3]);
                        
                        float dDiff1 = d1 - d2;
                        float dDiff2 = d0 - d3;
                        float dEdge = sqrt(dDiff1 * dDiff1 + dDiff2 * dDiff2) * _DepthSensitivity;
                        
                        float3 nDiff1 = n1 - n2;
                        float3 nDiff2 = n0 - n3;
                        float nEdge = sqrt(dot(nDiff1, nDiff1) + dot(nDiff2, nDiff2)) * _NormalSensitivity;
                        
                        float sampleEdge = saturate(max(dEdge, nEdge));
                        
                        // Remove main white edge
                        sampleEdge = saturate(sampleEdge - edge);
                        
                        // Apply distance factor
                        sampleEdge *= chromaDistanceFactor * _ChromaticIntensity;
                        
                        // Get rainbow color for this sample (hue from 0 to 1)
                        float hue = t;
                        float3 rainbowColor = HSVtoRGB(hue, 1.0, 1.0);
                        
                        // Accumulate this color band
                        chromaticColor += rainbowColor * sampleEdge;
                    }
                    
                    // Normalize by number of samples
                    chromaticColor /= (float)samples;
                    chromaticColor *= 2.0; // Boost for visibility
                }
                else
                {
                    // Original 3-color RGB mode
                    float chromaOffset = texelSize.x * _ChromaticSpread;
                    
                    // Red channel (furthest from edge)
                    float2 redOffsets[4] = {
                        uv + float2(chromaOffset, 0),
                        uv + float2(chromaOffset + texelSize.x, 0),
                        uv + float2(chromaOffset, texelSize.y),
                        uv + float2(chromaOffset + texelSize.x, texelSize.y)
                    };
                    
                    float redDepth0 = SampleSceneDepth(redOffsets[0]);
                    float redDepth1 = SampleSceneDepth(redOffsets[1]);
                    float redDepth2 = SampleSceneDepth(redOffsets[2]);
                    float redDepth3 = SampleSceneDepth(redOffsets[3]);
                    
                    float3 redNormal0 = SampleSceneNormals(redOffsets[0]);
                    float3 redNormal1 = SampleSceneNormals(redOffsets[1]);
                    float3 redNormal2 = SampleSceneNormals(redOffsets[2]);
                    float3 redNormal3 = SampleSceneNormals(redOffsets[3]);
                    
                    float redDepthDiff1 = redDepth1 - redDepth2;
                    float redDepthDiff2 = redDepth0 - redDepth3;
                    float redDepthEdge = sqrt(redDepthDiff1 * redDepthDiff1 + redDepthDiff2 * redDepthDiff2) * _DepthSensitivity;
                    
                    float3 redNormalDiff1 = redNormal1 - redNormal2;
                    float3 redNormalDiff2 = redNormal0 - redNormal3;
                    float redNormalEdge = sqrt(dot(redNormalDiff1, redNormalDiff1) + dot(redNormalDiff2, redNormalDiff2)) * _NormalSensitivity;
                    
                    float redEdge = saturate(max(redDepthEdge, redNormalEdge));
                    
                    // Green channel (middle distance)
                    float greenChromaOffset = chromaOffset * 0.66;
                    float2 greenOffsets[4] = {
                        uv + float2(greenChromaOffset * 0.5, greenChromaOffset * 0.866),
                        uv + float2(greenChromaOffset * 0.5 + texelSize.x, greenChromaOffset * 0.866),
                        uv + float2(greenChromaOffset * 0.5, greenChromaOffset * 0.866 + texelSize.y),
                        uv + float2(greenChromaOffset * 0.5 + texelSize.x, greenChromaOffset * 0.866 + texelSize.y)
                    };
                    
                    float greenDepth0 = SampleSceneDepth(greenOffsets[0]);
                    float greenDepth1 = SampleSceneDepth(greenOffsets[1]);
                    float greenDepth2 = SampleSceneDepth(greenOffsets[2]);
                    float greenDepth3 = SampleSceneDepth(greenOffsets[3]);
                    
                    float3 greenNormal0 = SampleSceneNormals(greenOffsets[0]);
                    float3 greenNormal1 = SampleSceneNormals(greenOffsets[1]);
                    float3 greenNormal2 = SampleSceneNormals(greenOffsets[2]);
                    float3 greenNormal3 = SampleSceneNormals(greenOffsets[3]);
                    
                    float greenDepthDiff1 = greenDepth1 - greenDepth2;
                    float greenDepthDiff2 = greenDepth0 - greenDepth3;
                    float greenDepthEdge = sqrt(greenDepthDiff1 * greenDepthDiff1 + greenDepthDiff2 * greenDepthDiff2) * _DepthSensitivity;
                    
                    float3 greenNormalDiff1 = greenNormal1 - greenNormal2;
                    float3 greenNormalDiff2 = greenNormal0 - greenNormal3;
                    float greenNormalEdge = sqrt(dot(greenNormalDiff1, greenNormalDiff1) + dot(greenNormalDiff2, greenNormalDiff2)) * _NormalSensitivity;
                    
                    float greenEdge = saturate(max(greenDepthEdge, greenNormalEdge));
                    
                    // Blue channel (closest to white edge)
                    float blueChromaOffset = chromaOffset * 0.33;
                    float2 blueOffsets[4] = {
                        uv + float2(-blueChromaOffset * 0.5, blueChromaOffset * 0.866),
                        uv + float2(-blueChromaOffset * 0.5 + texelSize.x, blueChromaOffset * 0.866),
                        uv + float2(-blueChromaOffset * 0.5, blueChromaOffset * 0.866 + texelSize.y),
                        uv + float2(-blueChromaOffset * 0.5 + texelSize.x, blueChromaOffset * 0.866 + texelSize.y)
                    };
                    
                    float blueDepth0 = SampleSceneDepth(blueOffsets[0]);
                    float blueDepth1 = SampleSceneDepth(blueOffsets[1]);
                    float blueDepth2 = SampleSceneDepth(blueOffsets[2]);
                    float blueDepth3 = SampleSceneDepth(blueOffsets[3]);
                    
                    float3 blueNormal0 = SampleSceneNormals(blueOffsets[0]);
                    float3 blueNormal1 = SampleSceneNormals(blueOffsets[1]);
                    float3 blueNormal2 = SampleSceneNormals(blueOffsets[2]);
                    float3 blueNormal3 = SampleSceneNormals(blueOffsets[3]);
                    
                    float blueDepthDiff1 = blueDepth1 - blueDepth2;
                    float blueDepthDiff2 = blueDepth0 - blueDepth3;
                    float blueDepthEdge = sqrt(blueDepthDiff1 * blueDepthDiff1 + blueDepthDiff2 * blueDepthDiff2) * _DepthSensitivity;
                    
                    float3 blueNormalDiff1 = blueNormal1 - blueNormal2;
                    float3 blueNormalDiff2 = blueNormal0 - blueNormal3;
                    float blueNormalEdge = sqrt(dot(blueNormalDiff1, blueNormalDiff1) + dot(blueNormalDiff2, blueNormalDiff2)) * _NormalSensitivity;
                    
                    float blueEdge = saturate(max(blueDepthEdge, blueNormalEdge));
                    
                    // Apply distance factor and intensity
                    redEdge *= chromaDistanceFactor * _ChromaticIntensity;
                    greenEdge *= chromaDistanceFactor * _ChromaticIntensity;
                    blueEdge *= chromaDistanceFactor * _ChromaticIntensity;
                    
                    // Remove white edge
                    redEdge = saturate(redEdge - edge);
                    greenEdge = saturate(greenEdge - edge);
                    blueEdge = saturate(blueEdge - edge);
                    
                    // Create distinct RGB channels with separation
                    chromaticColor.r = redEdge * (1.0 - greenEdge * 0.5) * (1.0 - blueEdge * 0.7);
                    chromaticColor.g = greenEdge * (1.0 - redEdge * 0.5) * (1.0 - blueEdge * 0.5);
                    chromaticColor.b = blueEdge * (1.0 - redEdge * 0.7) * (1.0 - greenEdge * 0.5);
                    
                    // Boost color channels for visibility
                    chromaticColor.r = saturate(chromaticColor.r * 2.5);
                    chromaticColor.g = saturate(chromaticColor.g * 2.0);
                    chromaticColor.b = saturate(chromaticColor.b * 3.0);
                }
                
                // Start with scene color
                float3 finalColor = sceneColor.rgb;
                
                // Add chromatic aberration
                float chromaAmount = max(max(chromaticColor.r, chromaticColor.g), chromaticColor.b);
                finalColor = lerp(finalColor, chromaticColor, chromaAmount);
                
                // Add white edge on top
                finalColor = lerp(finalColor, _EdgeColor.rgb, edge);
                
                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
