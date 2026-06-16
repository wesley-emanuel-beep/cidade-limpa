using UnityEngine;

namespace CidadeLimpa.Data
{
    /// <summary>
    /// Definição de um modelo de caminhão (GDD seção 05). Atributos: Velocidade,
    /// Capacidade, Eficiência e Descarregamento. Inclui custo e níveis máximos de
    /// upgrade para alimentar a loja (seção 10).
    /// </summary>
    [CreateAssetMenu(fileName = "Caminhao_", menuName = "Cidade Limpa/Caminhão", order = 1)]
    public class CaminhaoData : ScriptableObject
    {
        [Header("Identidade")]
        public string nomeModelo = "Coletor 01";

        [TextArea] public string descricao = "Caminhão inicial de coleta.";

        [Tooltip("Cor do corpo do caminhão (placeholder low poly).")]
        public Color cor = new Color(0.1f, 0.45f, 1f);

        [Header("Atributos Base")]
        [Tooltip("Velocidade de deslocamento (unidades/segundo).")]
        [Range(1f, 20f)] public float velocidade = 4f;

        [Tooltip("Unidades de lixo carregadas por viagem.")]
        [Range(1, 200)] public int capacidade = 20;

        [Tooltip("Multiplicador de sucata obtida na descarga.")]
        [Range(0.5f, 5f)] public float eficiencia = 1f;

        [Tooltip("Tempo (segundos) para esvaziar a carga no depósito.")]
        [Range(0.2f, 10f)] public float tempoDescarregamento = 2f;

        [Tooltip("Tempo (segundos) por unidade coletada no bairro.")]
        [Range(0.02f, 1f)] public float tempoColetaPorUnidade = 0.1f;

        [Header("Economia / Loja")]
        [Tooltip("Custo em sucata para adquirir este caminhão. 0 = inicial gratuito.")]
        public long custo = 0;

        [Tooltip("Nível máximo de upgrade por atributo na loja.")]
        public int nivelMaxUpgrade = 5;

        [Tooltip("Ganho percentual por nível de upgrade (0.15 = +15% por nível).")]
        [Range(0.05f, 0.5f)] public float ganhoPorUpgrade = 0.15f;

        [Tooltip("Prefab opcional do modelo 3D. Se nulo, o Scene Builder gera um placeholder low poly.")]
        public GameObject prefabModelo;
    }
}
