Shader "Custom/SelfCastGlowingBorder"
{
    Properties
    {
        _Color ("Border Color", Color) = (1,1,1,1)
        _Intensity ("Intensity", Range(0, 2)) = 1.0
        _BorderWidth ("Border Width", Range(0, 0.3)) = 0.1
        _BorderSoftness ("Border Softness", Range(0, 0.3)) = 0.15
        _DistortionAmount ("Border Distortion Amount", Range(0, 0.3)) = 0.15
        _NoiseScale ("Noise Scale", Float) = 10.0
        _NoiseSpeed ("Noise Speed", Float) = 0.5
        _GlowIntensity ("Glow Intensity", Range(0, 2)) = 1.5
        _PulseSpeed ("Pulse Speed", Float) = 2.0
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.2
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Overlay" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off
        
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
            
            float4 _Color;
            float _Intensity;
            float _BorderWidth;
            float _BorderSoftness;
            float _DistortionAmount;
            float _NoiseScale;
            float _NoiseSpeed;
            float _GlowIntensity;
            float _PulseSpeed;
            float _PulseAmount;
            
            // Simple 2D noise function
            float noise(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            // Smooth noise
            float smoothNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // Smoothstep
                
                float a = noise(i);
                float b = noise(i + float2(1.0, 0.0));
                float c = noise(i + float2(0.0, 1.0));
                float d = noise(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // Fractal noise
            float fractalNoise(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                
                for (int i = 0; i < 3; i++)
                {
                    value += amplitude * smoothNoise(p);
                    p *= 2.0;
                    amplitude *= 0.5;
                }
                
                return value;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // Calculate radial distance from center (elliptical border)
                float2 center = float2(0.5, 0.5);
                float2 delta = uv - center;
                
                // Don't adjust for aspect ratio - let it be elliptical to match screen shape
                float distFromCenter = length(delta);
                
                // Maximum possible distance from center (to corner of screen)
                float maxDist = length(float2(0.5, 0.5));
                
                // Normalize distance (0 at center, 1 at corners)
                float normalizedDist = distFromCenter / maxDist;
                
                // Add noise distortion to make the border shape irregular/blobby
                float2 noiseUV = uv * _NoiseScale;
                float time = _Time.y * _NoiseSpeed;
                
                // Generate noise for border distortion (slower, larger scale for shape)
                float shapeNoise1 = fractalNoise(noiseUV * 0.5 + float2(time * 0.3, 0));
                float shapeNoise2 = fractalNoise(noiseUV * 0.3 - float2(0, time * 0.2));
                float shapeDistortion = (shapeNoise1 + shapeNoise2) * 0.5;
                
                // Apply distortion to the distance - makes the border wavy/blobby
                float distortedDist = normalizedDist + (shapeDistortion - 0.5) * _DistortionAmount;
                
                // Create elliptical border mask (1 at edges, 0 at center)
                // Use multiple smoothstep layers for extra soft, blurred edges
                float border1 = smoothstep(1.0 - _BorderWidth - _BorderSoftness, 1.0 - _BorderWidth + _BorderSoftness, distortedDist);
                float border2 = smoothstep(1.0 - _BorderWidth - _BorderSoftness * 0.5, 1.0 - _BorderWidth + _BorderSoftness * 1.5, distortedDist);
                
                // Blend the two layers for a softer, more gradual falloff
                float border = (border1 * 0.6 + border2 * 0.4);
                
                // Apply an additional smoothing curve for extra softness
                border = pow(border, 1.5);
                
                // Animated noise for the glow texture (keep original noise for surface detail)
                
                // Animated noise for the glow texture (keep original noise for surface detail)
                // Add different noise layers moving in different directions
                float noise1 = fractalNoise(noiseUV + float2(time, 0));
                float noise2 = fractalNoise(noiseUV - float2(0, time * 0.7));
                float combinedNoise = (noise1 + noise2) * 0.5;
                
                // Apply noise to border
                float noisyBorder = border * combinedNoise;
                
                // Add pulsing effect
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                
                // Combine effects
                float finalAlpha = noisyBorder * _Intensity * pulse * _GlowIntensity;
                
                // Add edge glow
                float edgeGlow = border * 0.5;
                finalAlpha += edgeGlow * _Intensity * pulse;
                
                // Output color
                float4 col = _Color;
                col.a *= saturate(finalAlpha);
                
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "UI/Default"
}
