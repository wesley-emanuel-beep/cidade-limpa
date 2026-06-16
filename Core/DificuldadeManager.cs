using UnityEngine;
using CidadeLimpa.Data;

namespace CidadeLimpa.Core
{
    /// <summary>
    /// Controla a progressão de dificuldade (GDD seção 10). Avança de nível com
    /// base na sucata total acumulada e expõe os parâmetros do nível atual
    /// (cadência de lixo, eventos habilitados, acúmulos críticos) para os demais
    /// sistemas consultarem.
    /// </summary>
    public class DificuldadeManager : Singleton<DificuldadeManager>
    {
        [Header("Configuração")]
        [SerializeField] private DificuldadeConfig config;

        /// <summary>Nível atual (1-based).</summary>
        public int NivelAtual { get; private set; } = 1;

        /// <summary>Parâmetros do nível atual.</summary>
        public DificuldadeConfig.Nivel Nivel { get; private set; }

        /// <summary>Configuração completa (para o EventoManager ler intervalo/chance).</summary>
        public DificuldadeConfig Config => config;

        private float _timerChecagem;
        private const float IntervaloChecagem = 1f;

        protected override void AoInicializar()
        {
            if (config == null)
            {
                var banco = BancoDeDados.Carregar();
                if (banco != null) config = banco.dificuldade;
            }

            if (config == null)
            {
                Debug.LogWarning("[DificuldadeManager] DificuldadeConfig não atribuído — usando configuração padrão em memória.");
                config = CriarConfigPadrao();
            }

            NivelAtual = 1;
            Nivel = config.ObterNivel(NivelAtual);
        }

        /// <summary>
        /// Cria uma configuração de dificuldade padrão (valores do GDD seção 10) em
        /// memória, garantindo que o jogo funcione mesmo se o asset não for ligado.
        /// </summary>
        private static DificuldadeConfig CriarConfigPadrao()
        {
            var c = ScriptableObject.CreateInstance<DificuldadeConfig>();
            c.niveis = new[]
            {
                new DificuldadeConfig.Nivel { nome = "Tranquilo", lixosPorTick = 1, intervaloSegundos = 6f, eventosHabilitados = false, acumulosCriticos = false, sucataParaAvancar = 300 },
                new DificuldadeConfig.Nivel { nome = "Normal",    lixosPorTick = 1, intervaloSegundos = 5f, eventosHabilitados = false, acumulosCriticos = false, sucataParaAvancar = 800 },
                new DificuldadeConfig.Nivel { nome = "Intenso",   lixosPorTick = 2, intervaloSegundos = 5f, eventosHabilitados = false, acumulosCriticos = false, sucataParaAvancar = 1800 },
                new DificuldadeConfig.Nivel { nome = "Dinâmico",  lixosPorTick = 2, intervaloSegundos = 4f, eventosHabilitados = true,  acumulosCriticos = false, sucataParaAvancar = 3500 },
                new DificuldadeConfig.Nivel { nome = "Caótico",   lixosPorTick = 3, intervaloSegundos = 3f, eventosHabilitados = true,  acumulosCriticos = true,  sucataParaAvancar = long.MaxValue },
            };
            c.intervaloTentativaEvento = 40f;
            c.chanceEvento = 0.6f;
            return c;
        }

        private void Start()
        {
            GameEvents.DispararNivelDificuldadeAlterado(NivelAtual);
        }

        private void Update()
        {
            if (config == null || !SucataManager.Existe) return;

            _timerChecagem += Time.deltaTime;
            if (_timerChecagem < IntervaloChecagem) return;
            _timerChecagem = 0f;

            TentarAvancar();
        }

        private void TentarAvancar()
        {
            // Já no último nível?
            if (NivelAtual >= config.TotalNiveis) return;

            long totalGanho = SucataManager.Instancia.TotalGanho;
            if (totalGanho >= Nivel.sucataParaAvancar)
            {
                NivelAtual++;
                Nivel = config.ObterNivel(NivelAtual);
                GameEvents.DispararNivelDificuldadeAlterado(NivelAtual);
                Debug.Log($"[DificuldadeManager] Avançou para o nível {NivelAtual} — {Nivel.nome}.");
            }
        }
    }
}
