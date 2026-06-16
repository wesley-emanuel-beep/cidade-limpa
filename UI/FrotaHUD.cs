using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CidadeLimpa.Core;
using CidadeLimpa.Caminhoes;

namespace CidadeLimpa.UI
{
    /// <summary>
    /// Painel de despacho (inferior): um cartão por caminhão com número, barra de
    /// carga, estado e botão de retorno ao depósito. Tocar no cartão seleciona o
    /// caminhão; depois o jogador toca num ponto de coleta no mapa para enviá-lo.
    /// Estilo Bauhaus. Autoconstruído e reativo aos eventos da frota.
    /// </summary>
    public class FrotaHUD : MonoBehaviour
    {
        private class Cartao
        {
            public int id;
            public Caminhao caminhao;
            public Image fundo;
            public TextMeshProUGUI titulo;
            public TextMeshProUGUI estado;
            public TextMeshProUGUI cargaTxt;
            public Image barra;
        }

        private RectTransform _linha;
        private TextMeshProUGUI _dica;
        private readonly List<Cartao> _cartoes = new List<Cartao>();

        private void Awake()
        {
            ConstruirBase();
        }

        private void OnEnable()
        {
            GameEvents.FrotaAlterada += _ => Reconstruir();
            GameEvents.CaminhaoAtualizado += AtualizarCartao;
            GameEvents.CaminhaoSelecionado += (_, __) => RefrescarRealces();
        }

        private void Start() => Reconstruir();

        private void ConstruirBase()
        {
            _linha = UIFactory.NovoRect("LinhaFrota", transform);
            UIFactory.Posicionar(_linha, new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(1200f, 150f));
            var hlg = _linha.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.LowerLeft;
            hlg.childControlWidth = true; hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

            _dica = UIFactory.Texto("Dica", transform, "TOQUE NUM CAMINHÃO E DEPOIS NUM PONTO DE COLETA",
                18f, Paleta.Tinta, TextAlignmentOptions.Left);
            _dica.fontStyle = FontStyles.Bold;
            UIFactory.Posicionar(_dica.rectTransform, new Vector2(0f, 0f), new Vector2(26f, 182f), new Vector2(900f, 26f));
        }

        private void Reconstruir()
        {
            for (int i = _linha.childCount - 1; i >= 0; i--)
                Destroy(_linha.GetChild(i).gameObject);
            _cartoes.Clear();

            if (!FrotaManager.Existe) return;
            var frota = FrotaManager.Instancia.Frota;
            for (int i = 0; i < frota.Count; i++)
                CriarCartao(frota[i]);
        }

        private void CriarCartao(Caminhao c)
        {
            var raiz = UIFactory.NovoRect("Cartao_" + c.Id, _linha);
            var le = raiz.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 210; le.preferredHeight = 150;

            var fundo = raiz.gameObject.AddComponent<Image>();
            fundo.sprite = Lixo.SpriteQuadrado.Obter();
            fundo.color = Paleta.Papel;

            // moldura preta
            var borda = raiz.gameObject.AddComponent<Outline>();
            borda.effectColor = Paleta.Tinta;
            borda.effectDistance = new Vector2(4f, -4f);

            var btn = raiz.gameObject.AddComponent<Button>();
            btn.targetGraphic = fundo;
            int id = c.Id;
            btn.onClick.AddListener(() => { if (FrotaManager.Existe) FrotaManager.Instancia.SelecionarPorId(id); });

            // Número grande (bloco azul)
            var bloco = UIFactory.Painel("Num", raiz, Paleta.Azul, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            UIFactory.Posicionar(bloco.rectTransform, new Vector2(0f, 1f), new Vector2(8f, -8f), new Vector2(54f, 54f));
            var titulo = UIFactory.Texto("N", bloco.transform, (c.Id + 1).ToString(), 34f, Paleta.Papel, TextAlignmentOptions.Center);
            titulo.fontStyle = FontStyles.Bold; UIFactory.Esticar(titulo.rectTransform);

            var estado = UIFactory.Texto("Estado", raiz, "PARADO", 16f, Paleta.Tinta, TextAlignmentOptions.Left);
            estado.fontStyle = FontStyles.Bold;
            UIFactory.Posicionar(estado.rectTransform, new Vector2(0f, 1f), new Vector2(70f, -18f), new Vector2(132f, 24f));

            var carga = UIFactory.Texto("Carga", raiz, "0/0", 15f, Paleta.Tinta, TextAlignmentOptions.Left);
            UIFactory.Posicionar(carga.rectTransform, new Vector2(0f, 1f), new Vector2(70f, -44f), new Vector2(132f, 20f));

            var barra = UIFactory.Barra("Barra", raiz, Paleta.Tinta, Paleta.Eco,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(8f, 52f), new Vector2(-8f, 70f));
            barra.rectTransform.offsetMin = new Vector2(2f, 2f); barra.rectTransform.offsetMax = new Vector2(-2f, -2f);

            var btnVolta = UIFactory.Botao("Volta", raiz, "↩ DEPÓSITO", Paleta.Tinta, Paleta.Papel,
                () => c.EnviarAoDeposito(), 16f);
            UIFactory.Posicionar(btnVolta.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(190f, 34f));

            var cartao = new Cartao { id = c.Id, caminhao = c, fundo = fundo, titulo = titulo, estado = estado, barra = barra };
            cartao.cargaTxt = carga;
            _cartoes.Add(cartao);
            Atualizar(cartao);
        }

        private void AtualizarCartao(int id)
        {
            for (int i = 0; i < _cartoes.Count; i++)
                if (_cartoes[i].id == id) { Atualizar(_cartoes[i]); return; }
        }

        private void Atualizar(Cartao card)
        {
            if (card.caminhao == null) return;
            card.estado.text = Traduzir(card.caminhao.Estado, card.caminhao.RoteiroCount);
            card.cargaTxt.text = $"{card.caminhao.Carga}/{card.caminhao.Capacidade}";
            if (card.barra != null) card.barra.fillAmount = card.caminhao.CargaNormalizada;
            RefrescarRealce(card);
        }

        private void RefrescarRealces()
        {
            for (int i = 0; i < _cartoes.Count; i++) RefrescarRealce(_cartoes[i]);
        }

        private void RefrescarRealce(Cartao card)
        {
            bool sel = FrotaManager.Existe && FrotaManager.Instancia.Selecionado == card.caminhao;
            card.fundo.color = sel ? Paleta.Amarelo : Paleta.Papel;
        }

        private static string Traduzir(EstadoCaminhao e, int rota)
        {
            switch (e)
            {
                case EstadoCaminhao.Ocioso: return rota > 0 ? "NA FILA" : "PARADO";
                case EstadoCaminhao.EmRota: return "A CAMINHO";
                case EstadoCaminhao.Coletando: return "COLETANDO";
                case EstadoCaminhao.Retornando: return "VOLTANDO";
                case EstadoCaminhao.Descarregando: return "DESCARGA";
                default: return e.ToString().ToUpperInvariant();
            }
        }
    }
}
