using System.Collections.Generic;
using UnityEngine;
using CidadeLimpa.Core;

namespace CidadeLimpa.Lixo
{
    /// <summary>
    /// Pool de objetos <see cref="LixoItem"/> (otimização exigida: GC mínimo em
    /// mobile). Cria um SpriteRenderer quadrado procedural como visual padrão,
    /// permitindo o jogo rodar sem sprites importados. Singleton de cena.
    /// </summary>
    public class LixoPool : Singleton<LixoPool>
    {
        [Header("Pool")]
        [Tooltip("Quantidade pré-instanciada no início.")]
        [SerializeField] private int tamanhoInicial = 32;

        [Tooltip("Prefab opcional do item de lixo. Se nulo, gera um placeholder.")]
        [SerializeField] private LixoItem prefab;

        [Tooltip("Sprite do lixo (arte). Se nulo, usa um quadrado procedural.")]
        [SerializeField] private Sprite spriteLixo;

        private readonly Queue<LixoItem> _reserva = new Queue<LixoItem>();
        private Sprite _spritePadrao;
        private Transform _raiz;

        protected override void AoInicializar()
        {
            _raiz = transform;
            _spritePadrao = spriteLixo != null ? spriteLixo : SpriteQuadrado.Obter();
            for (int i = 0; i < tamanhoInicial; i++)
                _reserva.Enqueue(CriarNovo());
        }

        private LixoItem CriarNovo()
        {
            LixoItem item;
            if (prefab != null)
            {
                item = Instantiate(prefab, _raiz);
            }
            else
            {
                var go = new GameObject("LixoItem");
                go.transform.SetParent(_raiz);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _spritePadrao;
                sr.sortingOrder = 5;
                item = go.AddComponent<LixoItem>();
            }

            item.AoLiberar = Devolver;
            item.gameObject.SetActive(false);
            return item;
        }

        /// <summary>Retira (ou cria) um item configurado do pool.</summary>
        public LixoItem Obter(TipoLixo tipo, CategoriaReciclavel categoria, Vector3 posicao)
        {
            var item = _reserva.Count > 0 ? _reserva.Dequeue() : CriarNovo();
            item.Configurar(tipo, categoria, posicao);
            return item;
        }

        /// <summary>Devolve um item à reserva (chamado via callback do item).</summary>
        public void Devolver(LixoItem item)
        {
            if (item == null) return;
            item.gameObject.SetActive(false);
            item.transform.SetParent(_raiz);
            _reserva.Enqueue(item);
        }
    }

    /// <summary>
    /// Gera (uma vez) um sprite quadrado branco 1x1 reutilizável como placeholder
    /// para ícones flat, evitando dependência de assets de textura.
    /// </summary>
    public static class SpriteQuadrado
    {
        private static Sprite _cache;

        public static Sprite Obter()
        {
            if (_cache != null) return _cache;

            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels32(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            _cache = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            _cache.name = "SpriteQuadradoProcedural";
            return _cache;
        }
    }

    /// <summary>
    /// Gera (uma vez) um sprite circular branco com borda suave, usado como
    /// marcador translúcido de bairro — menos intrusivo que um quadrado.
    /// </summary>
    public static class SpriteCirculo
    {
        private static Sprite _cache;

        public static Sprite Obter()
        {
            if (_cache != null) return _cache;

            const int R = 64;
            var tex = new Texture2D(R, R, TextureFormat.RGBA32, false);
            var px = new Color32[R * R];
            float c = (R - 1) * 0.5f;
            for (int y = 0; y < R; y++)
            {
                for (int x = 0; x < R; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    // disco cheio com borda anti-aliased nos 8% externos
                    float a = Mathf.Clamp01((1f - d) / 0.08f);
                    px[y * R + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;

            _cache = Sprite.Create(tex, new Rect(0, 0, R, R), new Vector2(0.5f, 0.5f), R);
            _cache.name = "SpriteCirculoProcedural";
            return _cache;
        }
    }
}
