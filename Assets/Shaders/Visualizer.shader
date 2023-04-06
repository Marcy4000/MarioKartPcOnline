Shader "NoiseTest/Visualizer"
{
    Properties
    {
        _Color1("maxColor", Color) = (1,1,1,1)
        _Color0("minColor", Color) = (0,0,0,0)
        _Progress("Progress", Range(0.0,1.0)) = 0
        _Speed("Speed", Float) = 1
    }

    CGINCLUDE


    #include "UnityCG.cginc"
    #include "Packages/jp.keijiro.noiseshader/Shader/ClassicNoise2D.hlsl"
    #include "Packages/jp.keijiro.noiseshader/Shader/ClassicNoise3D.hlsl"
    #include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise2D.hlsl"
    #include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise3D.hlsl"

   
    void Vertex(float4 position : POSITION,
                float2 uv : TEXCOORD,
                out float4 outPosition : SV_Position,
                out float2 outUV : TEXCOORD)
    {
        outPosition = UnityObjectToClipPos(position);
        outUV = uv;
    }
    float4 _Color0;
    float4 _Color1;

    float _Speed;
    float _Progress;
    float getNoise(float2 uv){
        float o = 0.5;
        float s = 1;
        float w = 0.5;
        for (int i = 0; i < 6; i++)
        {
            
            float3 coord = float3(uv * s, _Time.y);
            float3 period = float3(s, s, 1.0) * 2.0;            
            o += PeriodicNoise(coord, period) * w;
            s *= 2.0;
            w *= 0.5;
        }
        return o;
    
    }
    float clampf(float x){
        if (1 - x <= 1- _Progress) return 1;
        return 0;
    }
    float4 Fragment(float4 position : SV_Position,
                    float2 uv : TEXCOORD) : SV_Target
    {
        float o = getNoise( uv * 8 + float2(1, 0.2) * _Time.y*_Speed);
       
        float transparency = getNoise( uv * 18);
        float3 min = _Color0.rgb;
        float3 max = _Color1.rgb;

        return float4(lerp(min,max ,o),clampf(o));
    }

    ENDCG
    SubShader
    {

    LOD 200

        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            ENDCG
        }
    }
}
