using UnityEngine;
using CidadeLimpa.Core;
using CidadeLimpa.Anim;

namespace CidadeLimpa.Lixo
{
    /// <summary>
    /// Uma unidade de resíduo no mapa (GDD seção 04). É um objeto poolável:
    /// nunca é destruído, apenas ativado/desativado pelo <see cref="LixoPool"/>.
    /// Representação visual flat 2D via SpriteRenderer; o tamanho cresce com o
    /// acúmulo (urgência), conforme HUD ("tamanho indica urgência").
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class LixoItem : MonoBehaviour
    {
        [Header("Volume")]
        [Tooltip("Quantas unidades de capacidade este lixo ocupa no caminhão.")]
        [SerializeField] private int volume = 1;

        [Tooltip("Sucata base concedida ao descarregar este resíduo.")]
        [SerializeField] private long valorSucataBase = 5;

        private SpriteRenderer _sr;

        /// <summary>Tipo do resíduo.</summary>
        public TipoLixo Tipo { get; private set; }

        /// <summary>Categoria reciclável (relevante quando Tipo == Reciclavel).</summary>
        public CategoriaReciclavel Categoria { get; private set; }

        /// <summary>Volume ocupado na capacidade do caminhão.</summary>
        public int Volume => volume;

        /// <summary>Sucata base deste resíduo (antes de eficiência/multiplicadores).</summary>
        public long ValorSucataBase => valorSucataBase;

        /// <summary>Callback chamado pelo pool quando este item deve voltar à reserva.</summary>
        public System.Action<LixoItem> AoLiberar;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Configura o item ao ser retirado do pool.
        /// </summary>
        public void Configurar(TipoLixo tipo, CategoriaReciclavel categoria, Vector3 posicao)
        {
            Tipo = tipo;
            Categoria = categoria;
            transform.position = posicao;

            // Tinte suave para preservar a arte do sprite; o tamanho indica urgência.
            switch (tipo)
            {
                case TipoLixo.Comum:
                    volume = 1; valorSucataBase = 5;
                    DefinirVisual(Color.white, 1.1f);
                    break;
                case TipoLixo.Reciclavel:
                    volume = 1; valorSucataBase = 12;
                    DefinirVisual(Color.Lerp(Color.white, Paleta.Eco, 0.4f), 1.2f);
                    break;
                case TipoLixo.GrandeAcumulo:
                    volume = 4; valorSucataBase = 30;
                    DefinirVisual(Color.Lerp(Color.white, Paleta.Alerta, 0.45f), 2.0f);
                    break;
            }

            gameObject.SetActive(true);

            // Pop ao surgir (já começa visível, caso a animação não rode).
            Vector3 alvo = transform.localScale;
            transform.localScale = alvo * 0.6f;
            Tween.Escala(transform, alvo, 0.25f, TipoEase.OutBack, naoEscalado: false);
        }

        private void DefinirVisual(Color cor, float escala)
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _sr.color = cor;
            transform.localScale = Vector3.one * escala;
        }

        /// <summary>Devolve o item ao pool.</summary>
        public void Liberar()
        {
            AoLiberar?.Invoke(this);
        }
    }
}
