using System.Collections;
using TMPro;
using UnityEngine;
using CidadeLimpa.Core;

namespace CidadeLimpa.UI
{
    /// <summary>
    /// Exibe textos flutuantes de "+sucata" sobre o ponto de ganho (feedback
    /// imediato — reforço positivo, GDD seção 13). Converte a posição de mundo em
    /// coordenadas de tela e anima um TMP que sobe e desaparece.
    /// </summary>
    public class FeedbackFlutuante : MonoBehaviour
    {
        [SerializeField] private float duracao = 1f;
        [SerializeField] private float subida = 60f;

        private RectTransform _raiz;
        private Camera _camera;

        private void Awake()
        {
            _raiz = (RectTransform)transform;
        }

        private void OnEnable() => GameEvents.SucataGanha += Mostrar;
        private void OnDisable() => GameEvents.SucataGanha -= Mostrar;

        private void Mostrar(long quantidade, Vector3 posicaoMundo)
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            Vector3 tela = _camera.WorldToScreenPoint(posicaoMundo);
            if (tela.z < 0f) return; // atrás da câmera

            var txt = UIFactory.Texto("Floating", _raiz, $"+{quantidade:n0}", 28f, Paleta.Eco, TextAlignmentOptions.Center);
            var rt = txt.rectTransform;
            rt.position = tela;
            StartCoroutine(Animar(txt));
        }

        private IEnumerator Animar(TextMeshProUGUI txt)
        {
            float t = 0f;
            Vector3 inicio = txt.rectTransform.position;
            Color cor = txt.color;

            while (t < duracao)
            {
                t += Time.unscaledDeltaTime;
                float p = t / duracao;
                txt.rectTransform.position = inicio + Vector3.up * (subida * p);
                cor.a = 1f - p;
                txt.color = cor;
                yield return null;
            }

            Destroy(txt.gameObject);
        }
    }
}
