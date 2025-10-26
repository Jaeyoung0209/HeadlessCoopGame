Shader "Custom/MangaOutline"
{
    Properties
    {
        _OutlineThickness ("Outline Thickness", Float) = 3.0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" }
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass 
        {
            Name "MANGA OUTLINE"
            
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            #pragma vertex Vert
            #pragma fragment frag

            float _OutlineThickness;

            float rand2d(float2 co) { return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453); }
            float value_noise(float2 uv) { float2 i = floor(uv); float2 f = frac(uv); f = smoothstep(0.0, 1.0, f); float bl = rand2d(i), br = rand2d(i + float2(1,0)); float tl = rand2d(i + float2(0,1)), tr = rand2d(i + float2(1,1)); return lerp(lerp(bl, br, f.x), lerp(tl, tr, f.x), f.y); }
            float fbm(float2 uv) { float t = 0.0, a = 0.5; t += value_noise(uv) * a; uv *= 2.0; a *= 0.5; t += value_noise(uv) * a; return t; }

            float3 DecodeNormal(float4 enc) { float2 f = enc.xy * 2.0 - 1.0; float3 n = float3(f.x, f.y, 1.0 - abs(f.x) - abs(f.y)); float t = saturate(-n.z); n.xy += (n.xy >= 0.0 ? -t : t); return normalize(n); }
            
            float SobelDepth(float2 uv, float2 texel_size, float thickness)
            {
                float s00 = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, uv + texel_size * float2(-1, -1) * thickness).r;
                float s10 = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, uv + texel_size * float2( 0, -1) * thickness).r;
                float s20 = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, uv + texel_size * float2( 1, -1) * thickness).r;
                float s01 = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, uv + texel_size * float2(-1,  0) * thickness).r;
                float s21 = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, uv + texel_size * float2( 1,  0) * thickness).r;
                float s02 = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, uv + texel_size * float2(-1,  1) * thickness).r;
                float s12 = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, uv + texel_size * float2( 0,  1) * thickness).r;
                float s22 = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, uv + texel_size * float2( 1,  1) * thickness).r;
                float gx = s00 + 2*s01 + s02 - (s20 + 2*s21 + s22);
                float gy = s00 + 2*s10 + s20 - (s02 + 2*s12 + s22);
                return sqrt(gx*gx + gy*gy);
            }

            float SobelNormal(float2 uv, float2 texel_size, float thickness)
            {
                float3 n00 = DecodeNormal(_CameraNormalsTexture.Sample(sampler_CameraNormalsTexture, uv + texel_size * float2(-1, -1) * thickness));
                float3 n10 = DecodeNormal(_CameraNormalsTexture.Sample(sampler_CameraNormalsTexture, uv + texel_size * float2( 0, -1) * thickness));
                float3 n20 = DecodeNormal(_CameraNormalsTexture.Sample(sampler_CameraNormalsTexture, uv + texel_size * float2( 1, -1) * thickness));
                float3 n01 = DecodeNormal(_CameraNormalsTexture.Sample(sampler_CameraNormalsTexture, uv + texel_size * float2(-1,  0) * thickness));
                float3 n21 = DecodeNormal(_CameraNormalsTexture.Sample(sampler_CameraNormalsTexture, uv + texel_size * float2( 1,  0) * thickness));
                float3 n02 = DecodeNormal(_CameraNormalsTexture.Sample(sampler_CameraNormalsTexture, uv + texel_size * float2(-1,  1) * thickness));
                float3 n12 = DecodeNormal(_CameraNormalsTexture.Sample(sampler_CameraNormalsTexture, uv + texel_size * float2( 0,  1) * thickness));
                float3 n22 = DecodeNormal(_CameraNormalsTexture.Sample(sampler_CameraNormalsTexture, uv + texel_size * float2( 1,  1) * thickness));
                float3 gx = n00 + 2*n01 + n02 - (n20 + 2*n21 + n22);
                float3 gy = n00 + 2*n10 + n20 - (n02 + 2*n12 + n22);
                return sqrt(dot(gx, gx) + dot(gy, gy));
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                half4 _OutlineColor = half4(0.02, 0.02, 0.02, 1.0);
                float _LineDistortion = 2.0;
                float _NoiseScale = 150.0;
                float _DepthThreshold = 0.005;
                float _NormalThreshold = 0.8;

                float2 uv = IN.texcoord;
                float2 texel_size = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);
                
                float noise = fbm(uv * _NoiseScale);
                float2 distortion_offset = float2(noise, fbm(uv * _NoiseScale + 0.5)) * 2.0 - 1.0;
                distortion_offset *= _LineDistortion * texel_size;
                float2 distorted_uv = uv + distortion_offset;
                
                float edge_depth = SobelDepth(distorted_uv, texel_size, _OutlineThickness);
                float edge_normal = SobelNormal(distorted_uv, texel_size, _OutlineThickness);
                
                edge_depth = edge_depth > _DepthThreshold ? 1 : 0;
                edge_normal = edge_normal > _NormalThreshold ? 1 : 0;

                float edge = max(edge_depth, edge_normal);
                
                return half4(_OutlineColor.rgb, _OutlineColor.a * edge);
            }
            ENDHLSL
        }
    }
}