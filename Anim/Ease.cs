using UnityEngine;

namespace CidadeLimpa.Anim
{
    /// <summary>Funções de suavização (easing) para os tweens.</summary>
    public enum TipoEase { Linear, OutQuad, InOutQuad, OutBack, OutElastic }

    public static class Ease
    {
        public static float Avaliar(TipoEase tipo, float t)
        {
            t = Mathf.Clamp01(t);
            switch (tipo)
            {
                case TipoEase.OutQuad: return 1f - (1f - t) * (1f - t);
                case TipoEase.InOutQuad: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                case TipoEase.OutBack:
                {
                    const float c1 = 1.70158f, c3 = 2.70158f;
                    float u = t - 1f;
                    return 1f + c3 * u * u * u + c1 * u * u;
                }
                case TipoEase.OutElastic:
                {
                    if (t == 0f || t == 1f) return t;
                    const float c4 = (2f * Mathf.PI) / 3f;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
                }
                default: return t;
            }
        }
    }
}
