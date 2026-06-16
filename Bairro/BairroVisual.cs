using UnityEngine;
using CidadeLimpa.Core;
using CidadeLimpa.Lixo;

namespace CidadeLimpa.Bairros
{
    /// <summary>
    /// Visual 2D do bairro: um quad/sprite tingido pela cor de identidade que
    /// escurece conforme a sujeira aumenta (GDD seção 12 — "verde → cinza →
    /// escuro"). Cria o próprio SpriteRenderer se nenhum existir, para o Scene
    /// Builder não depender de assets externos.
    /// </summary>
    [RequireComponent(typeof(Bairro))]
    public class BairroVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer renderizador;
        [SerializeField] private float velocidadeTransicao = 2f;

        [Tooltip("Transparência do marcador do bairro (deixa o mapa aparecer por baixo).")]
        [SerializeField, Range(0f, 1f)] private float alpha = 0.32f;

        private Bairro _bairro;
        private Color _corAlvo;
        private Color _corBase;

        private void Awake()
        {
            _bairro = GetComponent<Bairro>();
            GarantirRenderizador();
        }

        private void Start()
        {
            _corBase = _bairro.Dados != null ? _bairro.Dados.corIdentidade : Paleta.Cinza;
            _corBase.a = alpha;
            AplicarEscala();
            _corAlvo = _corBase;
            if (renderizador != null) renderizador.color = _corBase;
        }

        private void GarantirRenderizador()
        {
            if (renderizador != null) return;

            renderizador = GetComponent<SpriteRenderer>();
            if (renderizador == null)
            {
                var go = new GameObject("Visual");
                go.transform.SetParent(transform, false);
                renderizador = go.AddComponent<SpriteRenderer>();
                renderizador.sprite = SpriteCirculo.Obter();
                renderizador.sortingOrder = -5; // acima do mapa, abaixo de lixo/caminhão
            }
        }

        private void AplicarEscala()
        {
            if (renderizador == null || _bairro.Dados == null) return;
            float d = _bairro.Dados.raio * 2f;
            renderizador.transform.localScale = new Vector3(d, d, 1f);
        }

        private void Update()
        {
            if (renderizador == null) return;

            // Limpeza 1 → cor de identidade; 0 → escurecida em direção à crise.
            float limpeza = _bairro.Limpeza01;
            Color sujo = Color.Lerp(_corBase, Paleta.Tinta, 0.65f);
            _corAlvo = Color.Lerp(sujo, _corBase, limpeza);
            _corAlvo.a = alpha; // mantém translucidez (deixa o mapa aparecer)

            renderizador.color = Color.Lerp(renderizador.color, _corAlvo,
                velocidadeTransicao * Time.deltaTime);
        }
    }
}
