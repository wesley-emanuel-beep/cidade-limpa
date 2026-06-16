using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using CidadeLimpa.Core;

namespace CidadeLimpa.GameInput
{
    /// <summary>
    /// Abstrai o ponteiro unificado (mouse no PC, toque no mobile) sobre o Input
    /// System, expondo um estado simples que os demais sistemas consomem sem se
    /// preocupar com a plataforma. Também detecta pinça de dois dedos para zoom.
    /// </summary>
    public class InputManager : Singleton<InputManager>
    {
        /// <summary>Ponteiro pressionado neste frame (clique/toque iniciou).</summary>
        public bool PressionouAgora { get; private set; }

        /// <summary>Ponteiro liberado neste frame.</summary>
        public bool SoltouAgora { get; private set; }

        /// <summary>Ponteiro mantido pressionado.</summary>
        public bool Pressionado { get; private set; }

        /// <summary>Posição do ponteiro em pixels de tela.</summary>
        public Vector2 PosicaoTela { get; private set; }

        /// <summary>Deslocamento do ponteiro desde o frame anterior (pixels).</summary>
        public Vector2 DeltaTela { get; private set; }

        /// <summary>Delta de zoom combinado (scroll do mouse + pinça). Positivo = aproximar.</summary>
        public float ZoomDelta { get; private set; }

        /// <summary>True enquanto dois dedos estão na tela (gesto de pinça).</summary>
        public bool Pincando { get; private set; }

        private Vector2 _posAnterior;
        private bool _pressionadoAnterior;
        private float _distPinchAnterior;

        private void Update()
        {
            LerPonteiro();
            LerZoom();
        }

        private void LerPonteiro()
        {
            bool pressionado = false;
            Vector2 pos = _posAnterior;

            var touch = Touchscreen.current;
            if (touch != null && touch.touches.Count > 0)
            {
                var t0 = touch.touches[0];
                if (t0.press.isPressed)
                {
                    pressionado = true;
                    pos = t0.position.ReadValue();
                }
            }

            // Mouse (PC) — só considera se não houver toque ativo.
            if (!pressionado && Mouse.current != null)
            {
                pos = Mouse.current.position.ReadValue();
                pressionado = Mouse.current.leftButton.isPressed;
            }

            PosicaoTela = pos;
            DeltaTela = _pressionadoAnterior && pressionado ? pos - _posAnterior : Vector2.zero;
            PressionouAgora = pressionado && !_pressionadoAnterior;
            SoltouAgora = !pressionado && _pressionadoAnterior;
            Pressionado = pressionado;

            _posAnterior = pos;
            _pressionadoAnterior = pressionado;
        }

        private void LerZoom()
        {
            float zoom = 0f;

            // Pinça (dois dedos) no mobile.
            var touch = Touchscreen.current;
            if (touch != null && touch.touches.Count >= 2 &&
                touch.touches[0].press.isPressed && touch.touches[1].press.isPressed)
            {
                Vector2 a = touch.touches[0].position.ReadValue();
                Vector2 b = touch.touches[1].position.ReadValue();
                float dist = Vector2.Distance(a, b);

                if (Pincando)
                    zoom += (dist - _distPinchAnterior) * 0.01f;

                _distPinchAnterior = dist;
                Pincando = true;
            }
            else
            {
                Pincando = false;
            }

            // Scroll do mouse no PC.
            if (Mouse.current != null)
                zoom += Mouse.current.scroll.ReadValue().y * 0.01f;

            ZoomDelta = zoom;
        }

        /// <summary>True se o ponteiro está sobre um elemento de UI (deve ignorar o mundo).</summary>
        public bool SobreUI()
        {
            if (EventSystem.current == null) return false;

            // Mobile: verifica o dedo; PC: verifica o ponteiro do mouse.
            var touch = Touchscreen.current;
            if (touch != null && touch.touches.Count > 0 && touch.touches[0].press.isPressed)
                return EventSystem.current.IsPointerOverGameObject(touch.touches[0].touchId.ReadValue());

            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
