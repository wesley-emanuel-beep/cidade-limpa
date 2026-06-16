using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CidadeLimpa.Core;
using CidadeLimpa.Caminhoes;
using CidadeLimpa.Anim;

namespace CidadeLimpa.UI
{
    /// <summary>
    /// Loja (modo despacho): comprar caminhões e melhorias globais da frota
    /// (capacidade, velocidade, valor do lixo, descarga rápida). Estilo Bauhaus.
    /// </summary>
    public class LojaUI : MonoBehaviour
    {
        private RectTransform _janela;
        private TextMeshProUGUI _saldo;
        private bool _aberta;

        private readonly List<Action> _refreshers = new List<Action>();

        private void Awake()
        {
            ConstruirUI();
            DefinirVisivel(false);
        }

        private void OnEnable()
        {
            GameEvents.SucataAlterada += _ => { if (_aberta) Refrescar(); };
            GameEvents.FrotaAlterada += _ => { if (_aberta) Refrescar(); };
        }

        private void ConstruirUI()
        {
            var overlay = UIFactory.Painel("OverlayLoja", transform,
                new Color(0f, 0f, 0f, 0.55f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.raycastTarget = true;

            var janela = UIFactory.Painel("JanelaLoja", overlay.transform, Paleta.Papel,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _janela = janela.rectTransform;
            UIFactory.Posicionar(_janela, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 768f));

            var cab = UIFactory.Painel("Cabecalho", _janela, Paleta.Vermelho,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -92f), Vector2.zero);
            var titulo = UIFactory.Texto("Titulo", cab.transform, "LOJA", 46f, Paleta.Papel, TextAlignmentOptions.Left);
            titulo.fontStyle = FontStyles.Bold;
            UIFactory.Posicionar(titulo.rectTransform, new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(400f, 56f));

            var fechar = UIFactory.Botao("Fechar", cab.transform, "X", Paleta.Tinta, Paleta.Papel, Fechar, 30f);
            UIFactory.Posicionar(fechar.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(-22f, 0f), new Vector2(58f, 58f));

            _saldo = UIFactory.Texto("Saldo", _janela, "DINHEIRO: 0", 30f, Paleta.Tinta, TextAlignmentOptions.Left);
            _saldo.fontStyle = FontStyles.Bold;
            UIFactory.Posicionar(_saldo.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -120f), new Vector2(600f, 40f));

            float y = -168f;
            CriarLinhaCaminhao(ref y);
            CriarLinhaMelhoria(TipoMelhoria.Capacidade, Paleta.Azul, ref y);
            CriarLinhaMelhoria(TipoMelhoria.Velocidade, Paleta.Azul, ref y);
            CriarLinhaMelhoria(TipoMelhoria.Valor, Paleta.Eco, ref y);
            CriarLinhaMelhoria(TipoMelhoria.Descarga, Paleta.Eco, ref y);
            CriarLinhaMelhoria(TipoMelhoria.Automacao, Paleta.Amarelo, ref y);
        }

        private void CriarLinhaCaminhao(ref float y)
        {
            var btn = UIFactory.Botao("Comprar", _janela, "NOVO CAMINHÃO", Paleta.Amarelo, Paleta.Tinta,
                () => { if (FrotaManager.Existe) FrotaManager.Instancia.ComprarCaminhaoAdicional(); Refrescar(); }, 24f);
            UIFactory.Posicionar(btn.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(740f, 66f));
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();

            _refreshers.Add(() =>
            {
                long custo = FrotaManager.Existe ? FrotaManager.Instancia.CustoProximoCaminhao : 0;
                int n = FrotaManager.Existe ? FrotaManager.Instancia.Frota.Count : 0;
                long saldo = SucataManager.Existe ? SucataManager.Instancia.Saldo : 0;
                bool pode = saldo >= custo;
                label.text = $"NOVO CAMINHÃO  (frota: {n})     {custo:n0}";
                btn.interactable = pode;
                btn.targetGraphic.color = pode ? Paleta.Amarelo : Paleta.Cinza;
            });
            y -= 78f;
        }

        private void CriarLinhaMelhoria(TipoMelhoria tipo, Color cor, ref float y)
        {
            var btn = UIFactory.Botao("Up_" + tipo, _janela, MelhoriasGlobais.Nome(tipo), cor, Paleta.Papel,
                () => { if (MelhoriasGlobais.Existe) MelhoriasGlobais.Instancia.Comprar(tipo); Refrescar(); }, 22f);
            UIFactory.Posicionar(btn.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(740f, 66f));
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();

            _refreshers.Add(() =>
            {
                if (!MelhoriasGlobais.Existe) return;
                var m = MelhoriasGlobais.Instancia;
                int nivel = m.Nivel(tipo);
                long custo = m.Custo(tipo);
                long saldo = SucataManager.Existe ? SucataManager.Instancia.Saldo : 0;
                bool max = m.NoMaximo(tipo);
                bool pode = !max && saldo >= custo;

                label.text = max
                    ? $"{MelhoriasGlobais.Nome(tipo)}  Nv.{nivel}  (MÁX)"
                    : $"{MelhoriasGlobais.Nome(tipo)}  Nv.{nivel}→{nivel + 1}     {custo:n0}";
                btn.interactable = pode;
                btn.targetGraphic.color = pode ? cor : Paleta.Cinza;
            });
            y -= 78f;
        }

        public void Abrir() { DefinirVisivel(true); Refrescar(); Tween.Pop(_janela, 1f, 1.12f, 0.25f); }
        public void Fechar() { DefinirVisivel(false); }
        public void Alternar() { if (_aberta) Fechar(); else Abrir(); }

        private void DefinirVisivel(bool v)
        {
            _aberta = v;
            if (transform.childCount > 0) transform.GetChild(0).gameObject.SetActive(v);
        }

        private void Refrescar()
        {
            if (_saldo != null)
                _saldo.text = $"DINHEIRO: {(SucataManager.Existe ? SucataManager.Instancia.Saldo : 0):n0}";
            for (int i = 0; i < _refreshers.Count; i++) _refreshers[i]?.Invoke();
        }
    }
}
