Shader "Custom/EcholocationMultiSphere"
{
    Properties
    {
        _HighlightColor("Highlight Color", Color) = (1,1,1,1)
        _Threshold("Line Thickness", Float) = 0.03
        _SphereCount("Number of Active Spheres", Int) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            ZWrite On
            ZTest LEqual
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            // Max number of simultaneous ripples
            #define MAX_SPHERES 10
            float4 _SphereCenters[MAX_SPHERES];
            float _SphereRadii[MAX_SPHERES];
            int _SphereCount;
            float _Threshold;
            float4 _HighlightColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float edge = 0;

                for (int s = 0; s < _SphereCount; s++)
                {
                    float dist = abs(length(i.worldPos - _SphereCenters[s].xyz) - _SphereRadii[s]);
                    edge = max(edge, saturate(1.0 - dist / _Threshold));
                }

                return lerp(fixed4(0,0,0,1), _HighlightColor, edge);
            }

            ENDCG
        }
    }
}
