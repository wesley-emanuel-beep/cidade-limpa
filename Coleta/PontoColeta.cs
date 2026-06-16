using System.Collections.Generic;
using TMPro;
using UnityEngine;
using CidadeLimpa.Core;
using CidadeLimpa.Lixo;
using CidadeLimpa.Anim;

namespace CidadeLimpa.Coleta
{
    /// <summary>
    /// Ponto de coleta no mapa (mecânica de despacho): quantidade de lixo + valor.
    /// Visual Bauhaus compacto: disco colorido por urgência + anel + número, e um
    /// selo verde com o número do caminhão quando já há um caminhão designado.
    /// Poolável.
    /// </summary>
    public class PontoColeta : MonoBehaviour
    {
        public static readonly List<PontoColeta> Ativos = new List<PontoColeta>();
        public static int TotalPendente { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Resetar() { Ativos.Clear(); TotalPendente = 0; }

        /// <summary>
        /// Limpa o registro estático (chamar ao montar a cena de jogo). A lista
        /// estática sobrevive a trocas de cena e poderia guardar pontos destruídos.
        /// </summary>
        public static void LimparAtivos() { Ativos.Clear(); TotalPendente = 0; }

        private void OnDestroy()
        {
            Ativos.Remove(this);
            RecalcularTotal();
        }

        private SpriteRenderer _anel, _disco, _badge;
        private TextMeshPro _numero, _badgeTxt;

        private int _quantidade;
        private bool _selecionavel;

        public int Quantidade => _quantidade;
        public int ValorPorUnidade { get; private set; }
        public int AtribuidoId { get; private set; } = -1;
        public bool Atribuido => AtribuidoId >= 0;
        public Vector3 Posicao => transform.position;
        public long Recompensa => (long)_quantidade * ValorPorUnidade;

        public System.Action<PontoColeta> AoLiberar;

        private void Awake() => ConstruirVisual();

        private void ConstruirVisual()
        {
            _anel = Circulo("Anel", Paleta.Tinta, 6, 0.62f);
            _disco = Circulo("Disco", Paleta.Amarelo, 7, 0.46f);
            _numero = Texto("Numero", 3.2f, 8, Vector3.zero);

            // Selo do caminhão designado (canto superior direito).
            _badge = Circulo("Badge", Paleta.Eco, 9, 0.30f);
            _badge.transform.localPosition = new Vector3(0.26f, 0.26f, -0.5f);
            _badgeTxt = Texto("BadgeNum", 2.4f, 10, new Vector3(0.26f, 0.26f, -1f));
            _badgeTxt.color = Paleta.Papel;
            _badge.gameObject.SetActive(false);
            _badgeTxt.gameObject.SetActive(false);
        }

        private SpriteRenderer Circulo(string nome, Color cor, int ordem, float escala)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * escala;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteCirculo.Obter();
            sr.color = cor;
            sr.sortingOrder = ordem;
            return sr;
        }

        private TextMeshPro Texto(string nome, float tam, int ordem, Vector3 pos)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, -1f);
            var t = go.AddComponent<TextMeshPro>();
            t.text = "0"; t.fontSize = tam; t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center; t.color = Paleta.Tinta;
            t.sortingOrder = ordem; t.rectTransform.sizeDelta = new Vector2(3f, 3f);
            return t;
        }

        public void Configurar(int quantidade, int valorPorUnidade, Vector3 posicao)
        {
            _quantidade = Mathf.Max(1, quantidade);
            ValorPorUnidade = Mathf.Max(1, valorPorUnidade);
            AtribuidoId = -1;
            _selecionavel = false;
            transform.position = posicao;

            gameObject.SetActive(true);
            if (!Ativos.Contains(this)) Ativos.Add(this);
            RecalcularTotal();
            AtualizarVisual();

            transform.localScale = Vector3.one * 0.4f;
            Tween.Escala(transform, Vector3.one, 0.3f, TipoEase.OutBack, naoEscalado: false);
        }

        public int Coletar(int maximo)
        {
            if (maximo <= 0 || _quantidade <= 0) return 0;
            int coletado = Mathf.Min(maximo, _quantidade);
            _quantidade -= coletado;
            RecalcularTotal();
            if (_quantidade <= 0) Liberar();
            else AtualizarVisual();
            return coletado;
        }

        public void DefinirRealce(bool valor) { _selecionavel = valor; AtualizarVisual(); }

        public void DefinirAtribuido(int id) { AtribuidoId = id; AtualizarVisual(); }

        private void AtualizarVisual()
        {
            if (_numero != null) _numero.text = _quantidade.ToString();

            Color cor = _quantidade >= 60 ? Paleta.Vermelho
                      : _quantidade >= 25 ? Paleta.Amarelo : Paleta.Azul;
            if (_disco != null) _disco.color = cor;
            if (_numero != null) _numero.color = (_quantidade >= 25) ? Paleta.Tinta : Paleta.Papel;

            if (_anel != null)
                _anel.color = Atribuido ? Paleta.Eco : (_selecionavel ? Paleta.Vermelho : Paleta.Tinta);

            bool mostraBadge = Atribuido;
            if (_badge != null && _badge.gameObject.activeSelf != mostraBadge) _badge.gameObject.SetActive(mostraBadge);
            if (_badgeTxt != null && _badgeTxt.gameObject.activeSelf != mostraBadge) _badgeTxt.gameObject.SetActive(mostraBadge);
            if (mostraBadge && _badgeTxt != null) _badgeTxt.text = (AtribuidoId + 1).ToString();
        }

        private void Liberar()
        {
            Ativos.Remove(this);
            AtribuidoId = -1;
            RecalcularTotal();
            gameObject.SetActive(false);
            AoLiberar?.Invoke(this);
        }

        private static void RecalcularTotal()
        {
            int t = 0;
            for (int i = 0; i < Ativos.Count; i++) t += Ativos[i]._quantidade;
            TotalPendente = t;
        }

        private void Update()
        {
            if (_anel == null) return;
            bool pulsa = _selecionavel && !Atribuido;
            _anel.transform.localScale = Vector3.one * (pulsa ? 1f + Mathf.Sin(Time.time * 6f) * 0.1f : 1f);
        }
    }
}
