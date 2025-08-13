Shader "Custom/OceanWave_CG"
{
	Properties
	{
		_BaseColor("Base Color", Color) = (0.0, 0.3, 0.6, 1)
		_WaveHeight("Wave Height", Float) = 0.5
		_WaveFrequency("Wave Frequency", Float) = 10
		_WaveSpeed("Wave Speed", Float) = 1.0
		_FoamColor("Foam Color", Color) = (1,1,1,1)
		_FoamThreshold("Foam Threshold", Float) = 0.1
	}

		SubShader
	{
		Tags { "RenderType" = "Opaque" }


		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv     : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv  : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
			};

			fixed4 _BaseColor;
			float _WaveHeight;
			float _WaveFrequency;
			float _WaveSpeed;
			fixed4 _FoamColor;
			float _FoamThreshold;

			// 波浪公式（和脚本保持一致）
			float WaveHeightFunc(float3 worldPos, float time)
			{
				float wave1 = sin(time * 2.0 + worldPos.x * _WaveFrequency) * 0.5;
				float wave2 = cos(time * 1.5 + worldPos.z * _WaveFrequency) * 0.3;
				return (wave1 + wave2) * _WaveHeight;
			}

			v2f vert(appdata v)
			{
				v2f o;
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				float t = _Time.y * _WaveSpeed;

				// 顶点位移（海浪）
				worldPos.y += WaveHeightFunc(worldPos, t);

				o.worldPos = worldPos;
				o.pos = UnityWorldToClipPos(worldPos);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				
				float height = i.worldPos.y;

			// 泡沫遮罩
			float foamMask = smoothstep(_FoamThreshold, 0.0, abs(height));

			//fixed3 finalColor = lerp(_BaseColor.rgb, _FoamColor.rgb, foamMask);
			fixed3 finalColor = lerp(_BaseColor.rgb, _FoamColor.rgb, foamMask);
			return fixed4(finalColor, 1.0);
		}
		ENDCG
	}
	}
}
