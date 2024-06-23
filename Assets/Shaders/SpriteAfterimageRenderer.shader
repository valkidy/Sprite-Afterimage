Shader "Sprites/SpriteAfterimageRenderer"
{
    Properties
    {
        [HideInInspector] [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}        
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [HideInInspector] [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0        
        _Color ("Tint", Color) = (1,1,1,1)
        _Alpha ("Alpha", Range(0, 1)) = 0.5
        [Enum(One,1, SrcAlpha,5)] _BlendOp ("Src blendOp", Float) = 5
        [KeywordEnum(Src, Gradient, Rainbow)] _Style ("Afterimage style", Float) = 2        
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        LOD 100
        
        Cull Off
        Lighting Off
        ZWrite Off
        Blend [_BlendOp] OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment SpriteFrag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile _STYLE_SRC _STYLE_GRADIENT _STYLE_RAINBOW

            #include "UnityCG.cginc"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"
            #include "Common.cginc"

            #ifdef UNITY_INSTANCING_ENABLED

                UNITY_INSTANCING_BUFFER_START(PerDrawSprite)
                    // SpriteRenderer.Color while Non-Batched/Instanced.
                    UNITY_DEFINE_INSTANCED_PROP(fixed4, unity_SpriteRendererColorArray)
                    // this could be smaller but that's how bit each entry is regardless of type
                    UNITY_DEFINE_INSTANCED_PROP(fixed2, unity_SpriteFlipArray)
                UNITY_INSTANCING_BUFFER_END(PerDrawSprite)

                #define _RendererColor  UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_SpriteRendererColorArray)
                #define _Flip           UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_SpriteFlipArray)

            #endif // instancing

            CBUFFER_START(UnityPerDrawSprite)
            #ifndef UNITY_INSTANCING_ENABLED
                fixed4 _RendererColor;
                fixed2 _Flip;
            #endif
                float _EnableExternalAlpha;
            CBUFFER_END
            
            uniform fixed4 _Color;
            uniform float _Alpha;
            uniform float4x4 _ObjectToWorld[64];
            uniform float _TotalFrameCount;            

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 vertexOS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            inline float4 UnityFlipSprite(in float3 pos, in fixed2 flip)
            {
                return float4(pos.xy * flip, pos.z, 1.0);
            }

            inline float2 UnityFlipSpriteUV(in float2 uv, in fixed2 flip)
			{
                return step(flip, 0) + sign(flip) * uv;
            }
                
            v2f SpriteVert(appdata_t IN,  uint svInstanceID : SV_InstanceID)                        
            {
                InitIndirectDrawArgs(0);

                v2f OUT;

                UNITY_SETUP_INSTANCE_ID (IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float4x4 objectToWorld = _ObjectToWorld[int(IN.vertex.z)];
                float4 wpos = mul(objectToWorld, float4(IN.vertex.xy, 0, 1));
                OUT.vertex = mul(UNITY_MATRIX_VP, wpos);

                OUT.vertexOS = IN.vertex;
                OUT.texcoord = IN.texcoord; // UnityFlipSpriteUV(IN.texcoord, _Flip);
#ifdef _STYLE_SRC
                OUT.color = _Color;
#elif _STYLE_GRADIENT
                OUT.color = float(IN.vertex.z/_TotalFrameCount) * _Color;
#else // _STYLE_RAINBOW
                OUT.color = fixed4(interpolateRainbowColor(IN.vertex.z/_TotalFrameCount), 0);
#endif                  
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap (OUT.vertex);
                #endif

                return OUT;
            }

            sampler2D _MainTex;
            sampler2D _AlphaTex;
            
            fixed4 SampleSpriteTexture (float2 uv)
            {
                fixed4 color = tex2D (_MainTex, uv);

            #if ETC1_EXTERNAL_ALPHA
                fixed4 alpha = tex2D (_AlphaTex, uv);
                color.a = lerp (color.a, alpha.r, _EnableExternalAlpha);
            #endif

                return color;
            }

            fixed4 SpriteFrag(v2f IN) : SV_Target
            {              
                fixed4 c = SampleSpriteTexture (IN.texcoord);

#ifdef _STYLE_SRC
                c.rgb *= (IN.color * c.a);
#else // _STYLE_GRADIENT || _STYLE_RAINBOW
                c.rgb = dot(c.rgb, float3(0.3, 0.59, 0.11)).rrr + IN.color;
#endif                    
                c.a *= _Alpha;
                return c;                
            }
            ENDCG
        }
    }
}
