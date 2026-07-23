Shader "UEBS2Stereo/SBSComposite"
{
    Properties
    {
        _LeftTex ("Left Eye", 2D) = "black" {}
        _RightTex ("Right Eye", 2D) = "black" {}
        _UiTex ("UI", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _LeftTex;
            sampler2D _RightTex;
            sampler2D _UiTex;
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            v2f vert(appdata_img v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 eyeUV = float2(uv.x < 0.5 ? uv.x * 2.0 : (uv.x - 0.5) * 2.0, uv.y);
                fixed4 eye = uv.x < 0.5 ? tex2D(_LeftTex, eyeUV) : tex2D(_RightTex, eyeUV);
                fixed4 ui = tex2D(_UiTex, eyeUV);
                return lerp(eye, ui, ui.a);
            }
            ENDCG
        }
    }
    FallBack Off
}
