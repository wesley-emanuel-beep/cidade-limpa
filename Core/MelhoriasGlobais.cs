using UnityEngine;

namespace CidadeLimpa.Core
{
    public enum TipoMelhoria { Capacidade, Velocidade, Valor, Descarga, Automacao }

    /// <summary>
    /// Melhorias globais que valem para TODA a frota (caminhões são idênticos no
    /// modo despacho). Compradas na Loja; os caminhões leem os bônus em runtime.
    /// "Automação" libera caminhões que se despacham sozinhos (1 por nível).
    /// </summary>
    public class MelhoriasGlobais : Singleton<MelhoriasGlobais>
    {
        [Header("Bônus por nível")]
        [SerializeField] private int capacidadePorNivel = 25;
        [SerializeField] private float velocidadePorNivel = 0.18f;
        [SerializeField] private float valorPorNivel = 0.25f;
        [SerializeField] private float descargaPorNivel = 0.25f;

        [Header("Limites e custo")]
        [SerializeField] private int nivelMaximo = 8;
        [SerializeField] private long custoBase = 120;
        [SerializeField] private float fatorCusto = 1.7f;

        [Header("Automação (despacho sozinho)")]
        [SerializeField] private int nivelMaximoAutomacao = 6;
        [SerializeField] private long custoBaseAutomacao = 600;
        [SerializeField] private float fatorCustoAutomacao = 2.1f;

        private int _nCap, _nVel, _nValor, _nDescarga, _nAuto;

        public int BonusCapacidade => _nCap * capacidadePorNivel;
        public float MultVelocidade => 1f + _nVel * velocidadePorNivel;
        public float MultValor => 1f + _nValor * valorPorNivel;
        public float MultDescarga => 1f + _nDescarga * descargaPorNivel;

        /// <summary>Quantos caminhões podem operar sozinhos simultaneamente.</summary>
        public int Automacao => _nAuto;

        public int Nivel(TipoMelhoria t)
        {
            switch (t)
            {
                case TipoMelhoria.Capacidade: return _nCap;
                case TipoMelhoria.Velocidade: return _nVel;
                case TipoMelhoria.Valor: return _nValor;
                case TipoMelhoria.Descarga: return _nDescarga;
                case TipoMelhoria.Automacao: return _nAuto;
                default: return 0;
            }
        }

        public int MaximoDe(TipoMelhoria t) =>
            t == TipoMelhoria.Automacao ? nivelMaximoAutomacao : nivelMaximo;

        public bool NoMaximo(TipoMelhoria t) => Nivel(t) >= MaximoDe(t);

        public long Custo(TipoMelhoria t)
        {
            if (t == TipoMelhoria.Automacao)
                return (long)(custoBaseAutomacao * Mathf.Pow(fatorCustoAutomacao, _nAuto));
            return (long)(custoBase * Mathf.Pow(fatorCusto, Nivel(t)));
        }

        /// <summary>Tenta comprar um nível de melhoria. Retorna true se efetivou.</summary>
        public bool Comprar(TipoMelhoria t)
        {
            if (NoMaximo(t)) return false;
            long custo = Custo(t);
            if (!SucataManager.Existe || !SucataManager.Instancia.Gastar(custo)) return false;

            switch (t)
            {
                case TipoMelhoria.Capacidade: _nCap++; break;
                case TipoMelhoria.Velocidade: _nVel++; break;
                case TipoMelhoria.Valor: _nValor++; break;
                case TipoMelhoria.Descarga: _nDescarga++; break;
                case TipoMelhoria.Automacao: _nAuto++; break;
            }
            return true;
        }

        public static string Nome(TipoMelhoria t)
        {
            switch (t)
            {
                case TipoMelhoria.Capacidade: return "CAPACIDADE";
                case TipoMelhoria.Velocidade: return "VELOCIDADE";
                case TipoMelhoria.Valor: return "VALOR DO LIXO";
                case TipoMelhoria.Descarga: return "DESCARGA RÁPIDA";
                case TipoMelhoria.Automacao: return "DESPACHO AUTOMÁTICO";
                default: return t.ToString();
            }
        }
    }
}
