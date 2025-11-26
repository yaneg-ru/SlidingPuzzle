Shader "Custom/PuzzlePieceShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _R ("Red", Range(0, 1)) = 1
        _G ("Green", Range(0, 1)) = 1
        _B ("Blue", Range(0, 1)) = 1
        _A ("Alpha", Range(0, 1)) = 1
        _Invert ("Invert RGB", Float) = 0
        _Brightness ("Brightness", Range(-1, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _R, _G, _B, _A;
            float _Invert;
            float _Brightness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // Яркость
                col.rgb += _Brightness;

                // Инверсия RGB
                if (_Invert > 0.5)
                    col.rgb = 1 - col.rgb;

                // Масштабирование каналов
                col.r *= _R;
                col.g *= _G;
                col.b *= _B;
                col.a *= _A;

                return col;
            }
            ENDCG
        }
    }
}