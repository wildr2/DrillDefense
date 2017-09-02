Shader "Custom/Circle" 
{
	Properties
	{
		[PerRendererData] _MainTex("Color (RGB) Alpha (A)", 2D) = "white"
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
		Blend One OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct vtf
			{
				float4 vertex : SV_POSITION;
				float3 uv : TEXCOORD0;
			};

			vtf vert(appdata_base v)
			{
				vtf o;

				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = v.texcoord;

				return o;
			}

			fixed4 frag(vtf In) : SV_Target
			{
				float2 center = float2(0.5, 0.5);
				float dist = distance(In.uv.xy, center);

				if (dist > 0.5f)
				{
					return fixed4(0,0,0,0);
				}
				else 
				{
					return fixed4(1,1,1,1);
				}
			}
			ENDCG
		}
	}
}