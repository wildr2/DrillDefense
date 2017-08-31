// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Sprites/FOW"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_FogTex ("Fog Texture", 2D) = "white" {}
		_DugTex("Dug Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		_FogColor("Fog Tint", Color) = (1,1,1,1)
		_DugColor("Dug Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
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

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
			CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
			sampler2D _FogTex;
			sampler2D _DugTex;
			fixed4 _FogColor;
			fixed4 _DugColor;

			fixed4 frag(v2f IN) : SV_Target
			{				
				fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
				fixed4 dug = tex2D(_DugTex, IN.texcoord);
				fixed4 fog = tex2D(_FogTex, IN.texcoord);
				
				if (dug.a == 1)
				{
					c.rgb = lerp(c.rgb, _DugColor, _DugColor.a);
				}
				if (fog.a == 1)
				{
					c.rgb = lerp(c.rgb, _FogColor, _FogColor.a);
				}
				return c;
			}
			ENDCG
        }

		
    }
}
