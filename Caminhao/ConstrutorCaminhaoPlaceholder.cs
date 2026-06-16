using UnityEngine;
using UnityEngine.Rendering;
using CidadeLimpa.Data;

namespace CidadeLimpa.Caminhoes
{
    /// <summary>
    /// Constrói um caminhão low poly procedural a partir de primitivas, para o
    /// jogo rodar sem um FBX importado (GDD: caminhão é o único elemento 3D).
    /// Estrutura: raiz (recebe o componente Caminhao) → "Modelo" (filho 0, girado
    /// pela lógica de orientação) → caixaria, cabine e rodas.
    /// </summary>
    public static class ConstrutorCaminhaoPlaceholder
    {
        private static Material _matCorpo;
        private static Material _matCabine;
        private static Material _matRoda;

        /// <summary>Cria a hierarquia do caminhão e retorna a raiz.</summary>
        public static GameObject Criar(CaminhaoData dados, Transform pai)
        {
            var raiz = new GameObject("Caminhao");
            raiz.transform.SetParent(pai, false);

            var modelo = new GameObject("Modelo");
            modelo.transform.SetParent(raiz.transform, false);

            Color cor = dados != null ? dados.cor : new Color(0.1f, 0.45f, 1f);
            GarantirMateriais(cor);

            // Caçamba (corpo) — comprimento ao longo do eixo Y (frente do veículo).
            var corpo = Cubo("Corpo", modelo.transform, _matCorpo);
            corpo.transform.localPosition = new Vector3(0f, -0.1f, 0.25f);
            corpo.transform.localScale = new Vector3(0.6f, 1.0f, 0.5f);

            // Cabine — à frente, mais alta.
            var cabine = Cubo("Cabine", modelo.transform, _matCabine);
            cabine.transform.localPosition = new Vector3(0f, 0.55f, 0.3f);
            cabine.transform.localScale = new Vector3(0.55f, 0.35f, 0.6f);

            // Rodas (4 cilindros achatados nas laterais).
            CriarRoda(modelo.transform, new Vector3(-0.32f, 0.35f, 0f));
            CriarRoda(modelo.transform, new Vector3(0.32f, 0.35f, 0f));
            CriarRoda(modelo.transform, new Vector3(-0.32f, -0.35f, 0f));
            CriarRoda(modelo.transform, new Vector3(0.32f, -0.35f, 0f));

            return raiz;
        }

        private static GameObject Cubo(string nome, Transform pai, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nome;
            go.transform.SetParent(pai, false);
            Object.Destroy(go.GetComponent<Collider>());
            AplicarMaterial(go, mat);
            return go;
        }

        private static void CriarRoda(Transform pai, Vector3 pos)
        {
            var roda = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            roda.name = "Roda";
            roda.transform.SetParent(pai, false);
            Object.Destroy(roda.GetComponent<Collider>());
            roda.transform.localPosition = pos;
            // Cilindro deitado no eixo X (rodas horizontais vistas de cima).
            roda.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            roda.transform.localScale = new Vector3(0.18f, 0.08f, 0.18f);
            AplicarMaterial(roda, _matRoda);
        }

        private static void AplicarMaterial(GameObject go, Material mat)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.On;
            }
        }

        private static void GarantirMateriais(Color corCorpo)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            _matCorpo = new Material(shader) { color = corCorpo };
            _matCabine = new Material(shader) { color = Color.Lerp(corCorpo, Color.white, 0.4f) };
            _matRoda = new Material(shader) { color = new Color(0.08f, 0.08f, 0.08f) };
        }
    }
}
