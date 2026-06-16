using UnityEngine;
using CidadeLimpa.Core;

namespace CidadeLimpa.CameraSystem
{
    /// <summary>
    /// Câmera ortográfica 2.5D do mapa (GDD seção 11). Suporta pan (drag) e zoom
    /// (scroll/pinça), com limites de área e de zoom. As operações são expostas
    /// como métodos para o <see cref="GameInput.ControleEntrada"/> orquestrar a
    /// arbitragem entre toque-como-clique e toque-como-arraste.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Limites de Pan (mundo)")]
        [SerializeField] private Vector2 limiteMin = new Vector2(-12f, -12f);
        [SerializeField] private Vector2 limiteMax = new Vector2(12f, 12f);

        [Header("Zoom (tamanho ortográfico)")]
        [SerializeField] private float zoomMin = 4f;
        [SerializeField] private float zoomMax = 14f;
        [SerializeField] private float velocidadeZoom = 8f;

        [Header("Pan")]
        [SerializeField] private float suavizacaoPan = 12f;

        private Camera _camera;
        private Vector3 _alvoPosicao;
        private float _alvoZoom;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            _alvoPosicao = transform.position;
            _alvoZoom = _camera.orthographicSize;
        }

        private void LateUpdate()
        {
            transform.position = Vector3.Lerp(transform.position, _alvoPosicao, suavizacaoPan * Time.unscaledDeltaTime);
            _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, _alvoZoom, velocidadeZoom * Time.unscaledDeltaTime);
        }

        /// <summary>Desloca a câmera por um delta em pixels de tela (arraste).</summary>
        public void Arrastar(Vector2 deltaTela)
        {
            // Converte pixels em unidades de mundo conforme o zoom atual.
            float unidadesPorPixel = (_camera.orthographicSize * 2f) / Screen.height;
            Vector3 delta = new Vector3(-deltaTela.x, -deltaTela.y, 0f) * unidadesPorPixel;

            _alvoPosicao += delta;
            _alvoPosicao.x = Mathf.Clamp(_alvoPosicao.x, limiteMin.x, limiteMax.x);
            _alvoPosicao.y = Mathf.Clamp(_alvoPosicao.y, limiteMin.y, limiteMax.y);
        }

        /// <summary>Aplica um delta de zoom (positivo = aproximar).</summary>
        public void Zoom(float delta)
        {
            if (Mathf.Approximately(delta, 0f)) return;
            _alvoZoom = Mathf.Clamp(_alvoZoom - delta * (zoomMax - zoomMin), zoomMin, zoomMax);
        }

        /// <summary>Converte uma posição de tela em ponto no plano XY (z = 0).</summary>
        public Vector3 TelaParaMundo(Vector2 posTela)
        {
            Vector3 p = posTela;
            p.z = -transform.position.z; // distância até o plano z = 0
            Vector3 mundo = _camera.ScreenToWorldPoint(p);
            mundo.z = 0f;
            return mundo;
        }

        /// <summary>Centraliza a câmera num ponto (sem suavização extra).</summary>
        public void IrPara(Vector3 ponto)
        {
            _alvoPosicao = new Vector3(
                Mathf.Clamp(ponto.x, limiteMin.x, limiteMax.x),
                Mathf.Clamp(ponto.y, limiteMin.y, limiteMax.y),
                _alvoPosicao.z);
        }

        /// <summary>Configura limites/zoom a partir do Scene Builder.</summary>
        public void Configurar(Vector2 min, Vector2 max, float zMin, float zMax)
        {
            limiteMin = min; limiteMax = max; zoomMin = zMin; zoomMax = zMax;
            _alvoZoom = Mathf.Clamp(_alvoZoom, zoomMin, zoomMax);
        }
    }
}
