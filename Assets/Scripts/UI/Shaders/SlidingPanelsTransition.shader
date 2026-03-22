Shader "UI/SlidingPanelsTransition"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Panel Color", Color) = (0,0,0,1)

        _Progress ("Progress", Range(0,1)) = 0
        _PanelCount ("Panel Count", Float) = 3
        _EdgeSoftness ("Edge Softness", Range(0.0001,0.1)) = 0.002

        // UI default support
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 uv            : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _MainTex_ST;

            float _Progress;
            float _PanelCount;
            float _EdgeSoftness;

            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPosition = v.vertex;
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            float SoftRange(float value, float minValue, float maxValue, float softness)
            {
                float left = smoothstep(minValue - softness, minValue + softness, value);
                float right = 1.0 - smoothstep(maxValue - softness, maxValue + softness, value);
                return saturate(left * right);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // Tránh chia cho 0
                float panelCount = max(1.0, round(_PanelCount));
                float progress = saturate(_Progress);
                float softness = max(0.0001, _EdgeSoftness);

                // Xác định panel hiện tại theo hàng ngang
                float rowFloat = floor(saturate(uv.y) * panelCount);

                // Trường hợp uv.y = 1 có thể thành panelCount, nên clamp lại
                rowFloat = min(rowFloat, panelCount - 1.0);

                // Chẵn đi từ phải, lẻ đi từ trái
                // row = 0,2,4... => dir = +1
                // row = 1,3,5... => dir = -1
                float isOdd = fmod(rowFloat, 2.0);
                float dir = lerp(1.0, -1.0, isOdd);

                // Ở progress = 0: panel nằm ngoài màn hình
                // Ở progress = 1: panel khớp toàn màn hình
                float shiftedX = uv.x - dir * (1.0 - progress);

                // Chỉ render khi phần panel đã "trượt vào" tới vị trí pixel hiện tại
                float panelMask = SoftRange(shiftedX, 0.0, 1.0, softness);

                // Fade dần theo progress
                float fade = smoothstep(0.0, 1.0, progress);

                fixed4 color = i.color;
                color.a *= panelMask * fade;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}