using UnityEngine;

namespace CidadeLimpa.Core
{
    /// <summary>
    /// Gerencia áudio do jogo (GDD seção "SOM" do cronograma): música ambiente em
    /// loop e SFX one-shot reagindo a eventos do <see cref="GameEvents"/>.
    /// Tolerante a clips ausentes (no-op), permitindo rodar sem assets de áudio.
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Fontes")]
        [SerializeField] private AudioSource fonteMusica;
        [SerializeField] private AudioSource fonteSfx;

        [Header("Clips")]
        [SerializeField] private AudioClip musicaAmbiente;
        [SerializeField] private AudioClip sfxColeta;
        [SerializeField] private AudioClip sfxSucata;
        [SerializeField] private AudioClip sfxCompra;
        [SerializeField] private AudioClip sfxEvento;
        [SerializeField] private AudioClip sfxAcerto;
        [SerializeField] private AudioClip sfxErro;

        [Header("Volumes")]
        [SerializeField, Range(0f, 1f)] private float volumeMusica = 0.5f;
        [SerializeField, Range(0f, 1f)] private float volumeSfx = 0.8f;

        protected override void AoInicializar()
        {
            GarantirFontes();
            CarregarClipsPadrao();
        }

        /// <summary>Carrega clips de Resources/Audio quando não atribuídos no Inspector.</summary>
        private void CarregarClipsPadrao()
        {
            if (musicaAmbiente == null) musicaAmbiente = Carregar("ambiente");
            if (sfxColeta == null) sfxColeta = Carregar("coleta");
            if (sfxSucata == null) sfxSucata = Carregar("sucata");
            if (sfxCompra == null) sfxCompra = Carregar("compra");
            if (sfxEvento == null) sfxEvento = Carregar("evento");
            if (sfxAcerto == null) sfxAcerto = Carregar("acerto");
            if (sfxErro == null) sfxErro = Carregar("erro");
        }

        private static AudioClip Carregar(string nome) => Resources.Load<AudioClip>("Audio/" + nome);

        private void OnEnable()
        {
            GameEvents.ColetaConcluida += AoColetaConcluida;
            GameEvents.SucataGanha += AoSucataGanha;
            GameEvents.EventoIniciado += AoEventoIniciado;
            GameEvents.FrotaAlterada += AoFrotaAlterada;
            GameEvents.MinigameConcluido += AoMinigameConcluido;
        }

        private void OnDisable()
        {
            GameEvents.ColetaConcluida -= AoColetaConcluida;
            GameEvents.SucataGanha -= AoSucataGanha;
            GameEvents.EventoIniciado -= AoEventoIniciado;
            GameEvents.FrotaAlterada -= AoFrotaAlterada;
            GameEvents.MinigameConcluido -= AoMinigameConcluido;
        }

        private void Start()
        {
            if (musicaAmbiente != null && fonteMusica != null)
            {
                fonteMusica.clip = musicaAmbiente;
                fonteMusica.loop = true;
                fonteMusica.volume = volumeMusica;
                fonteMusica.Play();
            }
        }

        private void GarantirFontes()
        {
            if (fonteMusica == null)
            {
                fonteMusica = gameObject.AddComponent<AudioSource>();
                fonteMusica.playOnAwake = false;
            }
            if (fonteSfx == null)
            {
                fonteSfx = gameObject.AddComponent<AudioSource>();
                fonteSfx.playOnAwake = false;
            }
        }

        /// <summary>Toca um SFX one-shot, respeitando o volume configurado.</summary>
        public void TocarSfx(AudioClip clip)
        {
            if (clip == null || fonteSfx == null) return;
            fonteSfx.PlayOneShot(clip, volumeSfx);
        }

        private void AoColetaConcluida(Vector3 _) => TocarSfx(sfxColeta);
        private void AoSucataGanha(long _, Vector3 __) => TocarSfx(sfxSucata);
        private void AoEventoIniciado(string _, float __) => TocarSfx(sfxEvento);
        private void AoFrotaAlterada(int _) => TocarSfx(sfxCompra);
        private void AoMinigameConcluido(long bonus) => TocarSfx(bonus > 0 ? sfxAcerto : sfxErro);
    }
}
