Shader "Custom/FOW"
{
	Properties
	{
		[PerRendererData] _MainTex("Texture", 2D) = "white" {}
		_VisionTex("Vision Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags 
		{
			"RenderType"="Opaque"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One One

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _VisionTex;
			
			struct vtf
			{
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
			};

			vtf vert(appdata_base v)
			{
				vtf o;

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = v.texcoord;

				return o;
			}
			
			fixed4 frag (vtf i) : SV_Target
			{
				fixed4 c = tex2D(_MainTex, i.texcoord);
				fixed4 vision = tex2D(_VisionTex, i.texcoord);

				bool isDug = c.r > 0;
				bool haveVision = vision.r > 0;

				if (isDug && haveVision)
				{
					// Mark dug pixels as dug and seen
					return fixed4(1, 1, 1, 1);
				}
				
				return c;
			}
			ENDCG
		}
	}
}
