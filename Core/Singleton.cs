using UnityEngine;

namespace CidadeLimpa.Core
{
    /// <summary>
    /// Base genérica para managers de cena que precisam de acesso global.
    /// Não usa DontDestroyOnLoad: o jogo é single-scene e o Scene Builder
    /// recria todos os managers a cada montagem da cena.
    /// </summary>
    /// <typeparam name="T">O próprio tipo concreto do manager.</typeparam>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        private static T _instancia;

        /// <summary>Instância única do manager na cena (pode ser nula durante o bootstrap).</summary>
        public static T Instancia => _instancia;

        /// <summary>True se a instância já foi inicializada.</summary>
        public static bool Existe => _instancia != null;

        protected virtual void Awake()
        {
            if (_instancia != null && _instancia != this)
            {
                Debug.LogWarning($"[{typeof(T).Name}] Instância duplicada em '{name}'. Destruindo a nova.");
                Destroy(gameObject);
                return;
            }

            _instancia = (T)this;
            AoInicializar();
        }

        /// <summary>Hook de inicialização chamado após o singleton ser registrado.</summary>
        protected virtual void AoInicializar() { }

        protected virtual void OnDestroy()
        {
            if (_instancia == this)
                _instancia = null;
        }
    }
}
