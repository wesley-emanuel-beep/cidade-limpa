using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CidadeLimpa.Core;
using CidadeLimpa.Data;

namespace CidadeLimpa.EditorTools
{
    /// <summary>
    /// Gera (ou carrega, se já existirem) os ScriptableObjects de dados do jogo a
    /// partir dos valores do GDD: 5 bairros, 3 modelos de caminhão, 4 eventos e a
    /// configuração de dificuldade. Usado pelo Scene Builder para popular a cena.
    /// </summary>
    public static class GeradorDeDados
    {
        public const string PastaDados = "Assets/_CidadeLimpa/Data";
        public const string PastaResources = "Assets/_CidadeLimpa/Resources";

        /// <summary>Conjunto de dados retornado ao Scene Builder.</summary>
        public class Pacote
        {
            public List<BairroData> bairros = new List<BairroData>();
            public List<CaminhaoData> caminhoes = new List<CaminhaoData>();
            public List<EventoData> eventos = new List<EventoData>();
            public DificuldadeConfig dificuldade;
            public BancoDeDados banco;
        }

        public static Pacote GerarTudo()
        {
            GarantirPasta();
            var pacote = new Pacote();

            pacote.bairros = GerarBairros();
            pacote.caminhoes = GerarCaminhoes();
            pacote.eventos = GerarEventos();
            pacote.dificuldade = GerarDificuldade();

            // Captura os caminhos ANTES do Refresh (o Refresh reimporta os .asset
            // recém-criados e invalida as instâncias em memória).
            var pBairros = pacote.bairros.Select(AssetDatabase.GetAssetPath).ToList();
            var pCaminhoes = pacote.caminhoes.Select(AssetDatabase.GetAssetPath).ToList();
            var pEventos = pacote.eventos.Select(AssetDatabase.GetAssetPath).ToList();
            var pDificuldade = AssetDatabase.GetAssetPath(pacote.dificuldade);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Recarrega referências FRESCAS do disco para evitar refs inválidas.
            pacote.bairros = pBairros.Select(AssetDatabase.LoadAssetAtPath<BairroData>).ToList();
            pacote.caminhoes = pCaminhoes.Select(AssetDatabase.LoadAssetAtPath<CaminhaoData>).ToList();
            pacote.eventos = pEventos.Select(AssetDatabase.LoadAssetAtPath<EventoData>).ToList();
            pacote.dificuldade = AssetDatabase.LoadAssetAtPath<DificuldadeConfig>(pDificuldade);

            // Banco central em Resources (fallback de runtime à prova de falhas).
            pacote.banco = GerarBanco(pacote);

            return pacote;
        }

        private static BancoDeDados GerarBanco(Pacote pacote)
        {
            if (!AssetDatabase.IsValidFolder(PastaResources))
            {
                Directory.CreateDirectory(PastaResources);
                AssetDatabase.Refresh();
            }

            string caminho = $"{PastaResources}/BancoDeDados.asset";
            var banco = ObterOuCriar<BancoDeDados>(caminho);
            banco.bairros = pacote.bairros;
            banco.caminhoes = pacote.caminhoes;
            banco.eventos = pacote.eventos;
            banco.dificuldade = pacote.dificuldade;
            EditorUtility.SetDirty(banco);
            AssetDatabase.SaveAssets();
            return banco;
        }

        private static void GarantirPasta()
        {
            if (!AssetDatabase.IsValidFolder(PastaDados))
            {
                Directory.CreateDirectory(PastaDados);
                AssetDatabase.Refresh();
            }
        }

        // ---------------- Bairros ----------------

        private static List<BairroData> GerarBairros()
        {
            var lista = new List<BairroData>
            {
                CriarBairro("Centro",  TipoBairro.Comercial,   Paleta.Eletrico, new Vector2(0f, 1f),   1.00f, 1.6f, 0.40f),
                CriarBairro("Oficinas", TipoBairro.Industrial, Paleta.Cinza,    new Vector2(-5.5f, 3f), 0.60f, 1.0f, 0.50f),
                CriarBairro("Humaita",  TipoBairro.Residencial, Paleta.Eco,     new Vector2(5.5f, 3f),  0.60f, 0.5f, 0.30f),
                CriarBairro("Dehon",    TipoBairro.Misto,       new Color(0.8f,0.55f,0.2f), new Vector2(-5.5f, -3f), 0.40f, 1.0f, 0.35f),
                CriarBairro("StoAntonioDePadua", TipoBairro.Residencial, new Color(0.6f,0.4f,0.8f), new Vector2(5.5f, -3f), 0.35f, 0.5f, 0.30f),
            };
            return lista;
        }

        private static BairroData CriarBairro(string nome, TipoBairro tipo, Color cor, Vector2 pos,
            float populacao, float multLixo, float chanceRec)
        {
            string caminho = $"{PastaDados}/Bairro_{nome}.asset";
            var d = ObterOuCriar<BairroData>(caminho);
            d.nomeBairro = nome.Replace("StoAntonioDePadua", "Sto. Antônio de Pádua").Replace("Humaita", "Humaitá");
            d.tipo = tipo;
            d.corIdentidade = cor;
            d.posicaoMapa = pos;
            d.raio = 1.3f;
            d.populacao = populacao;
            d.multiplicadorLixo = multLixo;
            d.chanceReciclavel = chanceRec;
            d.limiteSatisfatorio = 4;
            d.limiteCritico = 10;
            EditorUtility.SetDirty(d);
            return d;
        }

