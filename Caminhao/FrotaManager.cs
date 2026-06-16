using System.Collections.Generic;
using UnityEngine;
using CidadeLimpa.Core;
using CidadeLimpa.Data;
using CidadeLimpa.Coleta;

namespace CidadeLimpa.Caminhoes
{
    /// <summary>
    /// Gerencia a frota no modo DESPACHO: instancia caminhões, processa compras e
    /// resolve a interação (selecionar caminhão → clicar num ponto de coleta para
    /// adicioná-lo à rota daquele caminhão). É a fonte de verdade da seleção.
    /// </summary>
    public class FrotaManager : Singleton<FrotaManager>
    {
        [Header("Spawn")]
        [SerializeField] private GameObject prefabCaminhao;
        [SerializeField] private Sprite spriteCaminhao;
        [SerializeField] private float escalaCaminhao = 0.6f;
        [SerializeField] private CaminhaoData caminhaoInicial;

        [Header("Economia")]
        [Tooltip("Custo do 2º caminhão; cresce a cada compra.")]
        [SerializeField] private long custoBaseCaminhao = 200;
        [SerializeField] private float fatorCustoCaminhao = 1.8f;

        [Header("Interação")]
        [SerializeField] private float raioSelecaoCaminhao = 1.4f;
        [SerializeField] private float raioCliquePonto = 1.1f;

        private readonly List<Caminhao> _frota = new List<Caminhao>();
        private int _proximoId;
        private Caminhao _selecionado;

        public IReadOnlyList<Caminhao> Frota => _frota;
        public Caminhao Selecionado => _selecionado;

        /// <summary>Custo do próximo caminhão a comprar (cresce com a frota).</summary>
        public long CustoProximoCaminhao =>
            (long)(custoBaseCaminhao * Mathf.Pow(fatorCustoCaminhao, Mathf.Max(0, _frota.Count - 1)));

        private void Start()
        {
            if (caminhaoInicial == null)
            {
                var banco = BancoDeDados.Carregar();
                if (banco != null && banco.caminhoes.Count > 0) caminhaoInicial = banco.caminhoes[0];
            }
            if (caminhaoInicial == null)
            {
                Debug.LogWarning("[FrotaManager] Sem 'caminhaoInicial' nem banco — nenhum caminhão.");
                return;
            }
            if (_frota.Count == 0) CriarCaminhao(caminhaoInicial);
        }

        // ------------------------------------------------------------------
        //  Compra / criação
        // ------------------------------------------------------------------

        /// <summary>Compra um caminhão adicional pelo custo escalonado.</summary>
        public Caminhao ComprarCaminhaoAdicional()
        {
            if (caminhaoInicial == null) return null;
            long custo = CustoProximoCaminhao;
            if (!SucataManager.Existe || !SucataManager.Instancia.Gastar(custo))
                return null;
            return CriarCaminhao(caminhaoInicial);
        }

        private Caminhao CriarCaminhao(CaminhaoData dados)
        {
            Vector3 pos = Deposito.Existe ? Deposito.Instancia.Posicao : Vector3.zero;

            GameObject go;
            if (prefabCaminhao != null) go = Instantiate(prefabCaminhao, pos, Quaternion.identity, transform);
            else if (spriteCaminhao != null) go = ConstruirCaminhaoSprite(dados, pos);
            else { go = ConstrutorCaminhaoPlaceholder.Criar(dados, transform); go.transform.position = pos; }

            var caminhao = go.GetComponent<Caminhao>() ?? go.AddComponent<Caminhao>();
            if (go.GetComponent<CaminhaoSelecao>() == null) go.AddComponent<CaminhaoSelecao>();
            if (go.GetComponent<RotaRenderer>() == null) go.AddComponent<RotaRenderer>();

            caminhao.Inicializar(_proximoId++, dados);
            _frota.Add(caminhao);
            go.SetActive(false); // estacionado/oculto no depósito até ser despachado

            GameEvents.DispararFrotaAlterada(_frota.Count);
            return caminhao;
        }

        private GameObject ConstruirCaminhaoSprite(CaminhaoData dados, Vector3 pos)
        {
            var raiz = new GameObject("Caminhao");
            raiz.transform.SetParent(transform);
            raiz.transform.position = pos;

            var modelo = new GameObject("Modelo");
            modelo.transform.SetParent(raiz.transform, false);
            modelo.transform.localScale = Vector3.one * escalaCaminhao;

            var spriteGO = new GameObject("Sprite");
            spriteGO.transform.SetParent(modelo.transform, false);
            var sr = spriteGO.AddComponent<SpriteRenderer>();
            sr.sprite = spriteCaminhao;
            sr.sortingOrder = 20;
            spriteGO.AddComponent<ModeloAnimador>();

            return raiz;
        }

        // ------------------------------------------------------------------
        //  Interação (chamada pelo InputManager)
        // ------------------------------------------------------------------

