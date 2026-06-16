using UnityEngine;

namespace CidadeLimpa.Caminhoes
{
    /// <summary>
    /// Anima sutilmente o sprite do caminhão (bob + "respiração") para dar vida ao
    /// veículo sem conflitar com a orientação de direção (que gira o pai "Modelo").
    /// </summary>
    public class ModeloAnimador : MonoBehaviour
    {
        [SerializeField] private float amplitudeBob = 0.04f;
        [SerializeField] private float velocidadeBob = 7f;
        [SerializeField] private float amplitudeRespiro = 0.04f;
        [SerializeField] private float velocidadeRespiro = 3.5f;

        private Vector3 _posBase;
        private Vector3 _escalaBase;
        private float _fase;

        private void Awake()
        {
            _posBase = transform.localPosition;
            _escalaBase = transform.localScale;
            _fase = Random.value * 10f;
        }

        private void Update()
        {
            float t = Time.time + _fase;
            transform.localPosition = _posBase + Vector3.up * (Mathf.Sin(t * velocidadeBob) * amplitudeBob);
            float s = 1f + Mathf.Sin(t * velocidadeRespiro) * amplitudeRespiro;
            transform.localScale = _escalaBase * s;
        }
    }
}
