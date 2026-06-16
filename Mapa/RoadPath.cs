using UnityEngine;
using CidadeLimpa.Core;

namespace CidadeLimpa.Mapa
{
    /// <summary>
    /// Caminho da estrada principal (extraído da arte <c>estrada.png</c>). Guarda
    /// os waypoints em espaço local — como é filho do sprite da estrada, alinha-se
    /// automaticamente ao mapa. Expõe consultas por distância de arco para que os
    /// caminhões sigam a linha "bonitinho": posição/tangente em uma distância e
    /// projeção de um ponto do mundo na distância mais próxima ao longo da via.
    /// </summary>
    public class RoadPath : Singleton<RoadPath>
    {
        [SerializeField] private Vector2[] pontosLocais;

        private Vector3[] _mundo;
        private float[] _acum;

        /// <summary>Comprimento total da via, em unidades de mundo.</summary>
        public float Comprimento { get; private set; }

        /// <summary>True se a via tem pontos suficientes para navegação.</summary>
        public bool Valida => _mundo != null && _mundo.Length >= 2;

        protected override void AoInicializar() => Reconstruir();

        /// <summary>Define os waypoints (em espaço local) e reconstrói a tabela de arco.</summary>
        public void DefinirPontos(Vector2[] pontos)
        {
            pontosLocais = pontos;
            Reconstruir();
        }

        private void Reconstruir()
        {
            if (pontosLocais == null || pontosLocais.Length < 2)
            {
                _mundo = new Vector3[0];
                _acum = new float[0];
                Comprimento = 0f;
                return;
            }

            _mundo = new Vector3[pontosLocais.Length];
            for (int i = 0; i < pontosLocais.Length; i++)
                _mundo[i] = transform.TransformPoint(new Vector3(pontosLocais[i].x, pontosLocais[i].y, 0f));

            _acum = new float[_mundo.Length];
            _acum[0] = 0f;
            for (int i = 1; i < _mundo.Length; i++)
                _acum[i] = _acum[i - 1] + Vector3.Distance(_mundo[i - 1], _mundo[i]);

            Comprimento = _acum[_acum.Length - 1];
        }

        /// <summary>Posição no mundo a uma distância de arco (clamp nas extremidades).</summary>
        public Vector3 PontoEm(float distancia)
        {
            if (!Valida) return transform.position;
            distancia = Mathf.Clamp(distancia, 0f, Comprimento);

            for (int i = 1; i < _acum.Length; i++)
            {
                if (distancia <= _acum[i])
                {
                    float seg = Mathf.Max(1e-4f, _acum[i] - _acum[i - 1]);
                    float t = (distancia - _acum[i - 1]) / seg;
                    return Vector3.Lerp(_mundo[i - 1], _mundo[i], t);
                }
            }
            return _mundo[_mundo.Length - 1];
        }

        /// <summary>Direção (tangente normalizada) da via a uma distância de arco.</summary>
        public Vector3 TangenteEm(float distancia)
        {
            if (!Valida) return Vector3.up;
            distancia = Mathf.Clamp(distancia, 0f, Comprimento);

            for (int i = 1; i < _acum.Length; i++)
            {
                if (distancia <= _acum[i] + 1e-4f)
                {
                    Vector3 d = _mundo[i] - _mundo[i - 1];
                    d.z = 0f;
                    return d.sqrMagnitude > 1e-6f ? d.normalized : Vector3.up;
                }
            }
            Vector3 ult = _mundo[_mundo.Length - 1] - _mundo[_mundo.Length - 2];
            return ult.sqrMagnitude > 1e-6f ? ult.normalized : Vector3.up;
        }

        /// <summary>
        /// Projeta um ponto do mundo na via e retorna a distância de arco do ponto
        /// projetado mais próximo. Usado para "ancorar" bairros e depósito à via.
        /// </summary>
        public float ProjetarDistancia(Vector3 mundo)
        {
            if (!Valida) return 0f;
            mundo.z = 0f;

            float melhor = 0f;
            float melhorD2 = float.MaxValue;

            for (int i = 1; i < _mundo.Length; i++)
            {
                Vector3 a = _mundo[i - 1]; a.z = 0f;
                Vector3 b = _mundo[i]; b.z = 0f;
                Vector3 ab = b - a;
                float len2 = Mathf.Max(1e-6f, ab.sqrMagnitude);
                float t = Mathf.Clamp01(Vector3.Dot(mundo - a, ab) / len2);
                Vector3 proj = a + t * ab;
                float d2 = (proj - mundo).sqrMagnitude;
                if (d2 < melhorD2)
                {
                    melhorD2 = d2;
                    melhor = _acum[i - 1] + t * (_acum[i] - _acum[i - 1]);
                }
            }
            return melhor;
        }

        private void OnDrawGizmos()
        {
            if (pontosLocais == null || pontosLocais.Length < 2) return;
            Gizmos.color = Color.red;
            for (int i = 1; i < pontosLocais.Length; i++)
            {
                Vector3 a = transform.TransformPoint(pontosLocais[i - 1]);
                Vector3 b = transform.TransformPoint(pontosLocais[i]);
                Gizmos.DrawLine(a, b);
            }
        }
    }
}
