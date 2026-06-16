using UnityEngine;
using CidadeLimpa.Core;

namespace CidadeLimpa.Caminhoes
{
    /// <summary>
    /// Desenha dinamicamente a rota do caminhão (caminhão → pontos da fila →
    /// depósito) como uma linha vermelha. Some quando o caminhão está sem rota.
    /// Dá a sensação de "rotas pela cidade toda" conforme o jogador despacha.
    /// </summary>
    [RequireComponent(typeof(Caminhao))]
    public class RotaRenderer : MonoBehaviour
    {
        private static Material _mat;
        private Caminhao _caminhao;
        private LineRenderer _lr;
        private readonly System.Collections.Generic.List<Vector3> _pts = new System.Collections.Generic.List<Vector3>();

        private void Awake()
        {
            _caminhao = GetComponent<Caminhao>();

            var go = new GameObject("Rota");
            go.transform.SetParent(transform, false);
            _lr = go.AddComponent<LineRenderer>();
            _lr.useWorldSpace = true;
            _lr.widthMultiplier = 0.12f;
            _lr.numCapVertices = 3;
            _lr.numCornerVertices = 3;
            _lr.sortingOrder = -9;
            if (_mat == null) _mat = new Material(Shader.Find("Sprites/Default"));
            _lr.material = _mat;
            _lr.startColor = _lr.endColor = new Color(Paleta.Vermelho.r, Paleta.Vermelho.g, Paleta.Vermelho.b, 0.9f);
            _lr.positionCount = 0;
        }

        private void LateUpdate()
        {
            var rota = _caminhao.Rota;
            if (rota == null || rota.Count == 0) { if (_lr.positionCount != 0) _lr.positionCount = 0; return; }

            _pts.Clear();
            _pts.Add(ComZ(transform.position));
            for (int i = 0; i < rota.Count; i++)
                if (rota[i] != null) _pts.Add(ComZ(rota[i].Posicao));
            if (Deposito.Existe) _pts.Add(ComZ(Deposito.Instancia.Posicao));

            _lr.positionCount = _pts.Count;
            for (int i = 0; i < _pts.Count; i++) _lr.SetPosition(i, _pts[i]);
        }

        private static Vector3 ComZ(Vector3 p) { p.z = -0.2f; return p; }
    }
}
