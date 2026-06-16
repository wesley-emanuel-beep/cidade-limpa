using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CidadeLimpa.Core;
using CidadeLimpa.Anim;

namespace CidadeLimpa.UI
{
    /// <summary>
    /// Menu de pausa sobreposto: continuar, alternar som, voltar ao menu e sair.
    /// Pausa/retoma o jogo (Time.timeScale) ao abrir/fechar. Autoconstruído.
    /// </summary>
    public class PausaUI : MonoBehaviour
    {
        private RectTransform _overlay;
        private RectTransform _janela;
        private Image _iconeSom;
        private bool _aberto;

        private void Awake()
        {
            ConstruirUI();
            DefinirVisivel(false);
        }

        private void ConstruirUI()
        {
            var overlay = UIFactory.Painel("OverlayPausa", transform,
                new Color(0f, 0f, 0f, 0.7f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.raycastTarget = true;
            _overlay = overlay.rectTransform;

            var janela = UIFactory.Painel("JanelaPausa", _overlay, Paleta.Papel,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _janela = janela.rectTransform;
            UIFactory.Posicionar(_janela, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 520f));

            var titulo = UIFactory.Texto("TituloPausa", _janela, "PAUSA", 48f, Paleta.Tinta, TextAlignmentOptions.Center);
            UIFactory.Posicionar(titulo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(440f, 64f));

            Botao("Continuar", -130f, Paleta.Eco, () => Alternar());
            Botao("Menu Principal", -210f, Paleta.Eletrico, () => Navegacao.IrParaMenu());
            Botao("Sair", -290f, Paleta.Cinza, () => Navegacao.Sair());

            // Botão de som no canto superior direito (ícone troca on/off).
            var btnSom = UIFactory.BotaoIcone("Som", _janela, ConfiguracoesAudio.Mudo ? "som_off" : "som_on",
                Paleta.Tinta, AlternarSom);
            UIFactory.Posicionar(btnSom.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(64f, 64f));

            // O ícone é o filho do botão (a Image no próprio botão é o fundo).
            foreach (var im in btnSom.GetComponentsInChildren<Image>(true))
                if (im.gameObject != btnSom.gameObject) { _iconeSom = im; break; }
        }

        private void Botao(string rotulo, float y, Color cor, System.Action acao)
        {
            var b = UIFactory.Botao("Btn_" + rotulo, _janela, rotulo, cor, Paleta.Papel, acao, 28f);
            UIFactory.Posicionar(b.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(400f, 64f));
        }

        private void AlternarSom()
        {
            bool mudo = ConfiguracoesAudio.Alternar();
            var novo = Resources.Load<Sprite>("Icones/" + (mudo ? "som_off" : "som_on"));
            if (_iconeSom != null && novo != null) _iconeSom.sprite = novo;
        }

        /// <summary>Abre/fecha o menu de pausa (e pausa/retoma o jogo).</summary>
        public void Alternar()
        {
            if (_aberto) Fechar(); else Abrir();
        }

        public void Abrir()
        {
            DefinirVisivel(true);
            if (GameManager.Existe) GameManager.Instancia.DefinirPausa(true);
            Tween.Pop(_janela, 1f, 1.2f, 0.25f);
        }

        public void Fechar()
        {
            DefinirVisivel(false);
            if (GameManager.Existe) GameManager.Instancia.DefinirPausa(false);
        }

        private void DefinirVisivel(bool v)
        {
            _aberto = v;
            if (_overlay != null) _overlay.gameObject.SetActive(v);
        }
    }
}
