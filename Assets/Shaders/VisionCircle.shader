// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/VisionCircle" 
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

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			v2f vert(appdata_base i)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(i.vertex);
				o.uv = i.texcoord;

				return o;
			}

			fixed4 frag(v2f In) : SV_Target
			{
				const float PI = 3.14159;

				float2 center = float2(0.5, 0.5);
				float dist = distance(In.uv.xy, center);
				float a = atan2(In.uv.y - 0.5, In.uv.x - 0.5);
				float t = (sin(a * 5 + _Time * 30) + 1) * 0.5;
				float t2 = (sin(a * 3 - _Time * 50) + 1) * 0.5;
				float r = 0.47 + 0.015 * t + 0.015 * t2;

				if (dist > r)
				{
					// No vision
					return fixed4(0,0,0,0);
				}
				else 
				{
					// Vision (within circle)
					return fixed4(1,1,1,1);
				}
			}
			ENDCG
		}
	}
}