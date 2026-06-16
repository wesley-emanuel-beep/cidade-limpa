using System.Collections.Generic;
using UnityEngine;
using CidadeLimpa.Core;
using CidadeLimpa.Data;
using CidadeLimpa.Coleta;

namespace CidadeLimpa.Caminhoes
{
    /// <summary>
    /// Caminhão de coleta (modo DESPACHO). O jogador monta a rota; o caminhão a
    /// percorre, coleta até encher e, ao terminar o objetivo (ou encher), volta
    /// automaticamente ao depósito, descarrega (vira dinheiro) e some — fica
    /// estacionado/oculto até receber nova ordem. Lê os upgrades globais.
    /// </summary>
    public class Caminhao : MonoBehaviour
    {
        [Header("Configuração")]
        [SerializeField] private CaminhaoData dados;
        [SerializeField] private float distanciaChegada = 0.18f;
        [SerializeField] private int loteColeta = 8;

        private Transform _modelo;
        private EstadoCaminhao _estado = EstadoCaminhao.Ocioso;

        private readonly List<PontoColeta> _rota = new List<PontoColeta>();
        private int _carga;
        private long _sucataBase;
        private float _timer;

        public int Id { get; private set; } = -1;
        public CaminhaoData Dados => dados;
        public EstadoCaminhao Estado => _estado;
        public int Carga => _carga;
        public int RoteiroCount => _rota.Count;
        public bool Ativo => gameObject.activeSelf;
        public IReadOnlyList<PontoColeta> Rota => _rota;

        public float Velocidade =>
            (dados != null ? dados.velocidade : 4f) * (MelhoriasGlobais.Existe ? MelhoriasGlobais.Instancia.MultVelocidade : 1f);
        public int Capacidade =>
            (dados != null ? dados.capacidade : 100) + (MelhoriasGlobais.Existe ? MelhoriasGlobais.Instancia.BonusCapacidade : 0);
        public float CargaNormalizada => Capacidade > 0 ? (float)_carga / Capacidade : 0f;
        public bool Cheio => _carga >= Capacidade;

        private void Awake()
        {
            if (transform.childCount > 0) _modelo = transform.GetChild(0);
        }

        public void Inicializar(int id, CaminhaoData novoDados)
        {
            Id = id;
            dados = novoDados;
            name = $"Caminhao_{id}";
        }

        // ------------------------------------------------------------------
        //  Ordens do jogador
        // ------------------------------------------------------------------

        public void AdicionarAoRoteiro(PontoColeta ponto)
        {
            if (ponto == null || Cheio) return;
            if (_rota.Contains(ponto)) return;

            ponto.DefinirAtribuido(Id);
            _rota.Add(ponto);

            if (!gameObject.activeSelf)
            {
                transform.position = Deposito.Existe ? Deposito.Instancia.Posicao : transform.position;
                gameObject.SetActive(true);
            }
            if (_estado == EstadoCaminhao.Ocioso) IniciarProximo();
            GameEvents.DispararCaminhaoAtualizado(Id);
        }

        /// <summary>Remove um ponto da rota sem desatribuí-lo (usado em reatribuição).</summary>
        public void RemoverDoRoteiro(PontoColeta ponto)
        {
            _rota.Remove(ponto);
            GameEvents.DispararCaminhaoAtualizado(Id);
        }

        /// <summary>Manda voltar agora ao depósito; abandona a rota restante.</summary>
        public void EnviarAoDeposito()
        {
            if (!gameObject.activeSelf) return;
            if (_estado == EstadoCaminhao.Descarregando) return;
            LiberarRotaRestante();
            TrocarEstado(EstadoCaminhao.Retornando);
        }

        // ------------------------------------------------------------------
        //  Máquina de estados
        // ------------------------------------------------------------------

        private void Update()
        {
            if (GameManager.Existe && GameManager.Instancia.Pausado) return;
            if (dados == null) return;

            switch (_estado)
            {
                case EstadoCaminhao.EmRota: AtualizarEmRota(); break;
                case EstadoCaminhao.Coletando: AtualizarColetando(); break;
                case EstadoCaminhao.Retornando: AtualizarRetornando(); break;
                case EstadoCaminhao.Descarregando: AtualizarDescarregando(); break;
            }
        }

        private void IniciarProximo()
        {
            DescartarInvalidos();
            if (_rota.Count == 0) { TrocarEstado(EstadoCaminhao.Retornando); return; }
            TrocarEstado(EstadoCaminhao.EmRota);
        }

