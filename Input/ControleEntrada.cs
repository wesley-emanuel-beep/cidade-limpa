using UnityEngine;
using CidadeLimpa.CameraSystem;
using CidadeLimpa.Caminhoes;

namespace CidadeLimpa.GameInput
{
    /// <summary>
    /// Arbitra os gestos do ponteiro entre "toque/clique" (selecionar caminhão ou
    /// definir destino) e "arraste" (mover a câmera), além de aplicar o zoom.
    /// Mantém câmera e gameplay desacoplados: lê o <see cref="InputManager"/> e
    /// chama <see cref="CameraController"/> e <see cref="FrotaManager"/>.
    /// </summary>
    public class ControleEntrada : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private CameraController cameraCtrl;

        [Header("Arbitragem")]
        [Tooltip("Distância em pixels acima da qual o gesto vira arraste de câmera.")]
        [SerializeField] private float limiarArraste = 14f;

        private bool _arrastando;
        private Vector2 _posInicial;
        private bool _gestoSobreUI;

        private void Awake()
        {
            if (cameraCtrl == null) cameraCtrl = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;
        }

        private void Update()
        {
            if (!InputManager.Existe || cameraCtrl == null) return;
            var input = InputManager.Instancia;

            // Zoom é sempre processado (scroll/pinça), independente de arraste.
            if (Mathf.Abs(input.ZoomDelta) > 0.0001f && !input.SobreUI())
                cameraCtrl.Zoom(input.ZoomDelta);

            if (input.PressionouAgora)
            {
                _posInicial = input.PosicaoTela;
                _arrastando = false;
                _gestoSobreUI = input.SobreUI();
            }

            if (_gestoSobreUI) return; // gesto começou sobre a UI: ignora o mundo

            if (input.Pressionado)
            {
                if (!_arrastando)
                {
                    float dist = Vector2.Distance(input.PosicaoTela, _posInicial);
                    if (dist > limiarArraste) _arrastando = true;
                }

                if (_arrastando && !input.Pincando)
                    cameraCtrl.Arrastar(input.DeltaTela);
            }

            if (input.SoltouAgora)
            {
                if (!_arrastando)
                {
                    // Foi um toque/clique: encaminha ao gameplay.
                    Vector3 mundo = cameraCtrl.TelaParaMundo(input.PosicaoTela);
                    if (FrotaManager.Existe)
                        FrotaManager.Instancia.ProcessarClique(mundo);
                }
                _arrastando = false;
            }
        }
    }
}
