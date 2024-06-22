Shader "Sprites/SpriteAfterimageCrop"
{
    Properties
    {
        [HideInInspector] _MainTex ("Source Texture", 2D) = "white" {}
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
        // Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;    
                float4 color : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            int _Slice;
            int _FrameIndex;
            float2 _Flip;
            
            // Mapping vertex layout to texcoord on the dist render texture,
			//	assume frame index = 0
            //
            //    (-1, 1)   (-0.75, 1)
            //       0 -------- 1
            //       |          |
            //       |          |
            //       2 ---------3
            //    (-1, 0.75)(-0.75,0.75)
            //
            //  Frame index order on the dist render texture.
            //
            //    0  4  8 12 
            //    1  5  9 13
            //    2  6 10 14
            //    3  7 11 15
                                   
            v2f vert(appdata v)
            {                            
                // avoid crop image bleeding
                float padding = 1.0 - 1e-2;

                float widthStep = 1.0 / float(_Slice);                
                float2 origin = float2(-1.0 + widthStep, 1.0 - widthStep);
                float2 pivot = float2(_FrameIndex / _Slice, -_FrameIndex % _Slice);
                float2 norm = 2.0 * (padding * v.vertex.xy - 0.5);
                
                v2f o;
                o.vertex = float4(origin + widthStep * (2.0 * pivot + norm), 0, 1);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);   
                
                // flip sprite by_Flip 
                float2 flip = 1.0 - step(0, _Flip); // _Flip.x < 0
                o.uv = (1.0 - 2.0 * flip) * o.uv + flip;
                // vertical flip 
                o.uv.y = 1.0 - o.uv.y;

                o.color = float4(o.vertex.xy, 1, 1);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv); // * i.color;                 
            }
            ENDCG
        }
    }
}
