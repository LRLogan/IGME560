Shader "Custom/HDRP/TriplanarTerrainLit"
{
    Properties
    {
        _SplatMap("Splat Map", 2D) = "white" {}

        _GrassTex("Grass", 2D) = "white" {}
        _RockTex("Rock", 2D) = "white" {}
        _DirtTex("Dirt", 2D) = "white" {}
        _SandTex("Sand", 2D) = "white" {}

        _GrassNormal("Grass Normal", 2D) = "bump" {}
        _RockNormal("Rock Normal", 2D) = "bump" {}
        _DirtNormal("Dirt Normal", 2D) = "bump" {}
        _SandNormal("Sand Normal", 2D) = "bump" {}

        _Tiling("Tiling", Float) = 1
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal

    #pragma vertex Vert
    #pragma fragment Frag

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"

    TEXTURE2D(_SplatMap);
    SAMPLER(sampler_SplatMap);

    TEXTURE2D(_GrassTex); SAMPLER(sampler_GrassTex);
    TEXTURE2D(_RockTex);  SAMPLER(sampler_RockTex);
    TEXTURE2D(_DirtTex);  SAMPLER(sampler_DirtTex);
    TEXTURE2D(_SandTex);  SAMPLER(sampler_SandTex);

    TEXTURE2D(_GrassNormal);
    TEXTURE2D(_RockNormal);
    TEXTURE2D(_DirtNormal);
    TEXTURE2D(_SandNormal);

    float _Tiling;

    struct Attributes
    {
        float3 positionOS : POSITION;
        float3 normalOS   : NORMAL;
        float2 uv         : TEXCOORD0;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float3 positionWS;
        float3 normalWS;
        float2 uv;
    };

    Varyings Vert(Attributes v)
    {
        Varyings o;
        o.positionWS = TransformObjectToWorld(v.positionOS);
        o.positionCS = TransformWorldToHClip(o.positionWS);
        o.normalWS = TransformObjectToWorldNormal(v.normalOS);
        o.uv = v.uv;
        return o;
    }

    // ----------------------------
    // TRIPLANAR SAMPLING
    // ----------------------------

    float4 TriplanarTex(TEXTURE2D_PARAM(tex, samp), float3 p, float3 n)
    {
        float3 blend = abs(n);
        blend /= (blend.x + blend.y + blend.z);

        float4 x = SAMPLE_TEXTURE2D(tex, samp, p.yz);
        float4 y = SAMPLE_TEXTURE2D(tex, samp, p.xz);
        float4 z = SAMPLE_TEXTURE2D(tex, samp, p.xy);

        return x * blend.x + y * blend.y + z * blend.z;
    }

    float3 TriplanarNormal(TEXTURE2D_PARAM(tex, samp), float3 p, float3 n)
    {
        float3 blend = abs(n);
        blend /= (blend.x + blend.y + blend.z);

        float3 x = UnpackNormal(SAMPLE_TEXTURE2D(tex, samp, p.yz));
        float3 y = UnpackNormal(SAMPLE_TEXTURE2D(tex, samp, p.xz));
        float3 z = UnpackNormal(SAMPLE_TEXTURE2D(tex, samp, p.xy));

        return normalize(x * blend.x + y * blend.y + z * blend.z);
    }

    // ----------------------------
    // FRAGMENT
    // ----------------------------

    void Frag(Varyings IN,
              out float4 outColor : SV_Target)
    {
        float3 pos = IN.positionWS * _Tiling;
        float3 nrm = normalize(IN.normalWS);

        float4 splat = SAMPLE_TEXTURE2D(_SplatMap, sampler_SplatMap, IN.uv);

        float4 grass = TriplanarTex(TEXTURE2D_ARGS(_GrassTex, sampler_GrassTex), pos, nrm);
        float4 rock  = TriplanarTex(TEXTURE2D_ARGS(_RockTex, sampler_RockTex), pos, nrm);
        float4 dirt  = TriplanarTex(TEXTURE2D_ARGS(_DirtTex, sampler_DirtTex), pos, nrm);
        float4 sand  = TriplanarTex(TEXTURE2D_ARGS(_SandTex, sampler_SandTex), pos, nrm);

        float3 color =
            grass.rgb * splat.r +
            rock.rgb  * splat.g +
            dirt.rgb  * splat.b +
            sand.rgb  * splat.a;

        outColor = float4(color, 1);
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="ForwardOnly" }
        }
    }
}