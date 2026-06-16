using System.Collections.Generic;
using UnityEngine;
using CidadeLimpa.Core;
using CidadeLimpa.Data;
using CidadeLimpa.Lixo;
using CidadeLimpa.Mapa;

namespace CidadeLimpa.Bairros
{
    /// <summary>
    /// Representa um bairro de Tubarão no mapa (GDD seção 03). Mantém o conjunto
    /// de resíduos presentes, calcula a limpeza local e contribui para a
    /// satisfação global via <see cref="IFonteSatisfacao"/>.
    /// </summary>
    public class Bairro : MonoBehaviour, IFonteSatisfacao
    {
        /// <summary>Todos os bairros ativos na cena (registro estático leve).</summary>
        public static readonly List<Bairro> Todos = new List<Bairro>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetarRegistro() => Todos.Clear();

        [SerializeField] private BairroData dados;

        [Tooltip("Índice deste bairro no BancoDeDados (fallback se 'dados' faltar).")]
        [SerializeField] private int indiceBanco = -1;

        private readonly List<LixoItem> _lixos = new List<LixoItem>();
        private BairroVisual _visual;

        /// <summary>Dados de configuração deste bairro.</summary>
        public BairroData Dados => dados;

        /// <summary>Nome de exibição.</summary>
        public string Nome => dados != null ? dados.nomeBairro : name;

        /// <summary>Quantidade de resíduos atualmente acumulados.</summary>
        public int QuantidadeLixo => _lixos.Count;

        /// <summary>Posição central do bairro no mundo.</summary>
        public Vector3 Centro => transform.position;

        /// <summary>Distância de arco do bairro ao longo da estrada (destino do caminhão).</summary>
        public float DistanciaEstrada { get; private set; }

        // ---- IFonteSatisfacao ----
        public float Limpeza01
        {
            get
            {
                if (dados == null) return 1f;
                float t = Mathf.Clamp01((float)_lixos.Count / Mathf.Max(1, dados.limiteCritico));
                return 1f - t;
            }
        }

        public float PesoPopulacional => dados != null ? Mathf.Max(0.01f, dados.populacao) : 1f;

        private void Awake()
        {
            _visual = GetComponent<BairroVisual>();

            // Fallback: se a cena não atribuiu 'dados', busca no banco pelo índice.
            if (dados == null && indiceBanco >= 0)
            {
                var banco = BancoDeDados.Carregar();
                if (banco != null && indiceBanco < banco.bairros.Count)
                    dados = banco.bairros[indiceBanco];
            }
        }

        private void OnEnable()
        {
            if (!Todos.Contains(this)) Todos.Add(this);
            if (SatisfacaoManager.Existe) SatisfacaoManager.Instancia.Registrar(this);
        }

        private void Start()
        {
            // Registro tardio caso o manager tenha inicializado depois.
            if (SatisfacaoManager.Existe) SatisfacaoManager.Instancia.Registrar(this);

            // Ancora o bairro à estrada (distância de arco do ponto mais próximo).
            if (RoadPath.Existe && RoadPath.Instancia.Valida)
                DistanciaEstrada = RoadPath.Instancia.ProjetarDistancia(Centro);
        }

        private void OnDisable()
        {
            Todos.Remove(this);
            if (SatisfacaoManager.Existe) SatisfacaoManager.Instancia.Remover(this);
        }

        /// <summary>Aplica os dados (usado pelo Scene Builder ao montar a cena).</summary>
        public void Configurar(BairroData novoDados)
        {
            dados = novoDados;
            if (dados != null)
            {
                transform.position = new Vector3(dados.posicaoMapa.x, dados.posicaoMapa.y, 0f);
                name = "Bairro_" + dados.nomeBairro;
            }
        }

        /// <summary>Posição aleatória dentro do raio do bairro (para spawn de lixo).</summary>
        public Vector3 PosicaoAleatoria()
        {
            float raio = dados != null ? dados.raio : 2f;
            Vector2 offset = Random.insideUnitCircle * raio;
            return Centro + new Vector3(offset.x, offset.y, 0f);
        }

        /// <summary>Registra um novo resíduo neste bairro.</summary>
        public void AdicionarLixo(LixoItem item)
        {
            if (item == null) return;
            _lixos.Add(item);
            item.transform.SetParent(transform, true);
        }

        /// <summary>
        /// Remove e coleta até <paramref name="capacidade"/> unidades de volume de
        /// resíduo, devolvendo-os ao pool. Retorna a sucata base somada e preenche
        /// estatísticas de coleta.
        /// </summary>
        public ResultadoColeta Coletar(int capacidade)
        {
            var resultado = new ResultadoColeta();
            if (capacidade <= 0 || _lixos.Count == 0) return resultado;

            int volumeAcumulado = 0;
            // Coleta priorizando os resíduos mais antigos (início da lista).
            for (int i = 0; i < _lixos.Count && volumeAcumulado < capacidade;)
            {
                var item = _lixos[i];
                if (item == null) { _lixos.RemoveAt(i); continue; }

                if (volumeAcumulado + item.Volume > capacidade)
                {
                    // Não cabe inteiro: para respeitar capacidade, encerra aqui.
                    break;
                }

                volumeAcumulado += item.Volume;
                resultado.unidades += item.Volume;
                resultado.sucataBase += item.ValorSucataBase;
                if (item.Tipo == TipoLixo.Reciclavel) resultado.recolhidosReciclaveis++;

                _lixos.RemoveAt(i);
                item.Liberar();
            }

            return resultado;
        }

        private void OnDrawGizmosSelected()
        {
            float raio = dados != null ? dados.raio : 2f;
            Gizmos.color = dados != null ? dados.corIdentidade : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, raio);
        }
    }

    /// <summary>Resultado de uma operação de coleta em um bairro.</summary>
    public struct ResultadoColeta
    {
        public int unidades;
        public long sucataBase;
        public int recolhidosReciclaveis;

        public bool Vazio => unidades == 0;
    }
}
