// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Circle" 
{
	Properties
	{
		[PerRendererData] _MainTex("Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)
		_InnerRadius("Inner Radius", float) = 0.4
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
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _Color;
			float _InnerRadius;

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
				float2 center = float2(0.5, 0.5);
				float dist = distance(In.uv.xy, center);

				if (dist > 0.5f)
				{
					return fixed4(0, 0, 0, 0);
				}
				else if (dist > _InnerRadius)
				{
					return _Color;
				}
				else
				{
					return fixed4(0, 0, 0, 0);
				}
			}
			ENDCG
		}
	}
}