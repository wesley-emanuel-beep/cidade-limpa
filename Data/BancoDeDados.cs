using System.Collections.Generic;
using UnityEngine;

namespace CidadeLimpa.Data
{
    /// <summary>
    /// Banco central de dados do jogo (bairros, caminhões, eventos, dificuldade).
    /// Fica em Resources para que qualquer manager possa carregá-lo em runtime como
    /// fallback caso a referência atribuída na cena esteja faltando — eliminando a
    /// fragilidade de "referência nula" quando a montagem da cena falha.
    /// </summary>
    public class BancoDeDados : ScriptableObject
    {
        public List<BairroData> bairros = new List<BairroData>();
        public List<CaminhaoData> caminhoes = new List<CaminhaoData>();
        public List<EventoData> eventos = new List<EventoData>();
        public DificuldadeConfig dificuldade;

        /// <summary>Caminho dentro de uma pasta Resources (sem extensão).</summary>
        public const string CaminhoResources = "BancoDeDados";

        private static BancoDeDados _cache;

        /// <summary>Carrega o banco de dados de Resources (com cache).</summary>
        public static BancoDeDados Carregar()
        {
            if (_cache == null)
                _cache = Resources.Load<BancoDeDados>(CaminhoResources);
            return _cache;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Resetar() => _cache = null;
    }
}
