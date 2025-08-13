Shader "Custom/RealisticOcean"
{
	Properties
	{
		_DeepColor("Deep Color", Color) = (0.0, 0.15, 0.25, 1)
		_ShallowColor("Shallow Color", Color) = (0.1, 0.4, 0.7, 1)
		_WaveHeight("Wave Height", Range(0.1, 2)) = 0.5
		_WaveFrequency("Wave Frequency", Range(1, 30)) = 10
		_WaveSpeed("Wave Speed", Range(0.1, 5)) = 1.0
		_WaveSteepness("Wave Steepness", Range(0.1, 1)) = 0.5
		_FoamColor("Foam Color", Color) = (1,1,1,1)
		_FoamThreshold("Foam Threshold", Range(0.01, 0.5)) = 0.1
		_FoamDensity("Foam Density", Range(1, 10)) = 3.0
		_Glossiness("Glossiness", Range(0, 1)) = 0.8
		_Specular("Specular", Range(0, 1)) = 0.5
		_FresnelPower("Fresnel Power", Range(1, 10)) = 5.0
		_NormalStrength("Normal Strength", Range(0, 2)) = 0.5
		_NormalSpeed("Normal Speed", Range(0, 2)) = 0.5
		_NormalScale("Normal Scale", Range(0.1, 5)) = 1.0
	}

		SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		LOD 300
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 worldPos : TEXCOORD0;
				float3 worldNormal : TEXCOORD1;
				float3 viewDir : TEXCOORD2;
				float3 waveNormal : TEXCOORD3;
				float4 screenPos : TEXCOORD4;
				float2 uv : TEXCOORD5;
				float slope : TEXCOORD6;
				float foamMask : TEXCOORD7;
				LIGHTING_COORDS(8,9)
			};

			fixed4 _DeepColor;
			fixed4 _ShallowColor;
			float _WaveHeight;
			float _WaveFrequency;
			float _WaveSpeed;
			float _WaveSteepness;
			fixed4 _FoamColor;
			float _FoamThreshold;
			float _FoamDensity;
			float _Glossiness;
			float _Specular;
			float _FresnelPower;
			float _NormalStrength;
			float _NormalSpeed;
			float _NormalScale;

			// 使用Gerstner波函数生成更真实的波浪
			float3 GerstnerWave(float3 position, float waveLength, float amplitude, float steepness, float2 direction, float speed, float time)
			{
				float k = 2 * UNITY_PI / waveLength;
				float c = sqrt(9.8 / k);
				float2 d = normalize(direction);
				float f = k * (dot(d, position.xz) - c * time * speed);
				float a = steepness / k;

				return float3(
					d.x * (a * cos(f)),
					a * sin(f),
					d.y * (a * cos(f))
				);
			}

			// 生成多方向波浪叠加
			float3 WaveDisplacement(float3 worldPos, float time, out float3 normal)
			{
				float3 displacement = float3(0,0,0);
				float3 normalSum = float3(0,0,0);

				// 创建四个不同方向的波浪
				float2 directions[4] = {
					float2(1.0, 0.0),
					float2(0.707, 0.707),
					float2(-0.5, 0.866),
					float2(-0.866, -0.5)
				};

				float wavelengths[4] = {
					10.0,
					7.0,
					15.0,
					5.0
				};

				float amplitudes[4] = {
					_WaveHeight * 0.8,
					_WaveHeight * 0.5,
					_WaveHeight * 0.3,
					_WaveHeight * 0.4
				};

				float speeds[4] = {
					_WaveSpeed * 0.8,
					_WaveSpeed * 1.2,
					_WaveSpeed * 0.7,
					_WaveSpeed * 1.0
				};

				for (int i = 0; i < 4; i++)
				{
					float3 wave = GerstnerWave(worldPos, wavelengths[i], amplitudes[i], _WaveSteepness, directions[i], speeds[i], time);
					displacement += wave;

					// 计算法线
					float k = 2 * UNITY_PI / wavelengths[i];
					float f = k * (dot(normalize(directions[i]), worldPos.xz) - speeds[i] * time);
					float wa = k * amplitudes[i];
					float s = sin(f);
					float c = cos(f);

					normalSum += float3(
						-directions[i].x * (wa * c),
						1 - _WaveSteepness * wa * s,
						-directions[i].y * (wa * c)
					);
				}

				normal = normalize(normalSum);
				displacement.y *= 0.5; // 降低垂直位移强度
				return displacement;
			}

			v2f vert(appdata v)
			{
				v2f o;
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				float t = _Time.y;

				// 波浪位移
				float3 waveNormal;
				float3 displacement = WaveDisplacement(worldPos, t, waveNormal);
				worldPos += displacement;

				// 计算波浪斜率（用于泡沫）
				float slope = 1.0 - saturate(dot(waveNormal, float3(0,1,0)));

				o.worldPos = worldPos;
				o.pos = UnityWorldToClipPos(worldPos);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.viewDir = normalize(UnityWorldSpaceViewDir(worldPos));
				o.waveNormal = waveNormal;
				o.screenPos = ComputeScreenPos(o.pos);
				o.uv = v.uv * _NormalScale;
				o.slope = slope;

				// 泡沫遮罩（基于波浪斜率和高度）
				float heightFactor = saturate(1.0 - abs(worldPos.y) / _WaveHeight);
				o.foamMask = saturate(slope * 5.0 + heightFactor * 0.5);

				TRANSFER_VERTEX_TO_FRAGMENT(o);
				return o;
			}

			// 生成法线纹理效果
			float3 GenerateNormal(float2 uv, float time)
			{
				float2 uv1 = uv + float2(0.1, 0.3) * time * _NormalSpeed;
				float2 uv2 = uv * 0.8 - float2(0.2, 0.1) * time * _NormalSpeed * 0.7;

				float3 normal1 = UnpackNormal(float4(sin(uv1.x * 10) * 0.5 + 0.5,
											sin(uv1.y * 7) * 0.5 + 0.5, 0, 1));
				float3 normal2 = UnpackNormal(float4(cos(uv2.x * 8) * 0.5 + 0.5,
											sin(uv2.y * 12) * 0.5 + 0.5, 0, 1));

				return normalize(float3(normal1.xy + normal2.xy, normal1.z * normal2.z)) * _NormalStrength;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				// 生成动态法线纹理
				float3 detailNormal = GenerateNormal(i.uv, _Time.y);
				float3 combinedNormal = normalize(float3(i.waveNormal.xy + detailNormal.xy, i.waveNormal.z * detailNormal.z));

				// 光照计算
				float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
				float3 halfVector = normalize(lightDir + i.viewDir);

				// 菲涅尔效应
				float fresnel = pow(1.0 - saturate(dot(combinedNormal, i.viewDir)), _FresnelPower);

				// 基础颜色（基于水深）
				float depthFactor = saturate(i.worldPos.y * 2.0);
				fixed4 waterColor = lerp(_ShallowColor, _DeepColor, depthFactor);

				// 镜面反射
				float specular = pow(saturate(dot(combinedNormal, halfVector)), _Glossiness * 256) * _Specular;
				float3 specularColor = _LightColor0.rgb * specular;

				// 漫反射
				float diffuse = saturate(dot(combinedNormal, lightDir));
				float3 diffuseColor = _LightColor0.rgb * diffuse * 0.5;

				// 泡沫计算
				float foam = saturate((i.foamMask - _FoamThreshold) * _FoamDensity);
				float foamPattern = sin(i.worldPos.x * 0.5 + i.worldPos.z * 0.3 + _Time.y * 2.0) * 0.5 + 0.5;
				foam *= foamPattern;

				// 组合所有效果
				float3 finalColor = waterColor.rgb + diffuseColor + specularColor;
				finalColor = lerp(finalColor, _FoamColor.rgb, saturate(foam));

				// 透明度
				float alpha = lerp(0.7, 0.95, fresnel);

				return fixed4(finalColor, alpha);
			}
			ENDCG
		}
	}
		FallBack "Transparent/Diffuse"
}