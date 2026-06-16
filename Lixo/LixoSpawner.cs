using UnityEngine;
using CidadeLimpa.Core;
using CidadeLimpa.Bairros;

namespace CidadeLimpa.Lixo
{
    /// <summary>
    /// Gera resíduos nos bairros ao longo do tempo (GDD seções 04/10). A cadência
    /// vem do nível de dificuldade atual; a distribuição entre bairros é ponderada
    /// por população × multiplicador local; os multiplicadores de evento e do
    /// estado da cidade aceleram a geração. Lê tudo dos managers — sem acoplamento.
    /// </summary>
    public class LixoSpawner : MonoBehaviour
    {
        [Header("Fallback (sem DificuldadeManager)")]
        [SerializeField] private int lixosPorTickFallback = 1;
        [SerializeField] private float intervaloFallback = 5f;

        [Header("Início")]
        [Tooltip("Quantidade de lixo já presente quando o jogo começa (cidade viva).")]
        [SerializeField] private int prespawnInicial = 10;

        private float _timer;
        private bool _prespawnFeito;

        private void Start()
        {
            // Pré-popula a cidade para haver coleta imediata (caminhão sai do depósito).
            TentarPrespawn();
        }

        private void TentarPrespawn()
        {
            if (_prespawnFeito) return;
            if (Bairro.Todos.Count == 0 || !LixoPool.Existe) return;

            for (int i = 0; i < prespawnInicial; i++)
                GerarLixoEmBairroPonderado();
            _prespawnFeito = true;
        }

        private void Update()
        {
            if (GameManager.Existe && GameManager.Instancia.Pausado) return;
            if (Bairro.Todos.Count == 0 || !LixoPool.Existe) return;

            if (!_prespawnFeito) TentarPrespawn();

            float intervalo = ObterIntervalo();
            _timer += Time.deltaTime;
            if (_timer < intervalo) return;
            _timer = 0f;

            int quantidade = ObterLixosPorTick();
            for (int i = 0; i < quantidade; i++)
                GerarLixoEmBairroPonderado();
        }

        private float ObterIntervalo()
        {
            float intervalo = intervaloFallback;
            if (DificuldadeManager.Existe && DificuldadeManager.Instancia.Nivel != null)
                intervalo = DificuldadeManager.Instancia.Nivel.intervaloSegundos;

            // Eventos/estado aumentam a frequência (dividindo o intervalo).
            float acelerador = 1f;
            if (EventoManager.Existe) acelerador *= EventoManager.Instancia.MultiplicadorLixo;
            if (SatisfacaoManager.Existe) acelerador *= SatisfacaoManager.Instancia.MultiplicadorLixoEstado;

            return Mathf.Max(0.25f, intervalo / Mathf.Max(0.01f, acelerador));
        }

        private int ObterLixosPorTick()
        {
            if (DificuldadeManager.Existe && DificuldadeManager.Instancia.Nivel != null)
                return Mathf.Max(1, DificuldadeManager.Instancia.Nivel.lixosPorTick);
            return Mathf.Max(1, lixosPorTickFallback);
        }

        private bool AcumulosCriticosHabilitados()
        {
            return DificuldadeManager.Existe
                && DificuldadeManager.Instancia.Nivel != null
                && DificuldadeManager.Instancia.Nivel.acumulosCriticos;
        }

        /// <summary>Escolhe um bairro por sorteio ponderado e gera um resíduo nele.</summary>
        private void GerarLixoEmBairroPonderado()
        {
            var bairro = SortearBairro();
            if (bairro == null || bairro.Dados == null) return;

            var dados = bairro.Dados;
            TipoLixo tipo;
            var categoria = (CategoriaReciclavel)Random.Range(0, 4);

            // Grande acúmulo quando o bairro já passou do limite crítico.
            if (AcumulosCriticosHabilitados()
                && bairro.QuantidadeLixo >= dados.limiteCritico
                && Random.value < 0.25f)
            {
                tipo = TipoLixo.GrandeAcumulo;
            }
            else if (Random.value < dados.chanceReciclavel)
            {
                tipo = TipoLixo.Reciclavel;
            }
            else
            {
                tipo = TipoLixo.Comum;
            }

            var item = LixoPool.Instancia.Obter(tipo, categoria, bairro.PosicaoAleatoria());
            bairro.AdicionarLixo(item);
        }

        private Bairro SortearBairro()
        {
            float soma = 0f;
            for (int i = 0; i < Bairro.Todos.Count; i++)
            {
                var b = Bairro.Todos[i];
                if (b.Dados == null) continue;
                soma += b.Dados.populacao * b.Dados.multiplicadorLixo;
            }
            if (soma <= 0f) return Bairro.Todos.Count > 0 ? Bairro.Todos[0] : null;

            float r = Random.value * soma;
            for (int i = 0; i < Bairro.Todos.Count; i++)
            {
                var b = Bairro.Todos[i];
                if (b.Dados == null) continue;
                r -= b.Dados.populacao * b.Dados.multiplicadorLixo;
                if (r <= 0f) return b;
            }
            return Bairro.Todos[Bairro.Todos.Count - 1];
        }
    }
}
