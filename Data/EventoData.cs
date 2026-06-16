using UnityEngine;
using CidadeLimpa.Core;

namespace CidadeLimpa.Data
{
    /// <summary>
    /// Definição de um evento aleatório (GDD seção 08). O efeito é genérico para
    /// permitir novos eventos sem código: tipo de efeito + magnitude + duração.
    /// </summary>
    [CreateAssetMenu(fileName = "Evento_", menuName = "Cidade Limpa/Evento", order = 2)]
    public class EventoData : ScriptableObject
    {
        [Header("Identidade")]
        public string nomeEvento = "Evento";

        [TextArea] public string descricao = "Descrição do evento.";

        [Tooltip("Cor do banner do evento na HUD.")]
        public Color cor = Color.cyan;

        [Header("Efeito")]
        public TipoEfeitoEvento tipoEfeito = TipoEfeitoEvento.MultiplicadorLixo;

        [Tooltip("Magnitude do efeito. Para multiplicadores: 1.5 = +50%. " +
                 "Para satisfação imediata: delta percentual (ex.: 10).")]
        public float magnitude = 1.5f;

        [Tooltip("Duração em segundos. Ignorado para efeitos imediatos.")]
        public float duracao = 90f;

        [Header("Ocorrência")]
        [Tooltip("Peso relativo de sorteio entre os eventos disponíveis.")]
        [Range(0.1f, 10f)] public float peso = 1f;

        [Tooltip("Nível de dificuldade mínimo para este evento poder ocorrer.")]
        [Range(1, 5)] public int nivelMinimo = 1;

        /// <summary>True quando o efeito é aplicado uma única vez (sem duração).</summary>
        public bool EhImediato => tipoEfeito == TipoEfeitoEvento.SatisfacaoImediata;
    }
}
