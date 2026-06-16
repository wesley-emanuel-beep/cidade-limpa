using UnityEngine;

namespace CidadeLimpa.Core
{
    /// <summary>
    /// Orquestrador de alto nível do jogo. Não acumula responsabilidades de
    /// gameplay (delegadas aos managers especializados): coordena pausa global,
    /// estado de execução e serve como ponto único de configuração geral.
    /// O jogo é idle e não possui game over (GDD seção 02).
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [Header("Estado")]
        [SerializeField] private bool iniciarPausado = false;

        /// <summary>True quando o jogo está pausado (menus, etc.).</summary>
        public bool Pausado { get; private set; }

        protected override void AoInicializar()
        {
            Pausado = iniciarPausado;
            Application.targetFrameRate = 60; // estável em mobile e desktop
        }

        /// <summary>Alterna o estado de pausa global (Time.timeScale).</summary>
        public void AlternarPausa() => DefinirPausa(!Pausado);

        /// <summary>Define o estado de pausa global.</summary>
        public void DefinirPausa(bool pausado)
        {
            Pausado = pausado;
            Time.timeScale = pausado ? 0f : 1f;
        }

        private void OnApplicationPause(bool pausa)
        {
            // Em mobile, ao perder foco mantemos o timeScale coerente.
            if (pausa && !Pausado) Time.timeScale = 0f;
            else if (!pausa && !Pausado) Time.timeScale = 1f;
        }
    }
}
