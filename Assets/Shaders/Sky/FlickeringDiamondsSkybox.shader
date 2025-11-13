Shader "Skybox/FlickeringDiamonds"
{
    Properties
    {
        [Header(Stars)]
        _StarDensity ("Star Density", Range(0, 1)) = 0.015
        _StarSize ("Star Size", Range(0.001, 2)) = 0.01
        _StarBrightness ("Star Brightness", Range(0, 2)) = 1.0
        
        [Header(Flicker)]
        _FlickerSpeed ("Flicker Speed", Range(0, 10)) = 3.0
        _FlickerAmount ("Flicker Amount", Range(0, 1)) = 0.5
        _FlickerVariation ("Flicker Variation", Range(0, 1)) = 0.8
        
        [Header(Colors)]
        _BackgroundColor ("Background Color", Color) = (0, 0, 0, 1)
        _StarColor ("Star Color", Color) = (1, 1, 1, 1)
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
        }
        
        Cull Off
        ZWrite Off
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 texcoord : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float _StarDensity;
                float _StarSize;
                float _StarBrightness;
                float _FlickerSpeed;
                float _FlickerAmount;
                float _FlickerVariation;
                float4 _BackgroundColor;
                float4 _StarColor;
            CBUFFER_END
            
            // Hash function for pseudo-random values
            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }
            
            // Hash function for 3D input
            float hash3(float3 p)
            {
                p = frac(p * float3(443.897, 441.423, 437.195));
                p += dot(p, p.yzx + 19.19);
                return frac((p.x + p.y) * p.z);
            }
            
            // Voronoi-like star field
            float3 generateStars(float3 dir, out float starMask)
            {
                // Scale direction to create grid
                float3 scaledDir = dir * 50.0;
                float3 gridPos = floor(scaledDir);
                float3 localPos = frac(scaledDir);
                
                starMask = 0.0;
                float closestDist = 10.0;
                float3 starPos = float3(0, 0, 0);
                float starHash = 0.0;
                
                // Check neighboring cells
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            float3 neighborGrid = gridPos + float3(x, y, z);
                            
                            // Generate random position within cell
                            float hashValue = hash3(neighborGrid);
                            
                            // Only create stars in some cells based on density
                            if (hashValue < _StarDensity)
                            {
                                // Random position within cell
                                float3 randomOffset = float3(
                                    hash3(neighborGrid + float3(1.1, 2.2, 3.3)),
                                    hash3(neighborGrid + float3(4.4, 5.5, 6.6)),
                                    hash3(neighborGrid + float3(7.7, 8.8, 9.9))
                                );
                                
                                float3 cellStarPos = float3(x, y, z) + randomOffset;
                                float dist = length(localPos - cellStarPos);
                                
                                if (dist < closestDist)
                                {
                                    closestDist = dist;
                                    starPos = cellStarPos;
                                    starHash = hashValue;
                                }
                            }
                        }
                    }
                }
                
                // Create star if close enough
                if (closestDist < _StarSize)
                {
                    // Diamond shape using distance
                    float starIntensity = 1.0 - (closestDist / _StarSize);
                    starIntensity = pow(starIntensity, 2.0);
                    
                    // Flicker calculation
                    float flickerTime = _Time.y * _FlickerSpeed;
                    float uniqueFlicker = hash(starPos.xy + starPos.z);
                    float flickerPhase = flickerTime + uniqueFlicker * 10.0 * _FlickerVariation;
                    
                    // Multiple sine waves for complex flicker
                    float flicker = sin(flickerPhase) * 0.5 + 0.5;
                    flicker *= sin(flickerPhase * 2.3 + 1.2) * 0.25 + 0.75;
                    flicker *= sin(flickerPhase * 0.7 + 2.5) * 0.15 + 0.85;
                    
                    // Apply flicker amount
                    flicker = lerp(1.0, flicker, _FlickerAmount);
                    
                    starMask = starIntensity * flicker;
                }
                
                return starPos;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.texcoord = input.texcoord;
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.texcoord);
                
                // Generate stars
                float starMask;
                generateStars(dir, starMask);
                
                // Combine background and stars
                float3 finalColor = _BackgroundColor.rgb;
                finalColor = lerp(finalColor, _StarColor.rgb * _StarBrightness, starMask);
                
                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    
    FallBack Off
}
