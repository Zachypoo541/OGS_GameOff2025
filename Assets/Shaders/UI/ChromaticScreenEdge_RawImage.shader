Shader "UI/ChromaticScreenEdge_RawImage"
{
    Properties
    {
        _MainTex ("Dummy Texture", 2D) = "white" {}
        
        _ChromaticSpread ("Chromatic Spread", Range(0, 0.2)) = 0.05
        _ChromaticIntensity ("Chromatic Intensity", Range(0, 5)) = 2.0
        _EdgeThickness ("Edge Thickness", Range(0, 0.5)) = 0.15

        _RedColor ("Red Line Color", Color) = (1,0,0,1)
        _GreenColor ("Green Line Color", Color) = (0,1,0,1)
        _BlueColor ("Blue Line Color", Color) = (0,0,1,1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma target 3.0
            
            sampler2D _MainTex;

            float _ChromaticSpread;
            float _ChromaticIntensity;
            float _EdgeThickness;

            float4 _RedColor;
            float4 _GreenColor;
            float4 _BlueColor;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 screenUV : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // Convert clip-space (-1..1) to screen UV (0..1)
                o.screenUV = o.pos.xy / o.pos.w * 0.5 + 0.5;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.screenUV;

                float2 edgeDist = abs(uv - 0.5) * 2.0;
                float maxEdgeDist = max(edgeDist.x, edgeDist.y);

                float edgeMask = smoothstep(1.0 - _EdgeThickness, 1.0, maxEdgeDist);
                if (edgeMask < 0.01)
                    discard;

                float2 centerDir = normalize(uv - 0.5);
                float spread = _ChromaticSpread;

                float3 col = 0;
                float alpha = 0;

                float2 ruv = uv + centerDir * spread * 1.0;
                float rm = smoothstep(1.0 - _EdgeThickness, 1.0, 
                      max(abs(ruv.x - 0.5) * 2.0, abs(ruv.y - 0.5) * 2.0));
                col += _RedColor.rgb * rm;
                alpha = max(alpha, rm);

                float2 guv = uv + centerDir * spread * 0.66;
                float gm = smoothstep(1.0 - _EdgeThickness, 1.0, 
                      max(abs(guv.x - 0.5) * 2.0, abs(guv.y - 0.5) * 2.0));
                col += _GreenColor.rgb * gm;
                alpha = max(alpha, gm);

                float2 buv = uv + centerDir * spread * 0.33;
                float bm = smoothstep(1.0 - _EdgeThickness, 1.0, 
                      max(abs(buv.x - 0.5) * 2.0, abs(buv.y - 0.5) * 2.0));
                col += _BlueColor.rgb * bm;
                alpha = max(alpha, bm);

                col *= _ChromaticIntensity;

                return float4(col, alpha);
            }
            ENDCG
        }
    }
}
