using UnityEngine.SceneManagement;
using UnityEngine;

namespace CidadeLimpa.Core
{
    /// <summary>
    /// Navegação entre cenas do jogo (menu ↔ partida). Garante timeScale normal
    /// ao trocar de cena (evita ficar pausado após sair pelo menu de pausa).
    /// </summary>
    public static class Navegacao
    {
        public const string CenaMenu = "Menu";
        public const string CenaJogo = "CidadeLimpa";

        public static void IrParaJogo() => Carregar(CenaJogo);
        public static void IrParaMenu() => Carregar(CenaMenu);

        public static void Sair()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void Carregar(string nome)
        {
            Time.timeScale = 1f;
            if (Application.CanStreamedLevelBeLoaded(nome))
                SceneManager.LoadScene(nome);
            else
                Debug.LogError($"[Navegacao] Cena '{nome}' não está no Build Settings. Rode o Scene Builder.");
        }
    }
}
