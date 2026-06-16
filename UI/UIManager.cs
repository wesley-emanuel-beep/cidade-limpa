using UnityEngine;
using CidadeLimpa.Core;

namespace CidadeLimpa.UI
{
    /// <summary>
    /// Ponto único de construção e coordenação da UI. Colocado no Canvas pelo
    /// Scene Builder, cria os GameObjects-filho de cada painel e adiciona seus
    /// componentes autoconstruídos, mantendo as referências cruzadas centralizadas.
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        public HUDManager Hud { get; private set; }
        public FrotaHUD Frota { get; private set; }
        public EventoBannerUI Banner { get; private set; }
        public LojaUI Loja { get; private set; }
        public FeedbackFlutuante Feedback { get; private set; }
        public PausaUI Pausa { get; private set; }

        protected override void AoInicializar()
        {
            // Ordem importa para a profundidade de renderização (z-order da UI).
            Hud = Criar<HUDManager>("HUD");
            Frota = Criar<FrotaHUD>("FrotaHUD");
            Banner = Criar<EventoBannerUI>("BannerEvento");
            Feedback = Criar<FeedbackFlutuante>("FeedbackFlutuante");
            Loja = Criar<LojaUI>("Loja");
            Pausa = Criar<PausaUI>("Pausa"); // por último → renderiza acima de tudo
        }

        private T Criar<T>(string nome) where T : Component
        {
            var go = new GameObject(nome, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            UIFactory.Esticar(rt);
            return go.AddComponent<T>();
        }
    }
}
