using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CidadeLimpa.Core;
using CidadeLimpa.Caminhoes;
using CidadeLimpa.Anim;

namespace CidadeLimpa.UI
{
    /// <summary>
    /// Minigame de reciclagem (GDD seção 09). Disparado por chance após coletas com
    /// recicláveis. O jogador classifica resíduos tocando na lixeira correta
    /// (Papel/Plástico/Vidro/Metal) dentro do tempo. O bônus de sucata é
    /// proporcional à taxa de acerto. Autoconstruído e overlay (não pausa o idle).
    /// </summary>
    public class MinigameReciclagem : MonoBehaviour
    {
        [Header("Regras")]
        [SerializeField] private float duracao = 10f;
        [SerializeField] private int totalItens = 6;

        private RectTransform _overlay;
        private RectTransform _janela;
        private TextMeshProUGUI _timerLabel;
        private TextMeshProUGUI _placar;
        private Image _itemAtualIcone;
        private TextMeshProUGUI _itemAtualLabel;

        private bool _ativo;
        private float _tempo;
        private int _itensRestantes;
        private int _acertos;
        private long _bonusPotencial;
        private CategoriaReciclavel _categoriaAtual;

        private void Awake()
        {
            ConstruirUI();
            DefinirVisivel(false);
        }

        private void OnEnable() => GameEvents.MinigameSolicitado += Iniciar;
        private void OnDisable() => GameEvents.MinigameSolicitado -= Iniciar;

        private void ConstruirUI()
        {
            var overlay = UIFactory.Painel("OverlayMinigame", transform,
                new Color(0f, 0f, 0f, 0.6f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.raycastTarget = true;
            _overlay = overlay.rectTransform;

            var janela = UIFactory.Painel("JanelaMinigame", _overlay, Paleta.Papel,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            UIFactory.Posicionar(janela.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 520f));
            _janela = janela.rectTransform;

            var titulo = UIFactory.Texto("TituloMini", janela.transform, "MINIGAME DE RECICLAGEM",
                30f, Paleta.Tinta, TextAlignmentOptions.Center);
            UIFactory.Posicionar(titulo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(700f, 44f));

            _timerLabel = UIFactory.Texto("TimerMini", janela.transform, "10s", 28f, Paleta.Alerta, TextAlignmentOptions.Right);
            UIFactory.Posicionar(_timerLabel.rectTransform, new Vector2(1f, 1f), new Vector2(-24f, -70f), new Vector2(120f, 36f));

            _placar = UIFactory.Texto("PlacarMini", janela.transform, "Acertos: 0", 24f, Paleta.Eco, TextAlignmentOptions.Left);
            UIFactory.Posicionar(_placar.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -70f), new Vector2(260f, 36f));

            // Ícone do item atual.
            _itemAtualIcone = UIFactory.Painel("ItemIcone", janela.transform, Paleta.Cinza,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            UIFactory.Posicionar(_itemAtualIcone.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(120f, 120f));

            _itemAtualLabel = UIFactory.Texto("ItemLabel", janela.transform, "", 26f, Paleta.Tinta, TextAlignmentOptions.Center);
            UIFactory.Posicionar(_itemAtualLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(400f, 40f));

            // Quatro lixeiras.
            CriarLixeira(CategoriaReciclavel.Papel, -270f);
            CriarLixeira(CategoriaReciclavel.Plastico, -90f);
            CriarLixeira(CategoriaReciclavel.Vidro, 90f);
            CriarLixeira(CategoriaReciclavel.Metal, 270f);
        }

        private void CriarLixeira(CategoriaReciclavel cat, float x)
        {
            var capturada = cat;
            var pai = transform.GetChild(0).GetChild(0); // janela
            string icone = "bin_" + cat.ToString().ToLowerInvariant();

            var btn = UIFactory.BotaoIcone("Lixeira_" + cat, pai, icone,
                new Color(1f, 1f, 1f, 0.06f), () => Classificar(capturada));
            UIFactory.Posicionar(btn.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f), new Vector2(x, 40f), new Vector2(150f, 150f));

            var legenda = UIFactory.Texto("Legenda_" + cat, pai, cat.ToString().ToUpperInvariant(),
                18f, Paleta.Tinta, TextAlignmentOptions.Center);
            UIFactory.Posicionar(legenda.rectTransform, new Vector2(0.5f, 0f), new Vector2(x, 18f), new Vector2(160f, 24f));
        }

        private void Iniciar(long bonusPotencial)
        {
            if (_ativo) return;
            _ativo = true;
            _bonusPotencial = bonusPotencial;
            _tempo = duracao;
            _itensRestantes = totalItens;
            _acertos = 0;
            DefinirVisivel(true);
            Tween.Pop(_janela, 1f, 1.15f, 0.25f);
            ProximoItem();
            AtualizarPlacar();
        }

        private void ProximoItem()
        {
            if (_itensRestantes <= 0) { Finalizar(); return; }

            _categoriaAtual = (CategoriaReciclavel)Random.Range(0, 4);
            if (_itemAtualIcone != null) _itemAtualIcone.color = Paleta.CorReciclavel(_categoriaAtual);
            if (_itemAtualLabel != null) _itemAtualLabel.text = $"Onde vai o {_categoriaAtual.ToString().ToUpperInvariant()}?";
        }

        private void Classificar(CategoriaReciclavel escolha)
        {
            if (!_ativo) return;

            if (escolha == _categoriaAtual) _acertos++;
            _itensRestantes--;
            AtualizarPlacar();
            ProximoItem();
        }

        private void Update()
        {
            if (!_ativo) return;

            _tempo -= Time.deltaTime;
            if (_timerLabel != null) _timerLabel.text = $"{Mathf.CeilToInt(Mathf.Max(0f, _tempo))}s";

            if (_tempo <= 0f) Finalizar();
        }

        private void Finalizar()
        {
            if (!_ativo) return;
            _ativo = false;

            float taxa = totalItens > 0 ? (float)_acertos / totalItens : 0f;
            long bonus = (long)Mathf.Round(_bonusPotencial * taxa);

            if (bonus > 0 && SucataManager.Existe)
            {
                Vector3 pos = Deposito.Existe ? Deposito.Instancia.Posicao : Vector3.zero;
                SucataManager.Instancia.Ganhar(bonus, pos);
            }

            GameEvents.DispararMinigameConcluido(bonus);
            DefinirVisivel(false);
        }

        private void AtualizarPlacar()
        {
            if (_placar != null) _placar.text = $"Acertos: {_acertos}/{totalItens}";
        }

        private void DefinirVisivel(bool v)
        {
            if (_overlay != null) _overlay.gameObject.SetActive(v);
        }
    }
}
