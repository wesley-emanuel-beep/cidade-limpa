using System.Collections.Generic;
using UnityEngine;
using CidadeLimpa.Data;

namespace CidadeLimpa.Core
{
    /// <summary>
    /// Sorteia e aplica eventos aleatórios (GDD seção 08). Expõe os multiplicadores
    /// globais ativos (lixo, sucata) que outros sistemas leem. Só opera quando o
    /// nível de dificuldade atual habilita eventos.
    /// </summary>
    public class EventoManager : Singleton<EventoManager>
    {
        [Header("Catálogo")]
        [SerializeField] private List<EventoData> catalogo = new List<EventoData>();

        /// <summary>Multiplicador de geração de lixo dos eventos ativos.</summary>
        public float MultiplicadorLixo { get; private set; } = 1f;

        /// <summary>Multiplicador de sucata dos eventos ativos.</summary>
        public float MultiplicadorSucata { get; private set; } = 1f;

        // Eventos de duração atualmente ativos e seu tempo restante.
        private readonly List<EventoAtivo> _ativos = new List<EventoAtivo>();
        private float _timerTentativa;

        private class EventoAtivo
        {
            public EventoData dados;
            public float tempoRestante;
        }

        private void Start()
        {
            // Fallback: carrega o catálogo do banco se a cena não o atribuiu.
            if (catalogo == null || catalogo.Count == 0)
            {
                var banco = BancoDeDados.Carregar();
                if (banco != null && banco.eventos.Count > 0)
                    catalogo = new List<EventoData>(banco.eventos);
            }
        }

        private void Update()
        {
            AtualizarAtivos();
            TentarSortear();
        }

        private void AtualizarAtivos()
        {
            if (_ativos.Count == 0) return;

            bool recalcular = false;
            for (int i = _ativos.Count - 1; i >= 0; i--)
            {
                _ativos[i].tempoRestante -= Time.deltaTime;
                if (_ativos[i].tempoRestante <= 0f)
                {
                    GameEvents.DispararEventoFinalizado(_ativos[i].dados.nomeEvento);
                    _ativos.RemoveAt(i);
                    recalcular = true;
                }
            }

            if (recalcular)
                RecalcularMultiplicadores();
        }

        private void TentarSortear()
        {
            if (!DificuldadeManager.Existe) return;

            var dif = DificuldadeManager.Instancia;
            if (dif.Nivel == null || !dif.Nivel.eventosHabilitados) return;
            if (dif.Config == null) return;

            _timerTentativa += Time.deltaTime;
            if (_timerTentativa < dif.Config.intervaloTentativaEvento) return;
            _timerTentativa = 0f;

            if (Random.value > dif.Config.chanceEvento) return;

            var escolhido = SortearEvento(dif.NivelAtual);
            if (escolhido != null)
                Disparar(escolhido);
        }

        /// <summary>Sorteio ponderado entre eventos elegíveis para o nível atual.</summary>
        private EventoData SortearEvento(int nivelAtual)
        {
            float somaPeso = 0f;
            for (int i = 0; i < catalogo.Count; i++)
            {
                var e = catalogo[i];
                if (e != null && e.nivelMinimo <= nivelAtual)
                    somaPeso += e.peso;
            }

            if (somaPeso <= 0f) return null;

            float r = Random.value * somaPeso;
            for (int i = 0; i < catalogo.Count; i++)
            {
                var e = catalogo[i];
                if (e == null || e.nivelMinimo > nivelAtual) continue;
                r -= e.peso;
                if (r <= 0f) return e;
            }
            return null;
        }

        /// <summary>Aplica um evento imediatamente (também acessível para testes/debug).</summary>
        public void Disparar(EventoData evento)
        {
            if (evento == null) return;

            if (evento.EhImediato)
            {
                if (evento.tipoEfeito == TipoEfeitoEvento.SatisfacaoImediata && SatisfacaoManager.Existe)
                    SatisfacaoManager.Instancia.AplicarDelta(evento.magnitude);

                // Banner curto para feedback do evento imediato.
                GameEvents.DispararEventoIniciado(evento.nomeEvento, 2.5f);
                return;
            }

            _ativos.Add(new EventoAtivo { dados = evento, tempoRestante = evento.duracao });
            RecalcularMultiplicadores();
            GameEvents.DispararEventoIniciado(evento.nomeEvento, evento.duracao);
        }

        private void RecalcularMultiplicadores()
        {
            float lixo = 1f;
            float sucata = 1f;

            for (int i = 0; i < _ativos.Count; i++)
            {
                var d = _ativos[i].dados;
                switch (d.tipoEfeito)
                {
                    case TipoEfeitoEvento.MultiplicadorLixo: lixo *= d.magnitude; break;
                    case TipoEfeitoEvento.MultiplicadorSucata: sucata *= d.magnitude; break;
                }
            }

            MultiplicadorLixo = lixo;
            MultiplicadorSucata = sucata;
        }
    }
}
