using System;
using UnityEngine;

namespace CidadeLimpa.Data
{
    /// <summary>
    /// Configuração da progressão de dificuldade (GDD seção 10 — 5 níveis:
    /// Tranquilo → Normal → Intenso → Dinâmico → Caótico).
    /// </summary>
    [CreateAssetMenu(fileName = "DificuldadeConfig", menuName = "Cidade Limpa/Dificuldade", order = 3)]
    public class DificuldadeConfig : ScriptableObject
    {
        /// <summary>Parâmetros de um nível de dificuldade.</summary>
        [Serializable]
        public class Nivel
        {
            public string nome = "Tranquilo";

            [Tooltip("Quantidade de lixo gerada por tick global.")]
            public int lixosPorTick = 1;

            [Tooltip("Intervalo (segundos) entre ticks de geração de lixo.")]
            public float intervaloSegundos = 15f;

            [Tooltip("Eventos aleatórios podem ocorrer neste nível?")]
            public bool eventosHabilitados = false;

            [Tooltip("'Grande Acúmulo' (crise local) pode surgir neste nível?")]
            public bool acumulosCriticos = false;

            [Tooltip("Sucata necessária acumulada (total ganho) para avançar ao próximo nível.")]
            public long sucataParaAvancar = 500;
        }

        [Tooltip("Os 5 níveis na ordem do GDD. Index 0 = Nível 1.")]
        public Nivel[] niveis = new Nivel[5];

        [Tooltip("Intervalo (segundos) entre tentativas de evento aleatório.")]
        public float intervaloTentativaEvento = 45f;

        [Tooltip("Chance (0–1) de um evento ocorrer a cada tentativa.")]
        [Range(0f, 1f)] public float chanceEvento = 0.5f;

        /// <summary>Retorna a configuração do nível informado (1-based), com clamp seguro.</summary>
        public Nivel ObterNivel(int nivel1Based)
        {
            if (niveis == null || niveis.Length == 0)
                return new Nivel();

            int idx = Mathf.Clamp(nivel1Based - 1, 0, niveis.Length - 1);
            return niveis[idx] ?? new Nivel();
        }

        public int TotalNiveis => niveis != null ? niveis.Length : 0;
    }
}
