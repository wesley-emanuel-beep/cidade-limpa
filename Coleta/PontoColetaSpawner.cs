using System.Collections.Generic;
using UnityEngine;
using CidadeLimpa.Core;

namespace CidadeLimpa.Coleta
{
    /// <summary>
    /// Gera pontos de coleta espalhados pelas ruas da cidade (candidatos bakeados
    /// pelo Scene Builder). Mantém um número-alvo de pontos ativos, evita
    /// sobreposição e alimenta a "limpeza da cidade". Faz pooling dos pontos.
    /// </summary>
    public class PontoColetaSpawner : Singleton<PontoColetaSpawner>
    {
        [Header("Candidatos (preenchidos pelo Scene Builder)")]
        [SerializeField] private Vector2[] candidatos;

        [Header("Geração (base = início fácil)")]
        [SerializeField] private int alvoAtivos = 8;
        [SerializeField] private int prespawn = 4;
        [SerializeField] private float intervalo = 4.5f;
        [SerializeField] private float distanciaMinima = 1.6f;

        [Header("Valores (balanceamento)")]
        [SerializeField] private int quantidadeMin = 8;
        [SerializeField] private int quantidadeMax = 45;
        [SerializeField] private int valorPorUnidade = 4;

        [Header("Dificuldade progressiva (sobe com o dinheiro ganho)")]
        [Tooltip("A cada X de dinheiro total ganho, sobe 1 nível de dificuldade.")]
        [SerializeField] private long ganhoPorNivel = 700;
        [SerializeField] private int alvoAtivosMax = 24;
        [SerializeField] private int quantidadeMaxFinal = 100;
        [SerializeField] private float intervaloMin = 1.5f;

        private int NivelDificuldade()
        {
            long ganho = SucataManager.Existe ? SucataManager.Instancia.TotalGanho : 0;
            return (int)(ganho / Mathf.Max(1, ganhoPorNivel));
        }

        private int AlvoAtivosEf => Mathf.Min(alvoAtivos + NivelDificuldade(), alvoAtivosMax);
        private int QuantidadeMaxEf => Mathf.Min(quantidadeMax + NivelDificuldade() * 5, quantidadeMaxFinal);
        private float IntervaloEf => Mathf.Max(intervaloMin, intervalo - NivelDificuldade() * 0.2f);

        private readonly Queue<PontoColeta> _pool = new Queue<PontoColeta>();
        private Transform _raiz;
        private float _timer;
        private bool _prespawnFeito;

        protected override void AoInicializar() => _raiz = transform;

        private void Start() => TentarPrespawn();

        private void Update()
        {
            if (GameManager.Existe && GameManager.Instancia.Pausado) return;
            if (candidatos == null || candidatos.Length == 0) return;

            if (!_prespawnFeito) TentarPrespawn();

            if (SatisfacaoManager.Existe)
                SatisfacaoManager.Instancia.ReportarPendente(PontoColeta.TotalPendente);

            if (PontoColeta.Ativos.Count >= AlvoAtivosEf) return;

            _timer += Time.deltaTime;
            if (_timer < IntervaloEf) return;
            _timer = 0f;
            GerarPonto();
        }

        private void TentarPrespawn()
        {
            if (_prespawnFeito) return;
            if (candidatos == null || candidatos.Length == 0) return;
            for (int i = 0; i < prespawn; i++) GerarPonto();
            _prespawnFeito = true;
        }

        private void GerarPonto()
        {
            Vector3 pos;
            if (!EscolherPosicao(out pos)) return;

            int qtd = Random.Range(quantidadeMin, QuantidadeMaxEf + 1);
            Obter().Configurar(qtd, valorPorUnidade, pos);
        }

        /// <summary>Escolhe um candidato livre (longe de pontos existentes).</summary>
        private bool EscolherPosicao(out Vector3 pos)
        {
            for (int tent = 0; tent < 12; tent++)
            {
                Vector2 c = candidatos[Random.Range(0, candidatos.Length)];
                pos = new Vector3(c.x, c.y, 0f);
                if (LivrePerto(pos)) return true;
            }
            pos = Vector3.zero;
            return false;
        }

        private bool LivrePerto(Vector3 pos)
        {
            float m2 = distanciaMinima * distanciaMinima;
            for (int i = 0; i < PontoColeta.Ativos.Count; i++)
                if ((PontoColeta.Ativos[i].Posicao - pos).sqrMagnitude < m2) return false;
            return true;
        }

        private PontoColeta Obter() => _pool.Count > 0 ? _pool.Dequeue() : CriarNovo();

        private PontoColeta CriarNovo()
        {
            var go = new GameObject("PontoColeta");
            go.transform.SetParent(_raiz);
            var p = go.AddComponent<PontoColeta>();
            p.AoLiberar = Devolver;
            go.SetActive(false);
            return p;
        }

        private void Devolver(PontoColeta p)
        {
            if (p == null) return;
            p.transform.SetParent(_raiz);
            _pool.Enqueue(p);
        }
    }
}
