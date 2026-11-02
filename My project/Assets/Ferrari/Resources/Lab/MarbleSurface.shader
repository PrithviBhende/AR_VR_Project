Shader "FerrariLab/MarbleSurface"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 pos:SV_POSITION; float3 local:TEXCOORD0; float3 normal:TEXCOORD1; float3 world:TEXCOORD2; };
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p)
            {
                float2 cell=floor(p), f=frac(p);
                f=f*f*(3-2*f);
                return lerp(lerp(hash(cell),hash(cell+float2(1,0)),f.x),
                    lerp(hash(cell+float2(0,1)),hash(cell+1),f.x),f.y);
            }
            v2f vert(appdata v)
            {
                v2f o;o.pos=UnityObjectToClipPos(v.vertex);
                o.local=v.vertex.xyz;o.normal=UnityObjectToWorldNormal(v.normal);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=i.local.xz*8;
                float warp=noise(p*1.7)*1.25+noise(p*4.1)*.24;
                float contour=p.x*1.55+p.y*.55+warp*2.1;
                float thin=1-smoothstep(.014,.095,abs(sin(contour)));
                float branch=1-smoothstep(.008,.045,abs(sin(contour*2.1+p.y*.7)));
                float stone=noise(p*5.2)*.028+noise(p*1.4)*.018;
                float3 baseColor=float3(.014,.019,.029)+stone;
                float3 marble=lerp(baseColor,float3(.20,.23,.28),thin*.58+branch*.06);
                // A soft overhead highlight and an under-car contact shadow.
                float lightPool=exp(-dot(i.world.xz-float2(-.7,.35),i.world.xz-float2(-.7,.35))*.21);
                float contact=exp(-dot(i.local.xz*float2(2.0,3.2),i.local.xz*float2(2.0,3.2)));
                marble=marble*(.80+.20*lightPool)*(1-.30*contact);
                float side=saturate(i.normal.y*.5+.5);
                marble=lerp(float3(.014,.022,.033),marble,side);
                float3 viewDir=normalize(_WorldSpaceCameraPos-i.world);
                float spec=pow(saturate(dot(normalize(i.normal),normalize(viewDir+float3(.35,.8,-.48)))),48)*.12;
                return fixed4(marble+spec,1);
            }
            ENDCG
        }
    }
}
