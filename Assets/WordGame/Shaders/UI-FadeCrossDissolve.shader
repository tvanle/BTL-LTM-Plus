Shader "UI/FadeCrossDissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        // Dissolve properties
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _DissolveColor ("Dissolve Edge Color", Color) = (1, 0.8, 0.2, 1)
        _DissolveEdgeWidth ("Dissolve Edge Width", Range(0, 0.2)) = 0.05
        _DissolveGlow ("Dissolve Glow Intensity", Range(0, 5)) = 2
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float _DissolveAmount;
            fixed4 _DissolveColor;
            float _DissolveEdgeWidth;
            float _DissolveGlow;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

                OUT.color = v.color * _Color;
                return OUT;
            }

            // Noise function for dissolve pattern
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Voronoi-like pattern for organic dissolve
            float voronoiNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);

                float minDist = 1.0;

                for(int y = -1; y <= 1; y++)
                {
                    for(int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(float(x), float(y));
                        float2 cellPoint = i + neighbor;
                        float2 diff = neighbor + noise(cellPoint) - f;
                        float dist = length(diff);
                        minDist = min(minDist, dist);
                    }
                }

                return minDist;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                // Generate dissolve pattern
                float2 dissolveUV = IN.texcoord * 8.0; // Scale for pattern
                float noiseValue = voronoiNoise(dissolveUV);

                // Add animated distortion
                float time = _Time.y * 0.5;
                noiseValue += noise(IN.texcoord * 5.0 + time) * 0.1;

                // Calculate dissolve threshold
                float dissolveThreshold = 1.0 - _DissolveAmount;
                float dissolveEdge = dissolveThreshold + _DissolveEdgeWidth;

                // Apply dissolve
                if(noiseValue < dissolveThreshold)
                {
                    discard;
                }

                // Add glowing edge
                if(noiseValue < dissolveEdge)
                {
                    float edgeFactor = (dissolveEdge - noiseValue) / _DissolveEdgeWidth;
                    float3 edgeGlow = _DissolveColor.rgb * edgeFactor * _DissolveGlow;
                    color.rgb += edgeGlow;
                }

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
