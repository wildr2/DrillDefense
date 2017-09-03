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
		Blend One One

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 uv : TEXCOORD0;
			};

			v2f vert(appdata_base i)
			{
				v2f o;

				o.vertex = mul(UNITY_MATRIX_MVP, i.vertex);
				o.uv = i.texcoord;

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 c = tex2D(_MainTex, i.uv);
				
				bool underDigger = c.a > 0;

				if (underDigger)
				{
					return fixed4(1, 0, 0, 1);
				}
				return fixed4(0, 0, 0, 0);
			}
			ENDCG
		}
	}
}