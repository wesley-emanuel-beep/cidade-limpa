using UnityEngine;
using CidadeLimpa.Core;
using CidadeLimpa.Mapa;

namespace CidadeLimpa.Caminhoes
{
    /// <summary>
    /// Ponto central de descarga (GDD seção 06). Converte a carga de lixo dos
    /// caminhões em sucata, aplicando a eficiência do veículo, e publica o evento
    /// de conclusão de coleta para feedback (UI/áudio).
    /// </summary>
    public class Deposito : Singleton<Deposito>
    {
        [Header("Minigame")]
        [Tooltip("Chance (0–1) de disparar o minigame de reciclagem após uma descarga com recicláveis.")]
        [SerializeField, Range(0f, 1f)] private float chanceMinigame = 0.35f;

        [Tooltip("Fração da sucata base usada como bônus potencial do minigame.")]
        [SerializeField, Range(0f, 2f)] private float fatorBonusMinigame = 1f;

        /// <summary>Posição mundial do depósito (destino de retorno dos caminhões).</summary>
        public Vector3 Posicao => transform.position;

        /// <summary>Distância de arco do depósito ao longo da estrada (ancoragem na via).</summary>
        public float DistanciaEstrada { get; private set; }

        private void Start()
        {
            if (RoadPath.Existe && RoadPath.Instancia.Valida)
                DistanciaEstrada = RoadPath.Instancia.ProjetarDistancia(Posicao);
        }

        /// <summary>
        /// Processa a descarga de um caminhão: concede sucata (base × eficiência,
        /// já com multiplicadores globais aplicados pelo SucataManager) e, com
        /// chance, dispara o minigame de reciclagem.
        /// </summary>
        /// <param name="sucataBase">Sucata bruta somada dos resíduos coletados.</param>
        /// <param name="eficiencia">Multiplicador de eficiência do caminhão.</param>
        /// <param name="tinhaReciclaveis">Se a carga continha recicláveis (gatilho do minigame).</param>
        public void ProcessarDescarga(long sucataBase, float eficiencia, bool tinhaReciclaveis)
        {
            if (sucataBase <= 0) return;

            float multValor = MelhoriasGlobais.Existe ? MelhoriasGlobais.Instancia.MultValor : 1f;
            long valor = (long)Mathf.Max(1f, sucataBase * Mathf.Max(0.1f, eficiencia) * multValor);

            if (SucataManager.Existe)
                SucataManager.Instancia.Ganhar(valor, Posicao);

            GameEvents.DispararColetaConcluida(Posicao);

            if (tinhaReciclaveis && Random.value < chanceMinigame)
            {
                long bonusPotencial = (long)Mathf.Max(5f, valor * fatorBonusMinigame);
                GameEvents.DispararMinigameSolicitado(bonusPotencial);
            }
        }
    }
}
