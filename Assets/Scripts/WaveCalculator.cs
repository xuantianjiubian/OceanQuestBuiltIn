using UnityEngine;

public class WaveCalculator : MonoBehaviour
{
    // 波浪参数（与Shader保持一致）
    [Header("波浪参数")]
    public float _WaveHeight = 1.0f;
    public float _WaveSpeed = 1.0f;
    public float _WaveSteepness = 0.5f;

    // 波浪方向预设
    private static readonly Vector2[] WaveDirections = {
        new Vector2(1.0f, 0.0f),
        new Vector2(0.707f, 0.707f),
        new Vector2(-0.5f, 0.866f),
        new Vector2(-0.866f, -0.5f)
    };

    // 波长参数（与Shader保持一致）
    private static readonly float[] WaveLengths = {
        10.0f,
        7.0f,
        15.0f,
        5.0f
    };

    // 振幅参数（与Shader保持一致）
    private float[] WaveAmplitudes => new float[] {
        _WaveHeight * 0.8f,
        _WaveHeight * 0.5f,
        _WaveHeight * 0.3f,
        _WaveHeight * 0.4f
    };

    // 速度参数（与Shader保持一致）
    private float[] WaveSpeeds => new float[] {
        _WaveSpeed * 0.8f,
        _WaveSpeed * 1.2f,
        _WaveSpeed * 0.7f,
        _WaveSpeed * 1.0f
    };

    // 获取世界位置处的波浪高度
    public float GetWaveHeight(Vector3 worldPosition)
    {
        Vector3 normal;
        Vector3 displacement = WaveDisplacement(worldPosition, Time.time, out normal);
        return worldPosition.y + displacement.y;
    }

    // 获取世界位置处的波浪位移和法线
    public Vector3 WaveDisplacement(Vector3 worldPos, float time, out Vector3 normal)
    {
        Vector3 displacement = Vector3.zero;
        Vector3 normalSum = Vector3.zero;

        for (int i = 0; i < 4; i++)
        {
            float wavelength = WaveLengths[i];
            float amplitude = WaveAmplitudes[i];
            float speed = WaveSpeeds[i];
            Vector2 direction = WaveDirections[i];

            Vector3 wave = GerstnerWave(worldPos, wavelength, amplitude, _WaveSteepness, direction, speed, time);
            displacement += wave;

            // 计算法线贡献
            float k = 2 * Mathf.PI / wavelength;
            float f = k * (Vector2.Dot(direction.normalized, new Vector2(worldPos.x, worldPos.z)) - speed * time);
            float wa = k * amplitude;
            float s = Mathf.Sin(f);
            float c = Mathf.Cos(f);

            normalSum += new Vector3(
                -direction.normalized.x * (wa * c),
                1 - _WaveSteepness * wa * s,
                -direction.normalized.y * (wa * c)
            );
        }

        // 降低垂直位移强度（与Shader一致）
        displacement.y *= 0.5f;

        // 归一化法线
        normal = normalSum.normalized;

        return displacement;
    }

    // Gerstner波计算（与Shader实现一致）
    private Vector3 GerstnerWave(Vector3 position, float wavelength, float amplitude, float steepness, Vector2 direction, float speed, float time)
    {
        // 计算波数
        float k = 2 * Mathf.PI / wavelength;

        // 方向归一化
        Vector2 dirNormalized = direction.normalized;

        // 计算点乘和相位
        float dotProduct = Vector2.Dot(dirNormalized, new Vector2(position.x, position.z));
        float phase = k * (dotProduct - speed * time);

        // 计算三角函数值
        float cosPhase = Mathf.Cos(phase);
        float sinPhase = Mathf.Sin(phase);

        // Gerstner波公式
        float x = steepness * amplitude * dirNormalized.x * cosPhase;
        float y = amplitude * sinPhase;
        float z = steepness * amplitude * dirNormalized.y * cosPhase;

        return new Vector3(x, y, z);
    }

    // 示例：获取精确波浪高度（用于射线检测替代方案）
    public float GetExactWaveHeight(Vector3 position)
    {
        Vector3 normal;
        Vector3 displacement = WaveDisplacement(position, Time.time, out normal);
        return  displacement.y;
    }
}