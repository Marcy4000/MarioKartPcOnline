Shader "NoiseTest/Visualizer"
{
    Properties
    {
        _Color1("maxColor", Color) = (1,1,1,1)
        _Color0("minColor", Color) = (0,0,0,0)

        _Speed("Speed", Float) = 1
    }

    CGINCLUDE


    #include "UnityCG.cginc"
    #include "Packages/jp.keijiro.noiseshader/Shader/ClassicNoise2D.hlsl"
    #include "Packages/jp.keijiro.noiseshader/Shader/ClassicNoise3D.hlsl"
    #include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise2D.hlsl"
    #include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise3D.hlsl"

   
    float3 lerp(float3 a, float3 b, float t) {
        return float3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
    }
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
    float4 Fragment(float4 position : SV_Position,
                    float2 uv : TEXCOORD) : SV_Target
    {

        uv = uv * 8 + float2(1, 0.2) * _Time.y*_Speed;
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
        float3 min = float3(_Color0.x, _Color0.y, _Color0.z);
        float3 max = float3(_Color1.x, _Color1.y, _Color1.z);
        return float4(lerp(min,max ,o), 1);

    }

    ENDCG
    SubShader
    {
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
