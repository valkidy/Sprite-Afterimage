Shader "Sprites/SpriteSmearFramesRenderer"
{
    Properties
    {
        [HideInInspector] [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [HideInInspector] [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0

        _Intensity ("Intensity", Range(-0.3, 0.3)) = 0
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
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment SpriteFrag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"

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

            // Material Color.
            uniform fixed4 _Color;            
            uniform float _Intensity;            
            uniform float4x4 _ObjectToWorld;

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

                uint cmdID = GetCommandID(0);
                uint instanceID = GetIndirectInstanceID(svInstanceID);
                float4 wpos = mul(_ObjectToWorld, IN.vertex + float4(instanceID, cmdID, 0, 0));
                OUT.vertex = mul(UNITY_MATRIX_VP, wpos);

                // OUT.vertex = UnityFlipSprite(IN.vertex, _Flip);
                // OUT.vertex = UnityObjectToClipPos(OUT.vertex);
                OUT.vertexOS = IN.vertex;
                OUT.texcoord = UnityFlipSpriteUV(IN.texcoord, _Flip);
                OUT.color = IN.color * _Color * _RendererColor;

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

            inline float rand(float2 seed)
            {
                return frac(sin(dot(seed, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 SpriteFrag(v2f IN) : SV_Target
            {              
                float dragDir = sign(_Intensity.x);
                float dragForce = -dragDir * abs(_Intensity.x);
                
                // range [-0.5, 0.5] ~ [-0.7, 0.7]
                float ddx = (IN.vertexOS.x + 0.5 + 0.2 * dragDir);
                if (dragForce > 0) { ddx = 1.0 - ddx; }
                
                fixed2 uvOffset = -dragDir * fixed2(dragForce * frac(rand(fixed2(0, IN.texcoord.y)) + 0.15), 0);

                // if (ddx < 0.5)
                //     uvOffset = fixed2(0,0);
                // else
                //     uvOffset *= 2.0 * (ddx - 0.5);
                uvOffset *= step(0.5, ddx) * 2.0 * (ddx - 0.5);

                fixed4 c = SampleSpriteTexture (IN.texcoord + uvOffset) * IN.color;
                c.rgb *= c.a;                
                return c;                
                // return fixed4(IN.texcoord, 0, 1);
            }
            ENDCG
        }
    }
}
