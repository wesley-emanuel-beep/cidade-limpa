using UnityEngine;

namespace CidadeLimpa.Core
{
    /// <summary>
    /// Gerencia a moeda principal do jogo — Sucata (GDD seção 06).
    /// Centraliza ganho e gasto, aplicando os multiplicadores ativos de evento
    /// (Campanha Ambiental) e do estado da cidade (crise reduz recompensa).
    /// </summary>
    public class SucataManager : Singleton<SucataManager>
    {
        [Header("Saldo")]
        [SerializeField] private long saldoInicial = 0;

        /// <summary>Saldo atual de sucata disponível para gastar.</summary>
        public long Saldo { get; private set; }

        /// <summary>Total acumulado ganho na sessão — usado pela progressão de dificuldade.</summary>
        public long TotalGanho { get; private set; }

        protected override void AoInicializar()
        {
            Saldo = saldoInicial;
            GameEvents.DispararSucataAlterada(Saldo);
        }

        private void Start()
        {
            // Rebroadcast após todos os Awake — garante a UI sincronizada
            // independentemente da ordem de inicialização.
            GameEvents.DispararSucataAlterada(Saldo);
        }

        /// <summary>
        /// Concede sucata a partir de um valor base, aplicando os multiplicadores
        /// globais ativos (evento de sucata × estado da cidade).
        /// </summary>
        /// <param name="valorBase">Sucata bruta antes dos multiplicadores globais.</param>
        /// <param name="posicaoMundo">Posição para feedback visual flutuante.</param>
        /// <param name="aplicarMultiplicadores">Se false, concede o valor exato (ex.: bônus de minigame já calculado).</param>
        public void Ganhar(long valorBase, Vector3 posicaoMundo, bool aplicarMultiplicadores = true)
        {
            if (valorBase <= 0) return;

            float mult = 1f;
            if (aplicarMultiplicadores)
            {
                if (EventoManager.Existe) mult *= EventoManager.Instancia.MultiplicadorSucata;
                if (SatisfacaoManager.Existe) mult *= SatisfacaoManager.Instancia.MultiplicadorRecompensa;
            }

            long valorFinal = (long)Mathf.Max(1f, valorBase * mult);

            Saldo += valorFinal;
            TotalGanho += valorFinal;

            GameEvents.DispararSucataAlterada(Saldo);
            GameEvents.DispararSucataGanha(valorFinal, posicaoMundo);
        }

        /// <summary>True se o jogador possui saldo suficiente para um custo.</summary>
        public bool PodePagar(long custo) => Saldo >= custo;

        /// <summary>
        /// Tenta debitar um custo. Retorna false (sem alterar saldo) se insuficiente.
        /// </summary>
        public bool Gastar(long custo)
        {
            if (custo < 0)
            {
                Debug.LogError($"[SucataManager] Tentativa de gastar valor negativo: {custo}");
                return false;
            }

            if (!PodePagar(custo))
                return false;

            Saldo -= custo;
            GameEvents.DispararSucataAlterada(Saldo);
            return true;
        }
    }
}
