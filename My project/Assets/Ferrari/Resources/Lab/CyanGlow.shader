Shader "FerrariLab/CyanGlow"
{
    Properties { _Color("Glow",Color)=(.15,.75,1,.6) _Radial("Radial",Float)=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off ZWrite Off Blend SrcAlpha One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            fixed4 _Color;float _Radial;
            v2f vert(appdata v) { v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float lineGlow=pow(saturate(1-abs(i.uv.y*2-1)),.7);
                float radius=length((i.uv-.5)*2);
                float floorGlow=smoothstep(.42,.78,radius)*(1-smoothstep(.78,1,radius));
                return fixed4(_Color.rgb,_Color.a*lerp(lineGlow,floorGlow,saturate(_Radial)));
            }
            ENDCG
        }
    }
}
