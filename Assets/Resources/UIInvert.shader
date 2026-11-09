Shader "UI/ParryVignette"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color   ("Tint",    Color) = (1,1,1,1)
        // How sharply the glow falls off from the edges (higher = tighter)
        _Falloff ("Falloff", Range(0.5, 3.0)) = 1.4
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Overlay"
            "IgnoreProjector" = "True"
            "RenderType"      = "Transparent"
        }

        Cull Off
        Lighting Off
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
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float  _Falloff;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                o.color  = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Distance from center, scaled so the corner = 1.0
                float2 d    = (i.uv - 0.5) * 2.0;
                float  dist = length(d) * _Falloff;

                // Smooth ramp: fully transparent inside, white glow at edges
                float edge = smoothstep(0.5, 1.4, dist);

                // Image alpha (driven by UIManager timer) controls overall intensity
                float alpha = edge * i.color.a;

                return fixed4(0.0, 0.0, 0.0, alpha);
            }
            ENDCG
        }
    }
}