        // ---------------- Caminhões ----------------

        private static List<CaminhaoData> GerarCaminhoes()
        {
            var lista = new List<CaminhaoData>
            {
                CriarCaminhao("Coletor 01", "Caminhão de coleta da frota.",
                    Paleta.Eletrico, 4.5f, 100, 1.0f, 1.6f, 0),
                CriarCaminhao("Coletor 02", "Modelo intermediário: mais capacidade e eficiência.",
                    Paleta.Eco, 5.0f, 30, 1.3f, 1.6f, 400),
                CriarCaminhao("Coletor EX", "Modelo avançado: alta capacidade e descarga rápida.",
                    Paleta.Hex("FFC400"), 6.5f, 60, 1.8f, 1.2f, 1500),
            };
            return lista;
        }

        private static CaminhaoData CriarCaminhao(string nome, string desc, Color cor,
            float vel, int cap, float efi, float des, long custo)
        {
            string id = nome.Replace(" ", "");
            string caminho = $"{PastaDados}/Caminhao_{id}.asset";
            var d = ObterOuCriar<CaminhaoData>(caminho);
            d.nomeModelo = nome;
            d.descricao = desc;
            d.cor = cor;
            d.velocidade = vel;
            d.capacidade = cap;
            d.eficiencia = efi;
            d.tempoDescarregamento = des;
            d.tempoColetaPorUnidade = 0.05f;
            d.custo = custo;
            d.nivelMaxUpgrade = 5;
            d.ganhoPorUpgrade = 0.15f;
            EditorUtility.SetDirty(d);
            return d;
        }

        // ---------------- Eventos ----------------

        private static List<EventoData> GerarEventos()
        {
            var lista = new List<EventoData>
            {
                CriarEvento("Festival na Cidade", "Mais lixo gerado por 2 minutos.",
                    Paleta.Hex("FF8A00"), TipoEfeitoEvento.MultiplicadorLixo, 1.5f, 120f, 4),
                CriarEvento("Temporada de Chuva", "Lixo surge mais rápido.",
                    Paleta.Eletrico, TipoEfeitoEvento.MultiplicadorLixo, 1.4f, 90f, 4),
                CriarEvento("Campanha Ambiental", "Bônus +50% de sucata por 90 segundos.",
                    Paleta.Eco, TipoEfeitoEvento.MultiplicadorSucata, 1.5f, 90f, 4),
                CriarEvento("Mutirão Escolar", "Satisfação +10% imediata.",
                    Paleta.Hex("1ECC7A"), TipoEfeitoEvento.SatisfacaoImediata, 10f, 0f, 4),
            };
            return lista;
        }

        private static EventoData CriarEvento(string nome, string desc, Color cor,
            TipoEfeitoEvento tipo, float magnitude, float duracao, int nivelMin)
        {
            string id = nome.Replace(" ", "");
            string caminho = $"{PastaDados}/Evento_{id}.asset";
            var d = ObterOuCriar<EventoData>(caminho);
            d.nomeEvento = nome;
            d.descricao = desc;
            d.cor = cor;
            d.tipoEfeito = tipo;
            d.magnitude = magnitude;
            d.duracao = duracao;
            d.peso = 1f;
            d.nivelMinimo = nivelMin;
            EditorUtility.SetDirty(d);
            return d;
        }

        // ---------------- Dificuldade ----------------

        private static DificuldadeConfig GerarDificuldade()
        {
            string caminho = $"{PastaDados}/DificuldadeConfig.asset";
            var d = ObterOuCriar<DificuldadeConfig>(caminho);
            d.niveis = new[]
            {
                new DificuldadeConfig.Nivel { nome = "Tranquilo", lixosPorTick = 1, intervaloSegundos = 6f, eventosHabilitados = false, acumulosCriticos = false, sucataParaAvancar = 300 },
                new DificuldadeConfig.Nivel { nome = "Normal",    lixosPorTick = 1, intervaloSegundos = 5f, eventosHabilitados = false, acumulosCriticos = false, sucataParaAvancar = 800 },
                new DificuldadeConfig.Nivel { nome = "Intenso",   lixosPorTick = 2, intervaloSegundos = 5f, eventosHabilitados = false, acumulosCriticos = false, sucataParaAvancar = 1800 },
                new DificuldadeConfig.Nivel { nome = "Dinâmico",  lixosPorTick = 2, intervaloSegundos = 4f, eventosHabilitados = true,  acumulosCriticos = false, sucataParaAvancar = 3500 },
                new DificuldadeConfig.Nivel { nome = "Caótico",   lixosPorTick = 3, intervaloSegundos = 3f, eventosHabilitados = true,  acumulosCriticos = true,  sucataParaAvancar = long.MaxValue },
            };
            d.intervaloTentativaEvento = 40f;
            d.chanceEvento = 0.6f;
            EditorUtility.SetDirty(d);
            return d;
        }

        // ---------------- Util ----------------

        private static T ObterOuCriar<T>(string caminho) where T : ScriptableObject
        {
            var existente = AssetDatabase.LoadAssetAtPath<T>(caminho);
            if (existente != null) return existente;

            var novo = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(novo, caminho);
            return novo;
        }
    }
}
