Shader "Custom/Digger" 
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
		Blend One OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;

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
				fixed4 c = tex2D(_MainTex, In.uv);
				if (c.a > 0)
				{
					return fixed4(1, 0, 0, 1);
				}
				return fixed4(0, 0, 0, 0);
			}
			ENDCG
		}
	}
}