using System.Collections.Generic;
using UnityEngine;

namespace CidadeLimpa.Core
{
    /// <summary>
    /// Calcula e mantém o índice global de satisfação da população (GDD seção 07).
    /// A satisfação deriva da limpeza média ponderada dos bairros, mas é um valor
    /// com inércia: drifta suavemente até o alvo e pode receber deltas imediatos
    /// (ex.: evento Mutirão Escolar). Publica mudanças de valor e de estado.
    /// </summary>
    public class SatisfacaoManager : Singleton<SatisfacaoManager>
    {
        [Header("Configuração")]
        [Tooltip("Satisfação inicial (0–100).")]
        [SerializeField, Range(0f, 100f)] private float satisfacaoInicial = 100f;

        [Tooltip("Velocidade de convergência da satisfação até o alvo (pontos/segundo).")]
        [SerializeField] private float velocidadeDrift = 6f;

        [Tooltip("Frequência (segundos) de recálculo do alvo a partir dos bairros.")]
        [SerializeField] private float intervaloRecalculo = 0.5f;

        private readonly List<IFonteSatisfacao> _fontes = new List<IFonteSatisfacao>();
        private float _satisfacao;
        private float _alvo;
        private float _timer;
        private EstadoCidade _estadoAtual;

        /// <summary>Índice global de satisfação atual (0–100).</summary>
        public float Satisfacao => _satisfacao;

        /// <summary>Estado discreto atual da cidade.</summary>
        public EstadoCidade Estado => _estadoAtual;

        /// <summary>
        /// Multiplicador de recompensa derivado do estado da cidade — em crise,
        /// "menos sucata" (GDD seção 07).
        /// </summary>
        public float MultiplicadorRecompensa
        {
            get
            {
                switch (_estadoAtual)
                {
                    case EstadoCidade.Satisfeita: return 1.0f;
                    case EstadoCidade.Preocupada: return 0.85f;
                    case EstadoCidade.EmCrise: return 0.6f;
                    case EstadoCidade.Revoltada: return 0.4f;
                    case EstadoCidade.Abandonada: return 0.25f;
                    default: return 1f;
                }
            }
        }

        /// <summary>
        /// Multiplicador de geração de lixo derivado do estado — quanto mais
        /// insatisfeita, mais lixo se acumula (espiral de crise controlada).
        /// </summary>
        public float MultiplicadorLixoEstado
        {
            get
            {
                switch (_estadoAtual)
                {
                    case EstadoCidade.Satisfeita: return 1.0f;
                    case EstadoCidade.Preocupada: return 1.1f;
                    case EstadoCidade.EmCrise: return 1.25f;
                    case EstadoCidade.Revoltada: return 1.4f;
                    case EstadoCidade.Abandonada: return 1.6f;
                    default: return 1f;
                }
            }
        }

        protected override void AoInicializar()
        {
            _satisfacao = satisfacaoInicial;
            _alvo = satisfacaoInicial;
            _estadoAtual = CalcularEstado(_satisfacao);
        }

        private void Start()
        {
            // Publica o estado inicial após todos os Awake/registros.
            GameEvents.DispararSatisfacaoAlterada(_satisfacao);
            GameEvents.DispararEstadoCidadeAlterado(_estadoAtual);
        }

        /// <summary>Registra uma fonte (bairro) no cálculo global.</summary>
        public void Registrar(IFonteSatisfacao fonte)
        {
            if (fonte != null && !_fontes.Contains(fonte))
                _fontes.Add(fonte);
        }

        /// <summary>Remove uma fonte do cálculo global.</summary>
        public void Remover(IFonteSatisfacao fonte)
        {
            _fontes.Remove(fonte);
        }

        [Header("Despacho")]
        [Tooltip("Lixo pendente (somado dos pontos) que zera a limpeza da cidade.")]
        [SerializeField] private float pendenteParaCrise = 500f;

        private int _pendenteExterno;
        private bool _usaPendente;

        /// <summary>
        /// Reporta o total de lixo pendente nos pontos de coleta. Passa a satisfação
        /// a ser dirigida pela limpeza (despacho) quando não há bairros registrados.
        /// </summary>
        public void ReportarPendente(int total)
        {
            _pendenteExterno = total;
            _usaPendente = true;
        }

        /// <summary>
        /// Aplica um delta imediato à satisfação (ex.: evento Mutirão Escolar +10%).
        /// </summary>
        public void AplicarDelta(float delta)
        {
            _satisfacao = Mathf.Clamp(_satisfacao + delta, 0f, 100f);
            AtualizarEstadoSeMudou();
            GameEvents.DispararSatisfacaoAlterada(_satisfacao);
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= intervaloRecalculo)
            {
                _timer = 0f;
                _alvo = CalcularAlvo();
            }

            float anterior = _satisfacao;
            _satisfacao = Mathf.MoveTowards(_satisfacao, _alvo, velocidadeDrift * Time.deltaTime);

            if (!Mathf.Approximately(anterior, _satisfacao))
            {
                AtualizarEstadoSeMudou();
                GameEvents.DispararSatisfacaoAlterada(_satisfacao);
            }
        }

        /// <summary>Média de limpeza ponderada pela população, em escala 0–100.</summary>
        private float CalcularAlvo()
        {
            if (_fontes.Count == 0)
            {
                // Modo despacho: limpeza cai conforme o lixo pendente nos pontos.
                if (_usaPendente)
                    return 100f - Mathf.Clamp01(_pendenteExterno / Mathf.Max(1f, pendenteParaCrise)) * 100f;
                return _satisfacao; // sem fontes nem pendente: mantém valor atual
            }

            float somaPeso = 0f;
            float somaPonderada = 0f;
            for (int i = 0; i < _fontes.Count; i++)
            {
                float peso = Mathf.Max(0.01f, _fontes[i].PesoPopulacional);
                somaPeso += peso;
                somaPonderada += Mathf.Clamp01(_fontes[i].Limpeza01) * peso;
            }

            return (somaPonderada / somaPeso) * 100f;
        }

        private void AtualizarEstadoSeMudou()
        {
            var novo = CalcularEstado(_satisfacao);
            if (novo != _estadoAtual)
            {
                _estadoAtual = novo;
                GameEvents.DispararEstadoCidadeAlterado(_estadoAtual);
            }
        }

        /// <summary>Mapeia a satisfação numérica para o estado discreto (GDD seção 07).</summary>
        public static EstadoCidade CalcularEstado(float satisfacao)
        {
            if (satisfacao >= 85f) return EstadoCidade.Satisfeita;
            if (satisfacao >= 60f) return EstadoCidade.Preocupada;
            if (satisfacao >= 35f) return EstadoCidade.EmCrise;
            if (satisfacao >= 10f) return EstadoCidade.Revoltada;
            return EstadoCidade.Abandonada;
        }
    }
}
