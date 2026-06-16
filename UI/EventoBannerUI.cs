using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CidadeLimpa.Core;
using CidadeLimpa.Anim;

namespace CidadeLimpa.UI
{
    /// <summary>
    /// Banner que anuncia eventos aleatórios ativos (GDD seção 08) logo abaixo da
    /// barra superior, com contagem regressiva. Autoconstruído e dirigido pelos
    /// eventos <see cref="GameEvents.EventoIniciado"/> / <c>EventoFinalizado</c>.
    /// </summary>
    public class EventoBannerUI : MonoBehaviour
    {
        private RectTransform _painel;
        private Image _fundo;
        private TextMeshProUGUI _texto;
        private string _eventoAtual;
        private float _tempoRestante;

        private void Awake()
        {
            ConstruirUI();
            DefinirVisivel(false);
        }

        private void OnEnable()
        {
            GameEvents.EventoIniciado += AoIniciar;
            GameEvents.EventoFinalizado += AoFinalizar;
        }

        private void OnDisable()
        {
            GameEvents.EventoIniciado -= AoIniciar;
            GameEvents.EventoFinalizado -= AoFinalizar;
        }

        private void ConstruirUI()
        {
            _fundo = UIFactory.Painel("BannerEvento", transform, Paleta.Eletrico,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            _painel = _fundo.rectTransform;
            UIFactory.Posicionar(_painel, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(560f, 56f));

            _texto = UIFactory.Texto("TextoEvento", _painel, "", 26f, Paleta.Papel, TextAlignmentOptions.Center);
            UIFactory.Esticar(_texto.rectTransform);
        }

        private void AoIniciar(string nome, float duracao)
        {
            _eventoAtual = nome;
            _tempoRestante = duracao;
            DefinirVisivel(true);
            // Desliza de cima para baixo ao surgir.
            _painel.anchoredPosition = new Vector2(0f, -48f);
            Tween.MoverAncora(_painel, new Vector2(0f, -100f), 0.35f, TipoEase.OutBack);
            AtualizarTexto();
        }

        private void AoFinalizar(string nome)
        {
            if (_eventoAtual == nome)
            {
                _eventoAtual = null;
                DefinirVisivel(false);
            }
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(_eventoAtual)) return;

            _tempoRestante -= Time.deltaTime;
            if (_tempoRestante <= 0f)
            {
                // Eventos imediatos não disparam Finalizado: esconde por tempo.
                DefinirVisivel(false);
                _eventoAtual = null;
                return;
            }
            AtualizarTexto();
        }

        private void AtualizarTexto()
        {
            if (_texto != null)
                _texto.text = $"{_eventoAtual.ToUpperInvariant()}   ·   {Mathf.CeilToInt(_tempoRestante)}s";
        }

        private void DefinirVisivel(bool v)
        {
            if (_painel != null) _painel.gameObject.SetActive(v);
        }
    }
}
