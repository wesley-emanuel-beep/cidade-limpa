using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using CidadeLimpa.Core;
using CidadeLimpa.Data;
using CidadeLimpa.Bairros;
using CidadeLimpa.Caminhoes;
using CidadeLimpa.Lixo;
using CidadeLimpa.Coleta;
using CidadeLimpa.UI;
using CidadeLimpa.CameraSystem;
using CidadeLimpa.GameInput;
using CidadeLimpa.Mapa;

namespace CidadeLimpa.EditorTools
{
    /// <summary>
    /// SCENE BUILDER — monta automaticamente a cena completa de "Cidade Limpa" a
    /// partir das especificações do GDD: gera/lê os dados, cria a hierarquia
    /// organizada, configura câmera, iluminação, Canvas, Input/Event System e
    /// instancia todos os managers e sistemas. É idempotente: executar de novo
    /// recria a cena inteira do zero.
    /// </summary>
    public class CidadeLimpaSceneBuilder : EditorWindow
    {
        private const string CaminhoCena = "Assets/Scenes/CidadeLimpa.unity";
        private const string CaminhoMenu = "Assets/Scenes/Menu.unity";

        // Sprites de arte fornecidos pelo usuário.
        private const string PathMapa = "Assets/mapa_estilizado.png";
        private const string PathEstrada = "Assets/estrada.png";
        private const string PathCaminhao = "Assets/sprites/caminhao.png";
        private const string PathLixo = "Assets/sprites/lixo.png";
        private const string PathDeposito = "Assets/sprites/deposito.png";
        private const string PathPontosCidade = "Assets/_CidadeLimpa/Data/pontos_cidade.txt";
        private const string PathPontosEstrada = "Assets/_CidadeLimpa/Data/estrada_path.txt";

        // Pixels-por-unidade dos sprites (mapa e estrada DEVEM ser iguais p/ alinhar).
        private const float PpuMapa = 200f;
        private const float PpuCaminhao = 320f;   // caminhão menor
        private const float PpuDeposito = 130f;

        [MenuItem("CidadeLimpa/Scene Builder %#b", false, 0)]
        public static void AbrirJanela()
        {
            var janela = GetWindow<CidadeLimpaSceneBuilder>("Cidade Limpa");
            janela.minSize = new Vector2(340f, 220f);
            janela.Show();
        }

        [MenuItem("CidadeLimpa/Build Scene (direto)", false, 1)]
        public static void BuildDireto() => Construir();

        private void OnGUI()
        {
            GUILayout.Space(12);
            var titulo = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            GUILayout.Label("CIDADE LIMPA", titulo);
            GUILayout.Label("Tubarão Sustentável — Scene Builder", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(16);

            EditorGUILayout.HelpBox(
                "Gera os dados (bairros, caminhões, eventos, dificuldade) e monta a cena " +
                "completa: mapa, câmera 2.5D, iluminação, managers, frota, UI e Input.\n\n" +
                "Reexecutar reconstrói a cena inteira.", MessageType.Info);

            GUILayout.Space(12);

            var estiloBotao = new GUIStyle(GUI.skin.button) { fontSize = 16, fixedHeight = 48, fontStyle = FontStyle.Bold };
            GUI.backgroundColor = new Color(0.12f, 0.8f, 0.48f);
            if (GUILayout.Button("[ Build Scene ]", estiloBotao))
                Construir();
            GUI.backgroundColor = Color.white;

            GUILayout.Space(6);
            if (GUILayout.Button("Apenas (re)gerar dados", GUILayout.Height(26)))
            {
                GeradorDeDados.GerarTudo();
                Debug.Log("[SceneBuilder] Dados gerados em " + GeradorDeDados.PastaDados);
            }
        }

        // ==================================================================
        //  Construção
        // ==================================================================

        public static void Construir()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Cidade Limpa", "Saia do Play Mode antes de montar a cena.", "OK");
                return;
            }

