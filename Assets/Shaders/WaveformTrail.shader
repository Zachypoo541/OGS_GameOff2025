Shader "Custom/WaveformTrail"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _WaveType ("Wave Type", Float) = 0  // 0=Sine, 1=Square, 2=Sawtooth, 3=Triangle
        _Frequency ("Frequency", Float) = 10.0
        _Amplitude ("Amplitude", Float) = 0.4
        _Speed ("Speed", Float) = 1.0
        _Thickness ("Thickness", Float) = 0.08
        _Glow ("Glow Intensity", Float) = 2.5
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
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
            float _WaveType;
            float _Frequency;
            float _Amplitude;
            float _Speed;
            float _Thickness;
            float _Glow;
            
            #define PI 3.14159265359
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            // Sine wave function
            float sineWave(float x)
            {
                return sin(x * PI * 2.0) * _Amplitude;
            }
            
            // Square wave function with proper transitions
            float squareWave(float x)
            {
                float t = frac(x);
                return (t < 0.5 ? 1.0 : -1.0) * _Amplitude;
            }
            
            // Sawtooth wave function
            float sawtoothWave(float x)
            {
                float t = frac(x);
                return (t * 2.0 - 1.0) * _Amplitude;
            }
            
            // Triangle wave function
            float triangleWave(float x)
            {
                float t = frac(x);
                return (abs(t * 4.0 - 2.0) - 1.0) * _Amplitude;
            }
            
            // Distance from point to line segment (for vertical sections)
            float distanceToSegment(float2 p, float2 a, float2 b)
            {
                float2 pa = p - a;
                float2 ba = b - a;
                float h = saturate(dot(pa, ba) / dot(ba, ba));
                return length(pa - ba * h);
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float time = _Time.y;
                
                // X coordinate with frequency and time animation
                float x = (i.uv.x * _Frequency) - (time * _Speed);
                
                // Y coordinate centered at 0.5
                float y = i.uv.y - 0.5;
                
                // Calculate wave value and distance
                float dist = 999.0;
                float waveValue = 0;
                
                // Use floor to ensure clean integer conversion
                int waveTypeInt = (int)floor(_WaveType + 0.5);
                
                // SINE WAVE
                if (waveTypeInt == 0)
                {
                    waveValue = sineWave(x);
                    dist = abs(y - waveValue);
                }
                // SQUARE WAVE - with thick vertical sections
                else if (waveTypeInt == 1)
                {
                    float t = frac(x);
                    float prevValue = (frac(x - 0.001) < 0.5 ? 1.0 : -1.0) * _Amplitude;
                    float currValue = (t < 0.5 ? 1.0 : -1.0) * _Amplitude;
                    
                    // Check if we're near a transition (vertical section)
                    float distToTransition = min(abs(t), abs(t - 1.0));
                    
                    if (distToTransition < 0.05) // Near transition
                    {
                        // Measure distance to vertical line segment
                        float2 p = float2(t, y);
                        float2 a = float2(0, -_Amplitude);
                        float2 b = float2(0, _Amplitude);
                        dist = distanceToSegment(p, a, b) * 20.0; // Scale for proper thickness
                    }
                    else
                    {
                        // Horizontal section
                        dist = abs(y - currValue);
                    }
                }
                // SAWTOOTH WAVE - with thick vertical sections
                else if (waveTypeInt == 2)
                {
                    float t = frac(x);
                    float currValue = (t * 2.0 - 1.0) * _Amplitude;
                    
                    // Check if we're near the vertical drop
                    if (t > 0.95) // Near the end, vertical drop section
                    {
                        float2 p = float2(t, y);
                        float2 a = float2(1.0, _Amplitude);
                        float2 b = float2(1.0, -_Amplitude);
                        dist = distanceToSegment(p, a, b) * 20.0;
                    }
                    else
                    {
                        // Diagonal rising section
                        dist = abs(y - currValue);
                    }
                }
                // TRIANGLE WAVE
                else
                {
                    waveValue = triangleWave(x);
                    dist = abs(y - waveValue);
                }
                
                // Create smooth line with anti-aliasing
                float lineWidth = _Thickness;
                float aa = max(fwidth(dist) * 1.5, 0.001);
                float alpha = 1.0 - smoothstep(lineWidth - aa, lineWidth + aa, dist);
                
                // Add glow
                float glowWidth = lineWidth * _Glow;
                float glow = 1.0 - smoothstep(lineWidth, glowWidth, dist);
                glow = pow(glow, 2.0);
                
                // Combine base line and glow
                float finalAlpha = saturate(alpha + glow * 0.4);
                
                // Fade at edges for smooth trail start/end
                float edgeFadeX = smoothstep(0.0, 0.05, i.uv.x) * smoothstep(1.0, 0.95, i.uv.x);
                finalAlpha *= edgeFadeX;
                
                // Brighter where line is solid
                float brightness = lerp(0.6, 1.0, alpha);
                
                return float4(_Color.rgb * brightness, finalAlpha * _Color.a);
            }
            ENDCG
        }
    }
}