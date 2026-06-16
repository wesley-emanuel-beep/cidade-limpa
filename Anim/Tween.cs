using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CidadeLimpa.Anim
{
    /// <summary>
    /// Motor de animação leve baseado em coroutines, sem dependências externas.
    /// Usado para o "game feel": pops, slides e fades de UI e gameplay. Os tweens
    /// de UI usam tempo não-escalado (funcionam mesmo com o jogo pausado).
    /// </summary>
    public static class Tween
    {
        /// <summary>Anima a escala local de um Transform.</summary>
        public static Coroutine Escala(Transform alvo, Vector3 destino, float duracao,
            TipoEase ease = TipoEase.OutQuad, bool naoEscalado = true, Action aoFim = null)
        {
            if (alvo == null) return null;
            return TweenRunner.Instancia.StartCoroutine(
                RotinaEscala(alvo, alvo.localScale, destino, duracao, ease, naoEscalado, aoFim));
        }

        /// <summary>Pop: cresce além do alvo e assenta (OutBack). Bom para feedback de clique/ganho.</summary>
        public static Coroutine Pop(Transform alvo, float escalaBase = 1f, float intensidade = 1.25f, float duracao = 0.28f)
        {
            if (alvo == null) return null;
            alvo.localScale = Vector3.one * (escalaBase * 0.85f);
            return Escala(alvo, Vector3.one * escalaBase, duracao, TipoEase.OutBack);
        }

        /// <summary>Anima a posição ancorada de um RectTransform.</summary>
        public static Coroutine MoverAncora(RectTransform alvo, Vector2 destino, float duracao,
            TipoEase ease = TipoEase.OutQuad, bool naoEscalado = true, Action aoFim = null)
        {
            if (alvo == null) return null;
            return TweenRunner.Instancia.StartCoroutine(
                RotinaMover(alvo, alvo.anchoredPosition, destino, duracao, ease, naoEscalado, aoFim));
        }

        /// <summary>Anima o alpha de um CanvasGroup.</summary>
        public static Coroutine Fade(CanvasGroup grupo, float destino, float duracao,
            bool naoEscalado = true, Action aoFim = null)
        {
            if (grupo == null) return null;
            return TweenRunner.Instancia.StartCoroutine(
                RotinaFade(grupo, grupo.alpha, destino, duracao, naoEscalado, aoFim));
        }

        // ----------------- rotinas -----------------

        private static IEnumerator RotinaEscala(Transform t, Vector3 de, Vector3 para, float dur,
            TipoEase ease, bool naoEscalado, Action aoFim)
        {
            float e = 0f;
            while (e < dur)
            {
                if (t == null) yield break;
                e += naoEscalado ? Time.unscaledDeltaTime : Time.deltaTime;
                t.localScale = Vector3.LerpUnclamped(de, para, Ease.Avaliar(ease, e / dur));
                yield return null;
            }
            if (t != null) t.localScale = para;
            aoFim?.Invoke();
        }

        private static IEnumerator RotinaMover(RectTransform t, Vector2 de, Vector2 para, float dur,
            TipoEase ease, bool naoEscalado, Action aoFim)
        {
            float e = 0f;
            while (e < dur)
            {
                if (t == null) yield break;
                e += naoEscalado ? Time.unscaledDeltaTime : Time.deltaTime;
                t.anchoredPosition = Vector2.LerpUnclamped(de, para, Ease.Avaliar(ease, e / dur));
                yield return null;
            }
            if (t != null) t.anchoredPosition = para;
            aoFim?.Invoke();
        }

        private static IEnumerator RotinaFade(CanvasGroup g, float de, float para, float dur,
            bool naoEscalado, Action aoFim)
        {
            float e = 0f;
            while (e < dur)
            {
                if (g == null) yield break;
                e += naoEscalado ? Time.unscaledDeltaTime : Time.deltaTime;
                g.alpha = Mathf.Lerp(de, para, e / dur);
                yield return null;
            }
            if (g != null) g.alpha = para;
            aoFim?.Invoke();
        }
    }

    /// <summary>Executor oculto de coroutines para os tweens estáticos.</summary>
    public class TweenRunner : MonoBehaviour
    {
        private static TweenRunner _instancia;

        public static TweenRunner Instancia
        {
            get
            {
                if (_instancia == null)
                {
                    var go = new GameObject("[TweenRunner]") { hideFlags = HideFlags.HideAndDontSave };
                    _instancia = go.AddComponent<TweenRunner>();
                    DontDestroyOnLoad(go);
                }
                return _instancia;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Resetar() => _instancia = null;
    }
}
