using UnityEngine;
using CidadeLimpa.Core;

namespace CidadeLimpa.Data
{
    /// <summary>
    /// Definição estática de um bairro de Tubarão-SC (GDD seção 03 — Mapa).
    /// Os valores "Alta/Média/Baixa" do GDD são modelados como multiplicadores
    /// numéricos para permitir balanceamento fino sem alterar código.
    /// </summary>
    [CreateAssetMenu(fileName = "Bairro_", menuName = "Cidade Limpa/Bairro", order = 0)]
    public class BairroData : ScriptableObject
    {
        [Header("Identidade")]
        [Tooltip("Nome exibido na HUD (ex.: CENTRO).")]
        public string nomeBairro = "Bairro";

        [Tooltip("Perfil socioeconômico — afeta o tipo predominante de resíduo.")]
        public TipoBairro tipo = TipoBairro.Residencial;

        [Tooltip("Cor de identidade do bairro no mapa.")]
        public Color corIdentidade = Color.gray;

        [Header("Posição no Mapa (mundo)")]
        [Tooltip("Posição central do bairro no mapa, em coordenadas de mundo.")]
        public Vector2 posicaoMapa = Vector2.zero;

        [Tooltip("Raio aproximado da área do bairro (spawn de lixo).")]
        public float raio = 3f;

        [Header("Geração de Lixo")]
        [Tooltip("População relativa (0–1). Determina a taxa base de geração de lixo.")]
        [Range(0f, 1f)] public float populacao = 0.5f;

        [Tooltip("Multiplicador da taxa de lixo deste bairro sobre a taxa global.")]
        [Range(0f, 3f)] public float multiplicadorLixo = 1f;

        [Tooltip("Chance (0–1) de o resíduo gerado ser reciclável (mais sucata).")]
        [Range(0f, 1f)] public float chanceReciclavel = 0.3f;

        [Header("Limite de Acúmulo")]
        [Tooltip("Quantidade de lixo acumulado a partir da qual a satisfação local começa a cair.")]
        public int limiteSatisfatorio = 4;

        [Tooltip("Quantidade de lixo que caracteriza um 'Grande Acúmulo' (crise local).")]
        public int limiteCritico = 10;
    }
}
