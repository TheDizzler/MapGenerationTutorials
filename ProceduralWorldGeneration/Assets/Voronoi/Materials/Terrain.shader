Shader "AtomosZ/Voronoi/Terrain"
{
	Properties
	{

	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		struct Input
		{
			float3 worldPos;
		};

		const static int maxColorCount = 8; // this doesn't work
		const static float epsilon = 1E-4;

		
		int layerCount;
		float3 baseColors[maxColorCount];
		float baseStartHeights[maxColorCount];

		float minHeight;
		float maxHeight;


		float InverseLerp(float a, float b, float value)
		{
			return saturate((value - a) / (b - a));
		}

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			float heightPercent = InverseLerp(minHeight, maxHeight, IN.worldPos.y);
			for (int i = 0; i < layerCount; ++i)
			{
				float drawStrength = saturate(sign(heightPercent - baseStartHeights[i]));
				o.Albedo = o.Albedo * (1 - drawStrength) + baseColors[i] * drawStrength;
			}
		}
		ENDCG
	}
		FallBack "Diffuse"
}
