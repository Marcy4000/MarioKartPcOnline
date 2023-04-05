Shader "CustomRenderTexture/New Custom Render Texture"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _Speed("Speed", Float) = 1
     }

     SubShader
     {
        Blend One Zero

        Pass
        {
            Name "New Custom Render Texture"

            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            float4      _Color;
            sampler2D   _MainTex;
            struct vec2 {
                float x;
                float y;
            };
            float interpolate(float a0, float a1, float w)
            {
                /* // You may want clamping by inserting:
                 * if (0.0 > w) return a0;
                 * if (1.0 < w) return a1;
                 */
                return (a1 - a0) * w + a0;
                /* // Use this cubic interpolation [[Smoothstep]] instead, for a smooth appearance:
                 * return (a1 - a0) * (3.0 - w * 2.0) * w * w + a0;
                 *
                 * // Use [[Smootherstep]] for an even smoother result with a second derivative equal to zero on boundaries:
                 * return (a1 - a0) * ((w * (w * 6.0 - 15.0) + 10.0) * w * w * w) + a0;
                 */
            }

            /* Create pseudorandom direction vector
             */
            vec2 randomGradient(int ix, int iy)
            {
                // No precomputed gradients mean this works for any number of grid coordinates
                uint w = 8 *32;
                uint s = w / 2; // rotation width
                uint a = ix;
                uint b = iy;
                a *= 3284157443;
                b ^= a << s | a >> w - s;
                b *= 1911520717;
                a ^= b << s | b >> w - s;
                a *= 2048419325;
                float random = a * (3.14159265 / ~(~0u >> 1)); // in [0, 2*Pi]
                vec2 val;
                val.x = cos(random);
                val.y = sin(random);
                return val;
            }

            // Computes the dot product of the distance and gradient vectors.
            float dotGridGradient(int ix, int iy, float x, float y)
            {
                // Get gradient from integer coordinates
                vec2 gradient = randomGradient(ix, iy);

                // Compute the distance vector
                float dx = x - (float)ix;
                float dy = y - (float)iy;

                // Compute the dot-product
                return (dx * gradient.x + dy * gradient.y);
            }

            // Compute Perlin noise at coordinates x, y
            float perlin(float x, float y)
            {
                // Determine grid cell coordinates
                int x0 = (int)floor(x);
                int x1 = x0 + 1;
                int y0 = (int)floor(y);
                int y1 = y0 + 1;

                // Determine interpolation weights
                // Could also use higher order polynomial/s-curve here
                float sx = x - (float)x0;
                float sy = y - (float)y0;

                // Interpolate between grid point gradients
                float n0, n1, ix0, ix1, value;

                n0 = dotGridGradient(x0, y0, x, y);
                n1 = dotGridGradient(x1, y0, x, y);
                ix0 = interpolate(n0, n1, sx);

                n0 = dotGridGradient(x0, y1, x, y);
                n1 = dotGridGradient(x1, y1, x, y);
                ix1 = interpolate(n0, n1, sx);

                value = interpolate(ix0, ix1, sy);
                return value;
            }

            float4 frag(v2f_customrendertexture IN) : COLOR
            {
                float2 uv = IN.localTexcoord.xy;    
                float val = perlin(uv.x, uv.y);
                float4 color = (val,val,val,1) * _Color;
                
                return color;
            }
            ENDCG
        }
    }
}
