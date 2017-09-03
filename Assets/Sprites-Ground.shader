// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Custom/Ground"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_VisionTex ("Fog Texture", 2D) = "white" {}
		_DugTex("Dug Texture", 2D) = "white" {}
		_FogColor("Fog Tint", Color) = (1,1,1,1)
		_DugColor("Dug Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#include "UnityCG.cginc"
			
			sampler2D _MainTex;
			sampler2D _VisionTex;
			sampler2D _DugTex;
			fixed4 _FogColor;
			fixed4 _DugColor;
			fixed4 _RendererColor;

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
			};

			v2f vert(appdata_base i)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(i.vertex);
				o.texcoord = i.texcoord;

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{				
				fixed4 c = tex2D(_MainTex, i.texcoord) * _RendererColor;
				fixed4 dug = tex2D(_DugTex, i.texcoord);
				fixed4 vision = tex2D(_VisionTex, i.texcoord);

				bool notSky = c.a > 0;

				if (notSky)
				{
					bool isKnownDug = dug.g == 1;
					bool isDug = dug.g > 0;
					bool hasVision = vision.r > 0;

					// Dug tint
					if (isKnownDug || (isDug && hasVision))
					{
						c.rgb = lerp(c.rgb, _DugColor, _DugColor.a);
					}

					// Fog tint
					if (!hasVision)
					{
						c.rgb = lerp(c.rgb, _FogColor, _FogColor.a);
					}
				}

				return c;
			}
			ENDCG
        }

		
    }
}
