using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CidadeLimpa.Core;
using CidadeLimpa.Lixo;

namespace CidadeLimpa.UI
{
    /// <summary>
    /// Fábrica de elementos de UI construídos por código. Permite que cada
    /// componente de HUD monte sua própria hierarquia sem depender de prefabs ou
    /// de fiação manual no Inspector — essencial para o Scene Builder automático.
    /// Usa TextMeshPro e a paleta oficial do projeto.
    /// </summary>
    public static class UIFactory
    {
        /// <summary>Cria um RectTransform filho já configurado.</summary>
        public static RectTransform NovoRect(string nome, Transform pai)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Painel com fundo (Image) e âncoras configuráveis.</summary>
        public static Image Painel(string nome, Transform pai, Color cor,
            Vector2 ancMin, Vector2 ancMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = NovoRect(nome, pai);
            rt.anchorMin = ancMin;
            rt.anchorMax = ancMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = SpriteQuadrado.Obter();
            img.type = Image.Type.Sliced;
            img.color = cor;
            return img;
        }

        /// <summary>Texto TMP com configuração comum.</summary>
        public static TextMeshProUGUI Texto(string nome, Transform pai, string conteudo,
            float tamanho, Color cor, TextAlignmentOptions alinhamento = TextAlignmentOptions.Left)
        {
            var rt = NovoRect(nome, pai);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = conteudo;
            t.fontSize = tamanho;
            t.color = cor;
            t.alignment = alinhamento;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Botão com rótulo TMP e callback.</summary>
        public static Button Botao(string nome, Transform pai, string rotulo, Color corFundo,
            Color corTexto, Action aoClicar, float tamanhoFonte = 22f)
        {
            var img = Painel(nome, pai, corFundo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            img.raycastTarget = true;

            var btn = img.gameObject.AddComponent<Button>();
            var cores = btn.colors;
            cores.normalColor = Color.white;
            cores.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
            cores.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            cores.fadeDuration = 0.08f;
            btn.colors = cores;
            btn.targetGraphic = img;

            var label = Texto(nome + "_Label", img.transform, rotulo, tamanhoFonte, corTexto, TextAlignmentOptions.Center);
            Esticar(label.rectTransform);

            if (aoClicar != null)
                btn.onClick.AddListener(() => aoClicar());

            return btn;
        }

        /// <summary>Barra de progresso simples (fundo + preenchimento horizontal).</summary>
        public static Image Barra(string nome, Transform pai, Color corFundo, Color corFill,
            Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax)
        {
            var fundo = Painel(nome, pai, corFundo, ancMin, ancMax, offMin, offMax);

            var fill = Painel(nome + "_Fill", fundo.transform, corFill,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            return fill;
        }

        /// <summary>Cria uma Image com um sprite carregado de Resources/Icones.</summary>
        public static Image Icone(string nome, Transform pai, Color? cor = null)
        {
            var rt = NovoRect("Icone_" + nome, pai);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Resources.Load<Sprite>("Icones/" + nome);
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = img.sprite != null ? (cor ?? Color.white) : new Color(1f, 1f, 1f, 0f);
            return img;
        }

        /// <summary>Botão cujo conteúdo é um ícone (sem rótulo de texto).</summary>
        public static Button BotaoIcone(string nome, Transform pai, string icone, Color corFundo,
            System.Action aoClicar)
        {
            var img = Painel(nome, pai, corFundo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;

            var ic = Icone(icone, img.transform);
            var rt = ic.rectTransform;
            rt.anchorMin = new Vector2(0.18f, 0.18f);
            rt.anchorMax = new Vector2(0.82f, 0.82f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            if (aoClicar != null) btn.onClick.AddListener(() => aoClicar());
            return btn;
        }

        /// <summary>Estica um RectTransform para preencher o pai.</summary>
        public static void Esticar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>Define âncora + posição + tamanho de forma compacta.</summary>
        public static void Posicionar(RectTransform rt, Vector2 anchor, Vector2 posAnchored, Vector2 tamanho)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = posAnchored;
            rt.sizeDelta = tamanho;
        }
    }
}
