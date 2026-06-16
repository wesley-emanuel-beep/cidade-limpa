using UnityEngine;

namespace CidadeLimpa.Core
{
    /// <summary>
    /// Configuração global de áudio persistida em PlayerPrefs. Controla o mudo
    /// geral via AudioListener.volume, funcionando em qualquer cena (menu/jogo).
    /// </summary>
    public static class ConfiguracoesAudio
    {
        private const string Chave = "cl_mudo";

        public static bool Mudo
        {
            get => PlayerPrefs.GetInt(Chave, 0) == 1;
            set { PlayerPrefs.SetInt(Chave, value ? 1 : 0); PlayerPrefs.Save(); Aplicar(); }
        }

        /// <summary>Alterna o mudo e retorna o novo estado.</summary>
        public static bool Alternar()
        {
            Mudo = !Mudo;
            return Mudo;
        }

        /// <summary>Aplica o volume conforme a preferência salva.</summary>
        public static void Aplicar() => AudioListener.volume = Mudo ? 0f : 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AplicarNoInicio() => Aplicar();
    }
}