        private void AtualizarEmRota()
        {
            DescartarInvalidos();
            if (_rota.Count == 0) { TrocarEstado(EstadoCaminhao.Retornando); return; }

            if (MoverPara(_rota[0].Posicao))
            {
                _timer = 0f;
                TrocarEstado(EstadoCaminhao.Coletando);
            }
        }

        private void AtualizarColetando()
        {
            DescartarInvalidos();
            if (_rota.Count == 0 || Cheio) { DecidirAposColeta(); return; }

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            var ponto = _rota[0];
            int restante = Capacidade - _carga;
            int coletado = ponto.Coletar(Mathf.Min(restante, loteColeta));
            if (coletado > 0)
            {
                _carga += coletado;
                _sucataBase += (long)coletado * ponto.ValorPorUnidade;
                _timer = (dados != null ? dados.tempoColetaPorUnidade : 0.05f) * coletado;
                GameEvents.DispararCaminhaoAtualizado(Id);
            }

            if (ponto.Quantidade <= 0) { _rota.Remove(ponto); DecidirAposColeta(); }
            else if (Cheio) DecidirAposColeta();
        }

        private void DecidirAposColeta()
        {
            if (Cheio) { TrocarEstado(EstadoCaminhao.Retornando); return; }
            DescartarInvalidos();
            TrocarEstado(_rota.Count > 0 ? EstadoCaminhao.EmRota : EstadoCaminhao.Retornando);
        }

        private void AtualizarRetornando()
        {
            Vector3 destino = Deposito.Existe ? Deposito.Instancia.Posicao : Vector3.zero;
            if (MoverPara(destino))
            {
                _timer = (dados != null ? dados.tempoDescarregamento : 2f)
                       / (MelhoriasGlobais.Existe ? MelhoriasGlobais.Instancia.MultDescarga : 1f);
                TrocarEstado(EstadoCaminhao.Descarregando);
            }
        }

        private void AtualizarDescarregando()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            if (_sucataBase > 0 && Deposito.Existe)
                Deposito.Instancia.ProcessarDescarga(_sucataBase, dados != null ? dados.eficiencia : 1f, false);

            _carga = 0;
            _sucataBase = 0;
            EntrarNoDeposito();
        }

        /// <summary>Estaciona e oculta o caminhão no depósito (ocioso).</summary>
        private void EntrarNoDeposito()
        {
            LiberarRotaRestante();
            _estado = EstadoCaminhao.Ocioso;
            if (Deposito.Existe) transform.position = Deposito.Instancia.Posicao;
            GameEvents.DispararCaminhaoAtualizado(Id);
            gameObject.SetActive(false); // some no depósito
        }

        private void LiberarRotaRestante()
        {
            for (int i = 0; i < _rota.Count; i++)
                if (_rota[i] != null) _rota[i].DefinirAtribuido(-1);
            _rota.Clear();
        }

        private void DescartarInvalidos()
        {
            for (int i = _rota.Count - 1; i >= 0; i--)
                if (_rota[i] == null || _rota[i].Quantidade <= 0 || !_rota[i].gameObject.activeSelf)
                    _rota.RemoveAt(i);
        }

        // ------------------------------------------------------------------
        //  Movimento direto + orientação
        // ------------------------------------------------------------------

        private bool MoverPara(Vector3 alvo)
        {
            alvo.z = transform.position.z;
            Vector3 dir = alvo - transform.position;
            float dist = dir.magnitude;
            if (dist <= distanciaChegada) return true;

            Vector3 passo = dir.normalized * Velocidade * Time.deltaTime;
            if (passo.magnitude >= dist) { transform.position = alvo; return true; }
            transform.position += passo;
            OrientarModelo(dir);
            return false;
        }

        private void OrientarModelo(Vector3 direcao)
        {
            if (_modelo == null || direcao.sqrMagnitude < 0.0001f) return;
            float ang = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
            var alvo = Quaternion.Euler(0f, 0f, ang - 90f);
            _modelo.rotation = Quaternion.Slerp(_modelo.rotation, alvo, 10f * Time.deltaTime);
        }

        private void TrocarEstado(EstadoCaminhao novo)
        {
            if (_estado == novo) return;
            _estado = novo;
            GameEvents.DispararCaminhaoAtualizado(Id);
        }
    }
}
