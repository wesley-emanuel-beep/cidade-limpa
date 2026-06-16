using UnityEditor;
using UnityEngine;

namespace CidadeLimpa.EditorTools
{
    /// <summary>
    /// Aplica a identidade do jogo às configurações do Player: nome do produto,
    /// empresa e ícone do aplicativo (a partir de Art/appicon.png). Chamado pelo
    /// Scene Builder e disponível como item de menu.
    /// </summary>
    public static class IdentidadeProjeto
    {
        public const string Produto = "Cidade Limpa: Tubarão Sustentável";
        public const string Empresa = "Tubarão Game Studio";
        private const string CaminhoIcone = "Assets/_CidadeLimpa/Art/appicon.png";

        [MenuItem("CidadeLimpa/Aplicar Identidade (ícone + nome)", false, 20)]
        public static void Aplicar()
        {
            PlayerSettings.companyName = Empresa;
            PlayerSettings.productName = Produto;

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(CaminhoIcone);
            if (tex != null)
                DefinirIcone(tex);
            else
                Debug.LogWarning($"[Identidade] Ícone não encontrado em {CaminhoIcone}.");

            AssetDatabase.SaveAssets();
            Debug.Log("[Identidade] Nome e ícone aplicados ao Player Settings.");
        }

        private static void DefinirIcone(Texture2D tex)
        {
            var icones = new[] { tex };
            // Tenta a API moderna (Unity 6); cai para a legada se necessário.
            try
            {
                PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Standalone, icones, IconKind.Application);
                PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Android, icones, IconKind.Application);
                PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, icones, IconKind.Any);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Identidade] API moderna de ícones falhou, tentando legada: " + e.Message);
#pragma warning disable 618
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, icones);
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, icones);
#pragma warning restore 618
            }
        }
    }
}
