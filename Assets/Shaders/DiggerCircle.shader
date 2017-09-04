Shader "Custom/DiggerCircle" 
{
	Properties
	{
		[PerRendererData] _MainTex("Texture", 2D) = "white"
	}
	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
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

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			v2f vert(appdata_base i)
			{
				v2f o;

				o.vertex = mul(UNITY_MATRIX_MVP, i.vertex);
				o.uv = i.texcoord;

				return o;
			}

			fixed4 frag(v2f In) : SV_Target
			{
				float2 center = float2(0.5, 0.5);
				float dist = distance(In.uv.xy, center);

				if (dist > 0.5f)
				{
					return fixed4(0, 0, 0, 0);
				}
				else 
				{
					return fixed4(1, 0, 0, 1);
				}
			}
			ENDCG
		}
	}
}