            if (!GarantirTMPEssentials())
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Cidade Limpa",
                    "Os recursos essenciais do TextMeshPro foram importados agora.\n\n" +
                    "Clique em [ Build Scene ] novamente para montar a cena.", "OK");
                return;
            }

            try
            {
            EditorUtility.DisplayProgressBar("Cidade Limpa", "Gerando dados...", 0.1f);
            var dados = GeradorDeDados.GerarTudo();

            // Importa a arte como sprites.
            var spriteMapa = ImportarSprite(PathMapa, PpuMapa, 4096);
            var spriteEstrada = ImportarSprite(PathEstrada, PpuMapa, 4096);
            var spriteCaminhao = ImportarSprite(PathCaminhao, PpuCaminhao, 512);
            var spriteDeposito = ImportarSprite(PathDeposito, PpuDeposito, 512);

            // Candidatos de pontos de coleta espalhados pelas ruas (mundo).
            var candidatosCidade = CarregarPontosCidade();

            GarantirTagsLayers();

            EditorUtility.DisplayProgressBar("Cidade Limpa", "Criando cena...", 0.25f);
            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Raízes da hierarquia (estrutura sugerida no briefing).
            var environment = Raiz("=== ENVIRONMENT ===");
            var gameplay = Raiz("=== GAMEPLAY ===");
            var characters = Raiz("=== CHARACTERS ===");
            var vehicles = Raiz("=== VEHICLES ===");
            var props = Raiz("=== PROPS ===");
            var uiRoot = Raiz("=== UI ===");
            var audioRoot = Raiz("=== AUDIO ===");
            var systems = Raiz("=== SYSTEMS ===");
            var managers = Raiz("=== MANAGERS ===");

            EditorUtility.DisplayProgressBar("Cidade Limpa", "Iluminação e câmera...", 0.4f);
            ConfigurarAmbiente();
            ConfigurarIluminacao(environment);
            var cam = ConfigurarCamera();

            EditorUtility.DisplayProgressBar("Cidade Limpa", "Mapa e estrada...", 0.55f);
            ConstruirMapa(environment, spriteMapa, spriteEstrada);

            EditorUtility.DisplayProgressBar("Cidade Limpa", "Gameplay...", 0.7f);
            ConstruirDeposito(gameplay, spriteDeposito);
            ConstruirSistemasColeta(gameplay, candidatosCidade);
            ConstruirManagers(managers, audioRoot, dados);
            ConstruirFrota(vehicles, dados, spriteCaminhao);

            EditorUtility.DisplayProgressBar("Cidade Limpa", "Input e UI...", 0.85f);
            ConstruirInput(systems, cam);
            ConstruirUI(uiRoot, dados);

            EditorUtility.DisplayProgressBar("Cidade Limpa", "Salvando jogo...", 0.9f);
            SalvarCena(cena, CaminhoCena);

            EditorUtility.DisplayProgressBar("Cidade Limpa", "Montando menu...", 0.95f);
            var cenaMenu = ConstruirCenaMenu();
            SalvarCena(cenaMenu, CaminhoMenu);

            ConfigurarBuildSettings();
            IdentidadeProjeto.Aplicar();

            Debug.Log("<color=#1ECC7A><b>[SceneBuilder] Cenas 'Menu' e 'Cidade Limpa' montadas com sucesso!</b></color>");
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Cidade Limpa",
                "Tudo pronto!\n\nA cena de Menu está aberta — pressione Play para começar pelo menu.\n\nNo jogo: clique num caminhão e depois num bairro.", "Show!");
            }
            catch (System.Exception e)
            {
                // Garante que a barra nunca fique "presa" e que o erro apareça.
                EditorUtility.ClearProgressBar();
                Debug.LogError("[SceneBuilder] Falha ao montar a cena:\n" + e);
                EditorUtility.DisplayDialog("Cidade Limpa — erro",
                    "Ocorreu um erro ao montar a cena. Veja o Console para detalhes:\n\n" + e.Message, "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>Monta a cena de menu principal (câmera, canvas, MenuPrincipal).</summary>
        private static Scene ConstruirCenaMenu()
        {
            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGO = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Paleta.Papel;
            camGO.transform.position = new Vector3(0f, 0f, -10f);
            camGO.AddComponent<AudioListener>();

            var canvasGO = new GameObject("Canvas", typeof(RectTransform));
            canvasGO.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGO.AddComponent<MenuPrincipal>();

            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            var modulo = esGO.AddComponent<InputSystemUIInputModule>();
            ConfigurarModuloUI(modulo);

            return cena;
        }

        // ==================================================================
        //  Ambiente / Iluminação / Câmera
        // ==================================================================

        private static void ConfigurarAmbiente()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.85f, 0.87f, 0.9f);
            RenderSettings.fog = false;
        }

        private static void ConfigurarIluminacao(GameObject pai)
        {
            var go = new GameObject("Luz Direcional");
            go.transform.SetParent(pai.transform);
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var luz = go.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.color = Color.white;
            luz.intensity = 1.05f;
            luz.shadows = LightShadows.Soft;
        }

        private static CameraController ConfigurarCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";

            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 3.6f; // nível de rua (mapa estilizado não pixela)
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Paleta.Papel;
            cam.transform.position = new Vector3(0f, -0.3f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;

            go.AddComponent<AudioListener>();

            // Mapa ~20.5 x 14 unidades (PPU 200): limites de pan dentro do mapa.
            var ctrl = go.AddComponent<CameraController>();
            ctrl.Configurar(new Vector2(-7f, -5f), new Vector2(7f, 5.5f), 2.6f, 6.5f);
            return ctrl;
        }

        // ==================================================================
        //  Mapa
        // ==================================================================

        /// <summary>
        /// Monta o mapa: foto aérea (embaixo) + traçado da estrada (em cima,
        /// mesmo transform = alinhado) + o caminho navegável (RoadPath) com os
        /// waypoints extraídos da arte.
        /// </summary>
        private static void ConstruirMapa(GameObject pai, Sprite spriteMapa, Sprite spriteEstrada)
        {
            var raiz = new GameObject("Mapa");
            raiz.transform.SetParent(pai.transform);

            // Mapa estilizado achatado (camada de baixo).
            var mapa = new GameObject("MapaBase");
            mapa.transform.SetParent(raiz.transform);
            var srMapa = mapa.AddComponent<SpriteRenderer>();
            srMapa.sprite = spriteMapa;
            srMapa.sortingOrder = -30;

            // Traçado das ruas (em cima, mesmo transform → alinhado). É a "malha
            // de rotas" da cidade toda; as rotas vermelhas são dinâmicas por caminhão.
            var estrada = new GameObject("Estrada");
            estrada.transform.SetParent(raiz.transform);
            estrada.transform.localPosition = Vector3.zero;
            var srEstrada = estrada.AddComponent<SpriteRenderer>();
            srEstrada.sprite = spriteEstrada;
            srEstrada.sortingOrder = -20;
        }

        /// <summary>Desenha a rota principal como uma linha vermelha grossa (LineRenderer).</summary>
        private static void DesenharRota(GameObject pai, List<Vector2> pontos)
        {
            var go = new GameObject("RotaVisual");
            go.transform.SetParent(pai.transform);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.alignment = LineAlignment.View;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 4;
            lr.widthMultiplier = 0.22f;
            lr.sortingOrder = -10; // acima da estrada, abaixo dos pontos/caminhão
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = Paleta.Vermelho;

            lr.positionCount = pontos.Count;
            for (int i = 0; i < pontos.Count; i++)
                lr.SetPosition(i, new Vector3(pontos[i].x, pontos[i].y, -0.1f));
        }

        private static void ConstruirBairros(GameObject pai, List<BairroData> bairros, List<Vector2> pontos)
        {
            var root = new GameObject("Bairros");
            root.transform.SetParent(pai.transform);

            // Frações ao longo da estrada onde cada bairro é ancorado.
            float[] fracoes = { 0.12f, 0.32f, 0.50f, 0.70f, 0.90f };

            for (int i = 0; i < bairros.Count; i++)
            {
                var dados = bairros[i];
                if (dados == null) continue;

                var go = new GameObject("Bairro_" + dados.nomeBairro);
                go.transform.SetParent(root.transform);

                Vector3 posicao = (pontos != null && pontos.Count >= 2)
                    ? PontoNaFracao(pontos, fracoes[i % fracoes.Length])
                    : new Vector3(dados.posicaoMapa.x, dados.posicaoMapa.y, 0f);
                go.transform.position = posicao;
                TentarTag(go, "Bairro");

                var bairro = go.AddComponent<Bairro>();
                DefinirRef(bairro, "dados", dados);
                DefinirInt(bairro, "indiceBanco", i);
                go.AddComponent<BairroVisual>();

                // Rótulo do bairro.
                var label = new GameObject("Label");
                label.transform.SetParent(go.transform);
                label.transform.localPosition = new Vector3(0f, dados.raio + 0.5f, -1f);
                var tm = label.AddComponent<TMPro.TextMeshPro>();
                tm.text = dados.nomeBairro.ToUpperInvariant();
                tm.fontSize = 3.5f;
                tm.alignment = TMPro.TextAlignmentOptions.Center;
                tm.color = Paleta.Papel;
                tm.fontStyle = TMPro.FontStyles.Bold;
                tm.outlineWidth = 0.25f;
                tm.outlineColor = new Color32(10, 10, 10, 255);
                tm.sortingOrder = 16;
            }
        }

        // ==================================================================
        //  Gameplay
        // ==================================================================

        private static void ConstruirDeposito(GameObject pai, Sprite spriteDeposito)
        {
            var go = new GameObject("Deposito");
            go.transform.SetParent(pai.transform);
            go.transform.position = new Vector3(-6.4f, 3.0f, 0f); // canto isolado da cidade
            TentarTag(go, "Deposito");
            go.AddComponent<Deposito>();

            // Prédio (sprite Bauhaus). Fallback: bloco amarelo.
            var vis = new GameObject("Visual");
            vis.transform.SetParent(go.transform, false);
            var sr = vis.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            if (spriteDeposito != null)
            {
                sr.sprite = spriteDeposito;
            }
            else
            {
                sr.sprite = SpriteQuadrado.Obter();
                sr.color = Paleta.Amarelo;
                vis.transform.localScale = new Vector3(1.6f, 1.2f, 1f);
            }

            // Rótulo logo acima do prédio.
            var label = new GameObject("Label");
            label.transform.SetParent(go.transform);
            label.transform.localPosition = new Vector3(0f, 1.45f, -1f);
            var tm = label.AddComponent<TMPro.TextMeshPro>();
            tm.text = "DEPÓSITO";
            tm.fontSize = 3.2f;
            tm.fontStyle = TMPro.FontStyles.Bold;
            tm.alignment = TMPro.TextAlignmentOptions.Center;
            tm.color = Paleta.Tinta;
            tm.outlineWidth = 0.2f;
            tm.outlineColor = new Color32(244, 241, 234, 255);
            tm.sortingOrder = 10;
        }

        private static void ConstruirSistemasColeta(GameObject pai, List<Vector2> candidatos)
        {
            var spawner = new GameObject("PontoColetaSpawner");
            spawner.transform.SetParent(pai.transform);
            var sp = spawner.AddComponent<PontoColetaSpawner>();
            if (candidatos != null && candidatos.Count > 0)
                DefinirVetores(sp, "candidatos", candidatos);
            else
                Debug.LogWarning("[SceneBuilder] Sem candidatos de cidade — pontos não vão spawnar.");
        }

        private static void ConstruirManagers(GameObject pai, GameObject audioRoot, GeradorDeDados.Pacote dados)
        {
            AdicionarManager<GameManager>(pai, "GameManager");
            AdicionarManager<SucataManager>(pai, "SucataManager");
            AdicionarManager<SatisfacaoManager>(pai, "SatisfacaoManager");
            AdicionarManager<MelhoriasGlobais>(pai, "MelhoriasGlobais");

            var dif = AdicionarManager<DificuldadeManager>(pai, "DificuldadeManager");
            DefinirRef(dif, "config", dados.dificuldade);

            var eventos = AdicionarManager<EventoManager>(pai, "EventoManager");
            DefinirLista(eventos, "catalogo", dados.eventos.ConvertAll(e => (Object)e));

            // AudioManager vai num filho da raiz de áudio (não renomeia a raiz).
            var audioGO = new GameObject("AudioManager");
            audioGO.transform.SetParent(audioRoot.transform);
            audioGO.AddComponent<AudioManager>();
        }

        private static void ConstruirFrota(GameObject pai, GeradorDeDados.Pacote dados, Sprite spriteCaminhao)
        {
            var go = new GameObject("FrotaManager");
            go.transform.SetParent(pai.transform);
            var frota = go.AddComponent<FrotaManager>();

            if (dados.caminhoes.Count > 0)
                DefinirRef(frota, "caminhaoInicial", dados.caminhoes[0]);
            if (spriteCaminhao != null)
                DefinirRef(frota, "spriteCaminhao", spriteCaminhao);
        }

        // ==================================================================
        //  Input / UI
        // ==================================================================

        private static void ConstruirInput(GameObject pai, CameraController cam)
        {
            var im = new GameObject("InputManager");
            im.transform.SetParent(pai.transform);
            im.AddComponent<InputManager>();

            var ce = new GameObject("ControleEntrada");
            ce.transform.SetParent(pai.transform);
            var controle = ce.AddComponent<ControleEntrada>();
            DefinirRef(controle, "cameraCtrl", cam);
        }

        private static void ConstruirUI(GameObject pai, GeradorDeDados.Pacote dados)
        {
            // Canvas.
            var canvasGO = new GameObject("Canvas", typeof(RectTransform));
            canvasGO.transform.SetParent(pai.transform);
            canvasGO.layer = LayerMask.NameToLayer("UI");

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            var uiManager = canvasGO.AddComponent<UIManager>();
            DefinirLista(uiManager, "modelosVenda", dados.caminhoes.ConvertAll(c => (Object)c));

            // Event System com o módulo do novo Input System.
            var esGO = new GameObject("EventSystem");
            esGO.transform.SetParent(pai.transform);
            esGO.AddComponent<EventSystem>();
            var modulo = esGO.AddComponent<InputSystemUIInputModule>();
            ConfigurarModuloUI(modulo); // sem isto os cliques de UI não funcionam
        }

        // ==================================================================
        //  Input System UI
        // ==================================================================

        /// <summary>
        /// Configura o módulo de UI do Input System usando o asset de ações do
        /// projeto (InputSystem_Actions.inputactions). Evita AssignDefaultActions(),
        /// que lança exceção nesta versão do pacote. Zera as referências antes de
        /// trocar o asset para não disparar o mesmo erro, e religa cada ação da UI.
        /// </summary>
        private static void ConfigurarModuloUI(InputSystemUIInputModule modulo)
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (asset == null)
            {
                Debug.LogWarning("[SceneBuilder] 'Assets/InputSystem_Actions.inputactions' não encontrado — a UI pode não responder a cliques.");
                return;
            }

            // Limpa referências atuais (evita re-resolver refs órfãs e lançar exceção).
            modulo.point = modulo.move = modulo.leftClick = modulo.rightClick =
                modulo.middleClick = modulo.scrollWheel = modulo.submit =
                modulo.cancel = modulo.trackedDevicePosition = modulo.trackedDeviceOrientation = null;

            modulo.actionsAsset = asset;

            modulo.point = RefAcao(asset, "UI/Point");
            modulo.leftClick = RefAcao(asset, "UI/Click");
            modulo.middleClick = RefAcao(asset, "UI/MiddleClick");
            modulo.rightClick = RefAcao(asset, "UI/RightClick");
            modulo.scrollWheel = RefAcao(asset, "UI/ScrollWheel");
            modulo.move = RefAcao(asset, "UI/Navigate");
            modulo.submit = RefAcao(asset, "UI/Submit");
            modulo.cancel = RefAcao(asset, "UI/Cancel");
            modulo.trackedDevicePosition = RefAcao(asset, "UI/TrackedDevicePosition");
            modulo.trackedDeviceOrientation = RefAcao(asset, "UI/TrackedDeviceOrientation");
        }

        private static InputActionReference RefAcao(InputActionAsset asset, string caminho)
        {
            var acao = asset.FindAction(caminho);
            return acao != null ? InputActionReference.Create(acao) : null;
        }

        // ==================================================================
        //  Salvar
        // ==================================================================

        private static void SalvarCena(Scene cena, string caminho)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena, caminho);
        }

        /// <summary>Build Settings: Menu (índice 0) e depois o Jogo.</summary>
        private static void ConfigurarBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(CaminhoMenu, true),
                new EditorBuildSettingsScene(CaminhoCena, true),
            };
        }

        // ==================================================================
        //  Arte / Estrada
        // ==================================================================

        /// <summary>Configura uma textura como Sprite (pivot central) e a retorna.</summary>
        private static Sprite ImportarSprite(string caminho, float ppu, int maxTamanho)
        {
            if (!File.Exists(caminho))
            {
                Debug.LogWarning($"[SceneBuilder] Imagem não encontrada: {caminho}");
                return null;
            }

            var ti = AssetImporter.GetAtPath(caminho) as TextureImporter;
            if (ti == null)
            {
                Debug.LogWarning($"[SceneBuilder] Não foi possível importar: {caminho}");
                return null;
            }

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = ppu;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = maxTamanho;

            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.Center;
            s.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);

            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
        }

        /// <summary>
        /// Lê o arquivo de pontos da estrada (pixels) e converte para o espaço
        /// local do sprite da estrada (pivot central, PPU do mapa).
        /// </summary>
        /// <summary>
        /// Carrega os candidatos de ponto de coleta (pixels nas ruas) e converte
        /// para coordenadas de mundo (mapa com pivot central, PPU do mapa).
        /// </summary>
        private static List<Vector2> CarregarPontosCidade()
        {
            if (!File.Exists(PathPontosCidade))
            {
                Debug.LogWarning($"[SceneBuilder] Arquivo de candidatos não encontrado: {PathPontosCidade}");
                return null;
            }

            float W = 4096f, H = 2816f;
            var pontos = new List<Vector2>();
            foreach (var linhaRaw in File.ReadAllLines(PathPontosCidade))
            {
                string linha = linhaRaw.Trim();
                if (linha.Length == 0) continue;
                if (linha.StartsWith("#"))
                {
                    foreach (var tok in linha.Split(' '))
                    {
                        if (tok.StartsWith("W=")) float.TryParse(tok.Substring(2), out W);
                        else if (tok.StartsWith("H=")) float.TryParse(tok.Substring(2), out H);
                    }
                    continue;
                }
                var partes = linha.Split(',');
                if (partes.Length < 2) continue;
                if (!float.TryParse(partes[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float px)) continue;
                if (!float.TryParse(partes[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float py)) continue;
                pontos.Add(new Vector2((px - W * 0.5f) / PpuMapa, (H * 0.5f - py) / PpuMapa));
            }
            return pontos.Count > 0 ? pontos : null;
        }

        private static List<Vector2> CarregarPontosEstrada()
        {
            if (!File.Exists(PathPontosEstrada))
            {
                Debug.LogWarning($"[SceneBuilder] Arquivo de pontos não encontrado: {PathPontosEstrada}");
                return null;
            }

            // Dimensões da textura (para centralizar). Cabeçalho traz W e H.
            float W = 4096f, H = 2816f;
            var pontos = new List<Vector2>();

            foreach (var linhaRaw in File.ReadAllLines(PathPontosEstrada))
            {
                string linha = linhaRaw.Trim();
                if (linha.Length == 0) continue;
                if (linha.StartsWith("#"))
                {
                    foreach (var tok in linha.Split(' '))
                    {
                        if (tok.StartsWith("W=")) float.TryParse(tok.Substring(2), out W);
                        else if (tok.StartsWith("H=")) float.TryParse(tok.Substring(2), out H);
                    }
                    continue;
                }

                var partes = linha.Split(',');
                if (partes.Length < 2) continue;
                if (!float.TryParse(partes[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float px)) continue;
                if (!float.TryParse(partes[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float py)) continue;

                // pixel (origem topo-esq) → local (origem centro, Y para cima).
                float lx = (px - W * 0.5f) / PpuMapa;
                float ly = (H * 0.5f - py) / PpuMapa;
                pontos.Add(new Vector2(lx, ly));
            }

            if (pontos.Count < 2)
            {
                Debug.LogWarning("[SceneBuilder] Pontos da estrada insuficientes.");
                return null;
            }
            return pontos;
        }

        /// <summary>Posição (mundo, z=0) a uma fração [0..1] do comprimento da polilinha.</summary>
        private static Vector3 PontoNaFracao(List<Vector2> pts, float fracao)
        {
            if (pts == null || pts.Count == 0) return Vector3.zero;
            if (pts.Count == 1) return pts[0];

            float total = 0f;
            for (int i = 1; i < pts.Count; i++) total += Vector2.Distance(pts[i - 1], pts[i]);
            float alvo = Mathf.Clamp01(fracao) * total;

            float acum = 0f;
            for (int i = 1; i < pts.Count; i++)
            {
                float seg = Vector2.Distance(pts[i - 1], pts[i]);
                if (acum + seg >= alvo)
                {
                    float t = seg > 1e-4f ? (alvo - acum) / seg : 0f;
                    Vector2 p = Vector2.Lerp(pts[i - 1], pts[i], t);
                    return new Vector3(p.x, p.y, 0f);
                }
                acum += seg;
            }
            Vector2 ult = pts[pts.Count - 1];
            return new Vector3(ult.x, ult.y, 0f);
        }

        /// <summary>Atribui um array serializado de Vector2 via SerializedObject.</summary>
        private static void DefinirVetores(Component c, string campo, List<Vector2> valores)
        {
            var so = new SerializedObject(c);
            var p = so.FindProperty(campo);
            if (p == null) { Debug.LogWarning($"[SceneBuilder] Array '{campo}' não encontrado em {c.GetType().Name}."); return; }

            p.ClearArray();
            for (int i = 0; i < valores.Count; i++)
            {
                p.InsertArrayElementAtIndex(i);
                p.GetArrayElementAtIndex(i).vector2Value = valores[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ==================================================================
        //  Utilitários
        // ==================================================================

        private static GameObject Raiz(string nome)
        {
            var go = new GameObject(nome);
            return go;
        }

        private static T AdicionarManager<T>(GameObject pai, string nome) where T : Component
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai.transform);
            return go.AddComponent<T>();
        }

        private static GameObject NovoSprite(string nome, Transform pai, Color cor, int ordem)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteQuadrado.Obter();
            sr.color = cor;
            sr.sortingOrder = ordem;
            return go;
        }

        private static void DefinirRef(Component c, string campo, Object valor)
        {
            var so = new SerializedObject(c);
            var p = so.FindProperty(campo);
            if (p != null) { p.objectReferenceValue = valor; so.ApplyModifiedPropertiesWithoutUndo(); }
            else Debug.LogWarning($"[SceneBuilder] Campo '{campo}' não encontrado em {c.GetType().Name}.");
        }

        private static void DefinirInt(Component c, string campo, int valor)
        {
            var so = new SerializedObject(c);
            var p = so.FindProperty(campo);
            if (p != null) { p.intValue = valor; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        private static void DefinirLista(Component c, string campo, List<Object> valores)
        {
            var so = new SerializedObject(c);
            var p = so.FindProperty(campo);
            if (p == null) { Debug.LogWarning($"[SceneBuilder] Lista '{campo}' não encontrada em {c.GetType().Name}."); return; }

            p.ClearArray();
            for (int i = 0; i < valores.Count; i++)
            {
                p.InsertArrayElementAtIndex(i);
                p.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- TextMeshPro Essentials ----

        /// <summary>
        /// Garante que os recursos essenciais do TMP (fonte padrão, settings) estão
        /// no projeto. Importa o pacote silenciosamente na primeira vez. Retorna
        /// true se já estavam presentes (build pode prosseguir).
        /// </summary>
        private static bool GarantirTMPEssentials()
        {
            var encontrados = AssetDatabase.FindAssets("t:TMP_Settings");
            if (encontrados != null && encontrados.Length > 0)
                return true;

            string pacote = LocalizarTMPEssentials();
            if (string.IsNullOrEmpty(pacote))
            {
                Debug.LogWarning("[SceneBuilder] Pacote 'TMP Essential Resources' não encontrado. " +
                                 "Importe manualmente: Window > TextMeshPro > Import TMP Essential Resources.");
                return true; // não bloqueia o build; textos podem ficar sem fonte padrão
            }

            Debug.Log("[SceneBuilder] Importando TMP Essential Resources...");
            AssetDatabase.ImportPackage(pacote, false);
            AssetDatabase.Refresh();
            return false;
        }

        private static string LocalizarTMPEssentials()
        {
            const string nome = "TMP Essential Resources.unitypackage";

            // Caminho virtual do pacote ugui (TMP foi incorporado a ele no Unity 6).
            string direto = Path.GetFullPath(Path.Combine("Packages", "com.unity.ugui", "Package Resources", nome));
            if (File.Exists(direto)) return direto;

            // Fallback: varre o PackageCache.
            string cache = Path.Combine(Directory.GetCurrentDirectory(), "Library", "PackageCache");
            if (Directory.Exists(cache))
            {
                var achado = Directory.GetFiles(cache, nome, SearchOption.AllDirectories).FirstOrDefault();
                if (!string.IsNullOrEmpty(achado)) return achado;
            }
            return null;
        }

        // ---- Tags & Layers ----

        private static void GarantirTagsLayers()
        {
            EnsureTag("Bairro");
            EnsureTag("Deposito");
            EnsureTag("Caminhao");
            EnsureTag("Lixo");
        }

        private static void TentarTag(GameObject go, string tag)
        {
            try { go.tag = tag; } catch { /* tag pode não existir ainda */ }
        }

        private static void EnsureTag(string tag)
        {
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (asset.Length == 0) return;

            var so = new SerializedObject(asset[0]);
            var tags = so.FindProperty("tags");
            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
