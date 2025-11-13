Shader "Custom/WhiteDwarfStar"
{
    Properties
    {
        _CoreColor ("Core Color", Color) = (1, 1, 1, 1)
        _GlowColor ("Glow Color", Color) = (0.9, 0.95, 1, 1)
        _CoreSize ("Core Size", Range(0, 1)) = 0.3
        _GlowSize ("Glow Size", Range(0, 2)) = 1.5
        _Brightness ("Brightness", Range(0, 5)) = 2.0
        
        [Header(Chromatic Aberration Pulse)]
        _ChromaticStrength ("Chromatic Strength", Range(0, 0.1)) = 0.02
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1.0
        _PulseMin ("Pulse Min", Range(0, 1)) = 0.3
        _PulseMax ("Pulse Max", Range(0, 1)) = 1.0
        
        [Header(Dying Effect)]
        _FlickerSpeed ("Flicker Speed", Range(0, 10)) = 2.0
        _FlickerAmount ("Flicker Amount", Range(0, 0.5)) = 0.1
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _CoreColor;
                float4 _GlowColor;
                float _CoreSize;
                float _GlowSize;
                float _Brightness;
                float _ChromaticStrength;
                float _PulseSpeed;
                float _PulseMin;
                float _PulseMax;
                float _FlickerSpeed;
                float _FlickerAmount;
            CBUFFER_END
            
            // Noise function for flickering
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                
                output.positionCS = vertexInput.positionCS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                // Center UVs
                float2 uv = input.uv * 2.0 - 1.0;
                
                // Pulse animation
                float pulse = lerp(_PulseMin, _PulseMax, (sin(_Time.y * _PulseSpeed) + 1.0) * 0.5);
                
                // Flicker for dying effect
                float flicker = 1.0 - noise(float2(_Time.y * _FlickerSpeed, 0)) * _FlickerAmount;
                
                // Distance from center
                float dist = length(uv);
                
                // --- Chromatic Aberration ---
                float chromaticOffset = _ChromaticStrength * pulse;
                
                // Sample at different positions for each color channel
                float2 redUV = uv * (1.0 + chromaticOffset);
                float2 greenUV = uv;
                float2 blueUV = uv * (1.0 - chromaticOffset);
                
                float distRed = length(redUV);
                float distGreen = length(greenUV);
                float distBlue = length(blueUV);
                
                // Core (bright center)
                float coreRed = 1.0 - smoothstep(0.0, _CoreSize, distRed);
                float coreGreen = 1.0 - smoothstep(0.0, _CoreSize, distGreen);
                float coreBlue = 1.0 - smoothstep(0.0, _CoreSize, distBlue);
                
                // Glow (outer halo)
                float glowRed = 1.0 - smoothstep(_CoreSize, _GlowSize, distRed);
                float glowGreen = 1.0 - smoothstep(_CoreSize, _GlowSize, distGreen);
                float glowBlue = 1.0 - smoothstep(_CoreSize, _GlowSize, distBlue);
                
                // Combine core and glow
                float3 color;
                color.r = lerp(_GlowColor.r * glowRed, _CoreColor.r, coreRed);
                color.g = lerp(_GlowColor.g * glowGreen, _CoreColor.g, coreGreen);
                color.b = lerp(_GlowColor.b * glowBlue, _CoreColor.b, coreBlue);
                
                // Apply brightness, pulse, and flicker
                color *= _Brightness * pulse * flicker;
                
                // Calculate alpha (star fades at edges)
                float alpha = max(max(coreRed, coreGreen), coreBlue) + 
                              max(max(glowRed, glowGreen), glowBlue) * 0.5;
                alpha = saturate(alpha);
                
                // Apply fog
                color = MixFog(color, input.fogFactor);
                
                return float4(color, alpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
