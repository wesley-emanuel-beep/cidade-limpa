using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CidadeLimpa.Core;
using CidadeLimpa.Anim;

namespace CidadeLimpa.UI
{
    /// <summary>
    /// Barra superior da HUD — estilo Bauhaus/Suíço: blocos de cor primária,
    /// tipografia pesada e linha-régua preta. Mostra dinheiro e limpeza da cidade,
    /// com botões de Loja e Pausa. Autoconstruído.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        private TextMeshProUGUI _txtDinheiro;
        private TextMeshProUGUI _txtLimpeza;
        private Image _barraLimpeza;
        private long _dinheiroAnterior;

        private void Awake() => ConstruirUI();

        private void OnEnable()
        {
            GameEvents.SucataAlterada += AtualizarDinheiro;
            GameEvents.SatisfacaoAlterada += AtualizarLimpeza;
        }

        private void OnDisable()
        {
            GameEvents.SucataAlterada -= AtualizarDinheiro;
            GameEvents.SatisfacaoAlterada -= AtualizarLimpeza;
        }

        private void ConstruirUI()
        {
            // Faixa off-white com régua preta embaixo.
            var barra = UIFactory.Painel("BarraSuperior", transform, Paleta.Papel,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -118f), Vector2.zero);
            UIFactory.Painel("Regua", barra.transform, Paleta.Tinta,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 7f));

            // Bloco de DINHEIRO (amarelo) à esquerda.
            var bloco = UIFactory.Painel("BlocoDinheiro", barra.transform, Paleta.Amarelo,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            UIFactory.Posicionar(bloco.rectTransform, new Vector2(0f, 0.5f), new Vector2(24f, 4f), new Vector2(360f, 84f));

            var icoMoeda = UIFactory.Icone("dinheiro", bloco.transform, Paleta.Tinta);
            UIFactory.Posicionar(icoMoeda.rectTransform, new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(56f, 56f));

            _txtDinheiro = UIFactory.Texto("Dinheiro", bloco.transform, "0", 44f, Paleta.Tinta, TextAlignmentOptions.Right);
            _txtDinheiro.fontStyle = FontStyles.Bold;
            UIFactory.Posicionar(_txtDinheiro.rectTransform, new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(260f, 70f));

            // LIMPEZA DA CIDADE (centro): rótulo + barra emoldurada.
            var rot = UIFactory.Texto("RotuloLimpeza", barra.transform, "LIMPEZA DA CIDADE", 20f, Paleta.Tinta, TextAlignmentOptions.Left);
            rot.fontStyle = FontStyles.Bold;
            UIFactory.Posicionar(rot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(74f, 26f), new Vector2(360f, 28f));

            _barraLimpeza = UIFactory.Barra("BarraLimpeza", barra.transform, Paleta.Tinta, Paleta.Azul,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            UIFactory.Posicionar((RectTransform)_barraLimpeza.transform.parent, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(420f, 26f));
            // dá moldura preta: encolhe o fill 4px.
            var fillRT = _barraLimpeza.rectTransform;
            fillRT.offsetMin = new Vector2(4f, 4f); fillRT.offsetMax = new Vector2(-4f, -4f);

            _txtLimpeza = UIFactory.Texto("PctLimpeza", barra.transform, "100%", 22f, Paleta.Tinta, TextAlignmentOptions.Left);
            _txtLimpeza.fontStyle = FontStyles.Bold;
            UIFactory.Posicionar(_txtLimpeza.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(257f, -10f), new Vector2(80f, 28f));

            // LOJA (vermelho) + Pausa (preto), à direita.
            var btnLoja = UIFactory.Botao("BotaoLoja", barra.transform, "LOJA", Paleta.Vermelho, Paleta.Papel,
                () => { if (UIManager.Existe) UIManager.Instancia.Loja?.Alternar(); }, 26f);
            UIFactory.Posicionar(btnLoja.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(-104f, 4f), new Vector2(150f, 84f));

            var btnPausa = UIFactory.BotaoIcone("BotaoPausa", barra.transform, "pause", Paleta.Tinta,
                () => { if (UIManager.Existe) UIManager.Instancia.Pausa?.Alternar(); });
            UIFactory.Posicionar(btnPausa.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(-24f, 4f), new Vector2(64f, 64f));
        }

        private void AtualizarDinheiro(long total)
        {
            if (_txtDinheiro == null) return;
            _txtDinheiro.text = $"{total:n0}";
            if (total > _dinheiroAnterior) Tween.Pop(_txtDinheiro.transform, 1f, 1.25f, 0.22f);
            _dinheiroAnterior = total;
        }

        private void AtualizarLimpeza(float valor)
        {
            int pct = Mathf.RoundToInt(valor);
            if (_txtLimpeza != null) _txtLimpeza.text = $"{pct}%";
            if (_barraLimpeza != null)
            {
                _barraLimpeza.fillAmount = Mathf.Clamp01(valor / 100f);
                _barraLimpeza.color = Color.Lerp(Paleta.Vermelho, Paleta.Azul, valor / 100f);
            }
        }
    }
}