        /// <summary>
        /// Clique no mundo: (1) seleciona caminhão próximo; (2) com caminhão
        /// selecionado, clicar num ponto de coleta o adiciona à rota.
        /// Retorna true se consumiu o clique.
        /// </summary>
        public bool ProcessarClique(Vector3 posicaoMundo)
        {
            var caminhao = CaminhaoMaisProximo(posicaoMundo, raioSelecaoCaminhao);
            if (caminhao != null) { Selecionar(caminhao); return true; }

            var ponto = PontoMaisProximo(posicaoMundo, raioCliquePonto);
            if (ponto != null && _selecionado != null)
            {
                // Reatribui: se outro caminhão já cuidava do ponto, tira da rota dele.
                if (ponto.AtribuidoId != -1 && ponto.AtribuidoId != _selecionado.Id)
                {
                    var antigo = BuscarPorId(ponto.AtribuidoId);
                    if (antigo != null) antigo.RemoverDoRoteiro(ponto);
                }
                _selecionado.AdicionarAoRoteiro(ponto);
                return true;
            }

            return false;
        }

        public void Selecionar(Caminhao caminhao)
        {
            if (_selecionado == caminhao) return;
            LimparSelecao();
            _selecionado = caminhao;
            var sel = caminhao != null ? caminhao.GetComponent<CaminhaoSelecao>() : null;
            if (sel != null) sel.DefinirSelecionado(true);
            RealcarPontos(true);
        }

        public void SelecionarPorId(int id)
        {
            for (int i = 0; i < _frota.Count; i++)
                if (_frota[i].Id == id) { Selecionar(_frota[i]); return; }
        }

        public void LimparSelecao()
        {
            if (_selecionado == null) return;
            var sel = _selecionado.GetComponent<CaminhaoSelecao>();
            if (sel != null) sel.DefinirSelecionado(false);
            _selecionado = null;
            RealcarPontos(false);
        }

        /// <summary>Envia o caminhão selecionado de volta ao depósito.</summary>
        public void EnviarSelecionadoAoDeposito()
        {
            if (_selecionado != null) _selecionado.EnviarAoDeposito();
        }

        private void RealcarPontos(bool valor)
        {
            for (int i = 0; i < PontoColeta.Ativos.Count; i++)
                PontoColeta.Ativos[i].DefinirRealce(valor);
        }

        private float _timerAuto;

        private void Update()
        {
            // Mantém novos pontos realçados enquanto há caminhão selecionado.
            if (_selecionado != null) RealcarPontos(true);
            AtualizarAutomacao();
        }

        /// <summary>Despacha caminhões ociosos sozinhos, até o nível de Automação.</summary>
        private void AtualizarAutomacao()
        {
            if (!MelhoriasGlobais.Existe) return;
            int auto = MelhoriasGlobais.Instancia.Automacao;
            if (auto <= 0) return;
            if (GameManager.Existe && GameManager.Instancia.Pausado) return;

            _timerAuto += Time.deltaTime;
            if (_timerAuto < 0.5f) return;
            _timerAuto = 0f;

            int trabalhando = 0;
            for (int i = 0; i < _frota.Count; i++) if (_frota[i].Ativo) trabalhando++;

            for (int i = 0; i < _frota.Count && trabalhando < auto; i++)
            {
                if (_frota[i].Ativo) continue; // já está em serviço
                var ponto = MelhorPontoLivre();
                if (ponto == null) break;
                _frota[i].AdicionarAoRoteiro(ponto); // ativa e despacha sozinho
                trabalhando++;
            }
        }

        private PontoColeta MelhorPontoLivre()
        {
            PontoColeta melhor = null;
            long melhorValor = -1;
            var lista = PontoColeta.Ativos;
            for (int i = 0; i < lista.Count; i++)
                if (!lista[i].Atribuido && lista[i].Recompensa > melhorValor)
                {
                    melhorValor = lista[i].Recompensa;
                    melhor = lista[i];
                }
            return melhor;
        }

        private Caminhao BuscarPorId(int id)
        {
            for (int i = 0; i < _frota.Count; i++)
                if (_frota[i].Id == id) return _frota[i];
            return null;
        }

        private Caminhao CaminhaoMaisProximo(Vector3 ponto, float raio)
        {
            Caminhao melhor = null;
            float menor = raio * raio;
            for (int i = 0; i < _frota.Count; i++)
            {
                if (!_frota[i].Ativo) continue; // ocioso/oculto no depósito: seleção pelo cartão
                Vector3 p = _frota[i].transform.position; p.z = ponto.z;
                float d = (p - ponto).sqrMagnitude;
                if (d <= menor) { menor = d; melhor = _frota[i]; }
            }
            return melhor;
        }

        private PontoColeta PontoMaisProximo(Vector3 ponto, float raio)
        {
            PontoColeta melhor = null;
            float menor = raio * raio;
            for (int i = 0; i < PontoColeta.Ativos.Count; i++)
            {
                Vector3 p = PontoColeta.Ativos[i].Posicao; p.z = ponto.z;
                float d = (p - ponto).sqrMagnitude;
                if (d <= menor) { menor = d; melhor = PontoColeta.Ativos[i]; }
            }
            return melhor;
        }
    }
}
