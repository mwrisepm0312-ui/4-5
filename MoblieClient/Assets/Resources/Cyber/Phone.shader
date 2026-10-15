Shader "UI/PhoneRestore"
{
    Properties
    {
        [PerPixelAlpha] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0,10)) = 1
        _Saturation ("Saturation", Range(0,10)) = 1
        _Blur ("Blur", Range(0,10)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _Brightness;
            float _Saturation;
            float _Blur;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // ºí·¯: ÁÖº¯ ÇÈ¼¿ Æò±Õ (¹Ý°æÀº _Blur¿¡ ºñ·Ê)
                float2 r = _MainTex_TexelSize.xy * _Blur * 12.0;
                fixed4 col = tex2D(_MainTex, i.uv) * 0.2;
                col += tex2D(_MainTex, i.uv + float2( r.x, 0)) * 0.1;
                col += tex2D(_MainTex, i.uv + float2(-r.x, 0)) * 0.1;
                col += tex2D(_MainTex, i.uv + float2(0,  r.y)) * 0.1;
                col += tex2D(_MainTex, i.uv + float2(0, -r.y)) * 0.1;
                col += tex2D(_MainTex, i.uv + float2( r.x,  r.y)) * 0.1;
                col += tex2D(_MainTex, i.uv + float2(-r.x,  r.y)) * 0.1;
                col += tex2D(_MainTex, i.uv + float2( r.x, -r.y)) * 0.1;
                col += tex2D(_MainTex, i.uv + float2(-r.x, -r.y)) * 0.1;

                // Ã¤µµ: Èæ¹é ¡ê ¿ø»ö
                float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = lerp(float3(gray, gray, gray), col.rgb, _Saturation);

                // ¹à±â
                col.rgb *= _Brightness;

                return col * i.color;
            }
            ENDCG
        }
    }
}