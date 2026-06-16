using UnityEngine;
using CidadeLimpa.Core;

namespace CidadeLimpa.Caminhoes
{
    /// <summary>
    /// Indicador visual de seleção de um caminhão: um anel/quad sob o veículo que
    /// aparece quando selecionado. Criado proceduralmente para não depender de
    /// assets. O destaque pulsa levemente para leitura imediata na HUD.
    /// </summary>
    [RequireComponent(typeof(Caminhao))]
    public class CaminhaoSelecao : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer indicador;
        [SerializeField] private float tamanho = 1.05f;

        private Caminhao _caminhao;
        private bool _selecionado;

        public bool Selecionado => _selecionado;

        private void Awake()
        {
            _caminhao = GetComponent<Caminhao>();
            GarantirIndicador();
            DefinirSelecionado(false);
        }

        private void GarantirIndicador()
        {
            if (indicador != null) return;

            var go = new GameObject("IndicadorSelecao");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            go.transform.localScale = Vector3.one * tamanho;

            // Halo circular atrás do caminhão (sortingOrder logo abaixo do veículo).
            indicador = go.AddComponent<SpriteRenderer>();
            indicador.sprite = Lixo.SpriteCirculo.Obter();
            indicador.color = new Color(Paleta.Eletrico.r, Paleta.Eletrico.g, Paleta.Eletrico.b, 0.45f);
            indicador.sortingOrder = 19;
        }

        /// <summary>Liga/desliga o destaque de seleção.</summary>
        public void DefinirSelecionado(bool valor)
        {
            _selecionado = valor;
            if (indicador != null) indicador.enabled = valor;
            GameEvents.DispararCaminhaoSelecionado(_caminhao.Id, valor);
        }

        private void Update()
        {
            if (!_selecionado || indicador == null) return;
            float p = 1f + Mathf.Sin(Time.time * 6f) * 0.06f;
            indicador.transform.localScale = Vector3.one * tamanho * p;
        }
    }
}
