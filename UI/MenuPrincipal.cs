using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CidadeLimpa.Core;
using CidadeLimpa.Anim;

namespace CidadeLimpa.UI
{
    /// <summary>
    /// Menu principal do jogo: fundo, logo e ações (Jogar, Som, Sair).
    /// Autoconstruído; colocado num Canvas na cena "Menu" pelo Scene Builder.
    /// </summary>
    public class MenuPrincipal : MonoBehaviour
    {
        private Image _iconeSom;

        private void Awake() => ConstruirUI();

        private void ConstruirUI()
        {
            // Fundo sólido (papel) + faixa eco inferior para profundidade.
            UIFactory.Painel("Fundo", transform, Paleta.Papel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var faixa = UIFactory.Painel("FaixaEco", transform, new Color(Paleta.Eco.r, Paleta.Eco.g, Paleta.Eco.b, 0.12f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 220f));

            // Logo (carregado de Resources/Imagens/logo).
            var logo = UIFactory.NovoRect("Logo", transform);
            var imgLogo = logo.gameObject.AddComponent<Image>();
            imgLogo.sprite = Resources.Load<Sprite>("Imagens/logo");
            imgLogo.preserveAspect = true;
            imgLogo.raycastTarget = false;
            UIFactory.Posicionar(logo, new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(900f, 320f));
            if (imgLogo.sprite == null) // fallback textual
            {
                imgLogo.color = new Color(1, 1, 1, 0);
                var t = UIFactory.Texto("Titulo", transform, "CIDADE LIMPA", 80f, Paleta.Tinta, TextAlignmentOptions.Center);
                UIFactory.Posicionar(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(1000f, 120f));
            }
            Tween.Pop(logo, 1f, 1.15f, 0.45f);

            // Botão JOGAR.
            var jogar = UIFactory.Botao("Jogar", transform, "JOGAR", Paleta.Eco, Paleta.Papel,
                () => Navegacao.IrParaJogo(), 40f);
            var rtJogar = jogar.GetComponent<RectTransform>();
            UIFactory.Posicionar(rtJogar, new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(420f, 96f));
            Deslizar(rtJogar, new Vector2(0f, -120f), 0.35f);

            // Botão SAIR.
            var sair = UIFactory.Botao("Sair", transform, "SAIR", Paleta.Cinza, Paleta.Papel,
                () => Navegacao.Sair(), 30f);
            var rtSair = sair.GetComponent<RectTransform>();
            UIFactory.Posicionar(rtSair, new Vector2(0.5f, 0.5f), new Vector2(0f, -230f), new Vector2(300f, 72f));
            Deslizar(rtSair, new Vector2(0f, -230f), 0.45f);

            // Botão de som (canto superior direito).
            var btnSom = UIFactory.BotaoIcone("Som", transform, ConfiguracoesAudio.Mudo ? "som_off" : "som_on",
                Paleta.Tinta, AlternarSom);
            UIFactory.Posicionar(btnSom.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-36f, -36f), new Vector2(72f, 72f));
            foreach (var im in btnSom.GetComponentsInChildren<Image>(true))
                if (im.gameObject != btnSom.gameObject) { _iconeSom = im; break; }

            // Rodapé.
            var rodape = UIFactory.Texto("Rodape", transform, "v1.0 · Educação Ambiental · Tubarão-SC",
                20f, Paleta.Cinza, TextAlignmentOptions.Center);
            UIFactory.Posicionar(rodape.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(900f, 30f));
        }

        private void Deslizar(RectTransform rt, Vector2 destino, float dur)
        {
            rt.anchoredPosition = destino + new Vector2(0f, -60f);
            Tween.MoverAncora(rt, destino, dur, TipoEase.OutBack);
        }

        private void AlternarSom()
        {
            bool mudo = ConfiguracoesAudio.Alternar();
            var novo = Resources.Load<Sprite>("Icones/" + (mudo ? "som_off" : "som_on"));
            if (_iconeSom != null && novo != null) _iconeSom.sprite = novo;
        }
    }
}
