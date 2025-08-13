using UnityEngine;

public class WaveCalculator : MonoBehaviour
{
    // ���˲�������Shader����һ�£�
    [Header("���˲���")]
    public float _WaveHeight = 1.0f;
    public float _WaveSpeed = 1.0f;
    public float _WaveSteepness = 0.5f;

    // ���˷���Ԥ��
    private static readonly Vector2[] WaveDirections = {
        new Vector2(1.0f, 0.0f),
        new Vector2(0.707f, 0.707f),
        new Vector2(-0.5f, 0.866f),
        new Vector2(-0.866f, -0.5f)
    };

    // ������������Shader����һ�£�
    private static readonly float[] WaveLengths = {
        10.0f,
        7.0f,
        15.0f,
        5.0f
    };

    // �����������Shader����һ�£�
    private float[] WaveAmplitudes => new float[] {
        _WaveHeight * 0.8f,
        _WaveHeight * 0.5f,
        _WaveHeight * 0.3f,
        _WaveHeight * 0.4f
    };

    // �ٶȲ�������Shader����һ�£�
    private float[] WaveSpeeds => new float[] {
        _WaveSpeed * 0.8f,
        _WaveSpeed * 1.2f,
        _WaveSpeed * 0.7f,
        _WaveSpeed * 1.0f
    };

    // ��ȡ����λ�ô��Ĳ��˸߶�
    public float GetWaveHeight(Vector3 worldPosition)
    {
        Vector3 normal;
        Vector3 displacement = WaveDisplacement(worldPosition, Time.time, out normal);
        return worldPosition.y + displacement.y;
    }

    // ��ȡ����λ�ô��Ĳ���λ�ƺͷ���
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

            // ���㷨�߹���
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

        // ���ʹ�ֱλ��ǿ�ȣ���Shaderһ�£�
        displacement.y *= 0.5f;

        // ��һ������
        normal = normalSum.normalized;

        return displacement;
    }

    // Gerstner�����㣨��Shaderʵ��һ�£�
    private Vector3 GerstnerWave(Vector3 position, float wavelength, float amplitude, float steepness, Vector2 direction, float speed, float time)
    {
        // ���㲨��
        float k = 2 * Mathf.PI / wavelength;

        // �����һ��
        Vector2 dirNormalized = direction.normalized;

        // �����˺���λ
        float dotProduct = Vector2.Dot(dirNormalized, new Vector2(position.x, position.z));
        float phase = k * (dotProduct - speed * time);

        // �������Ǻ���ֵ
        float cosPhase = Mathf.Cos(phase);
        float sinPhase = Mathf.Sin(phase);

        // Gerstner����ʽ
        float x = steepness * amplitude * dirNormalized.x * cosPhase;
        float y = amplitude * sinPhase;
        float z = steepness * amplitude * dirNormalized.y * cosPhase;

        return new Vector3(x, y, z);
    }

    // ʾ������ȡ��ȷ���˸߶ȣ��������߼�����������
    public float GetExactWaveHeight(Vector3 position)
    {
        Vector3 normal;
        Vector3 displacement = WaveDisplacement(position, Time.time, out normal);
        return  displacement.y;
    }
}