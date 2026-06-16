using System;
using UnityEngine;

namespace CidadeLimpa.Core
{
    /// <summary>
    /// Barramento de eventos global do jogo. Centraliza a comunicação entre
    /// sistemas (managers, UI, gameplay) sem acoplamento direto — cada sistema
    /// publica/assina eventos em vez de referenciar concretamente os outros.
    ///
    /// Padrão: produtores chamam os métodos <c>Disparar*</c>; consumidores
    /// assinam os <c>event</c> correspondentes em OnEnable e cancelam em OnDisable.
    ///
    /// IMPORTANTE: por ser estático, os handlers persistem entre Play sessions no
    /// Editor. <see cref="ResetarEstaticos"/> limpa todas as assinaturas no início
    /// de cada Play graças a [RuntimeInitializeOnLoadMethod].
    /// </summary>
    public static class GameEvents
    {
        // ---------- Economia ----------
        /// <summary>Disparado quando o saldo de sucata muda. Param: novo saldo total.</summary>
        public static event Action<long> SucataAlterada;

        /// <summary>Disparado quando sucata é ganha (para feedback flutuante). Param: quantidade, posição mundial.</summary>
        public static event Action<long, Vector3> SucataGanha;

        // ---------- Satisfação / Cidade ----------
        /// <summary>Disparado quando o índice global de satisfação muda. Param: 0–100.</summary>
        public static event Action<float> SatisfacaoAlterada;

        /// <summary>Disparado quando o estado discreto da cidade muda. Param: novo estado.</summary>
        public static event Action<EstadoCidade> EstadoCidadeAlterado;

        // ---------- Lixo / Coleta ----------
        /// <summary>Disparado quando lixo é coletado por um caminhão. Param: tipo, quantidade.</summary>
        public static event Action<TipoLixo, int> LixoColetado;

        /// <summary>Disparado quando um caminhão termina de descarregar no depósito. Param: posição.</summary>
        public static event Action<Vector3> ColetaConcluida;

        // ---------- Caminhões / Frota ----------
        /// <summary>Disparado quando um caminhão é selecionado/desselecionado. Param: id, selecionado.</summary>
        public static event Action<int, bool> CaminhaoSelecionado;

        /// <summary>Disparado quando a frota muda (compra/venda). Param: novo total de caminhões.</summary>
        public static event Action<int> FrotaAlterada;

        /// <summary>Disparado quando o estado/status de um caminhão muda (atualiza painel). Param: id.</summary>
        public static event Action<int> CaminhaoAtualizado;

        // ---------- Eventos aleatórios ----------
        /// <summary>Evento aleatório iniciou. Param: id do evento (nome legível), duração em segundos.</summary>
        public static event Action<string, float> EventoIniciado;

        /// <summary>Evento aleatório terminou. Param: id do evento.</summary>
        public static event Action<string> EventoFinalizado;

        // ---------- Dificuldade ----------
        /// <summary>Nível de dificuldade aumentou. Param: novo nível (1–5).</summary>
        public static event Action<int> NivelDificuldadeAlterado;

        // ---------- Minigame ----------
        /// <summary>Solicita abertura do minigame de reciclagem. Param: bônus base potencial.</summary>
        public static event Action<long> MinigameSolicitado;

        /// <summary>Minigame concluído. Param: sucata bônus conquistada.</summary>
        public static event Action<long> MinigameConcluido;

        // ============================================================
        //  Métodos de disparo (encapsulam o null-check do invoke)
        // ============================================================

        public static void DispararSucataAlterada(long total) => SucataAlterada?.Invoke(total);
        public static void DispararSucataGanha(long qtd, Vector3 pos) => SucataGanha?.Invoke(qtd, pos);
        public static void DispararSatisfacaoAlterada(float valor) => SatisfacaoAlterada?.Invoke(valor);
        public static void DispararEstadoCidadeAlterado(EstadoCidade estado) => EstadoCidadeAlterado?.Invoke(estado);
        public static void DispararLixoColetado(TipoLixo tipo, int qtd) => LixoColetado?.Invoke(tipo, qtd);
        public static void DispararColetaConcluida(Vector3 pos) => ColetaConcluida?.Invoke(pos);
        public static void DispararCaminhaoSelecionado(int id, bool sel) => CaminhaoSelecionado?.Invoke(id, sel);
        public static void DispararFrotaAlterada(int total) => FrotaAlterada?.Invoke(total);
        public static void DispararCaminhaoAtualizado(int id) => CaminhaoAtualizado?.Invoke(id);
        public static void DispararEventoIniciado(string id, float dur) => EventoIniciado?.Invoke(id, dur);
        public static void DispararEventoFinalizado(string id) => EventoFinalizado?.Invoke(id);
        public static void DispararNivelDificuldadeAlterado(int nivel) => NivelDificuldadeAlterado?.Invoke(nivel);
        public static void DispararMinigameSolicitado(long bonusBase) => MinigameSolicitado?.Invoke(bonusBase);
        public static void DispararMinigameConcluido(long bonus) => MinigameConcluido?.Invoke(bonus);

        /// <summary>
        /// Limpa todas as assinaturas ao entrar em Play Mode. Necessário porque
        /// o domínio estático sobrevive entre sessões quando "Enter Play Mode
        /// Options (sem Domain Reload)" está ativo.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetarEstaticos()
        {
            SucataAlterada = null;
            SucataGanha = null;
            SatisfacaoAlterada = null;
            EstadoCidadeAlterado = null;
            LixoColetado = null;
            ColetaConcluida = null;
            CaminhaoSelecionado = null;
            FrotaAlterada = null;
            CaminhaoAtualizado = null;
            EventoIniciado = null;
            EventoFinalizado = null;
            NivelDificuldadeAlterado = null;
            MinigameSolicitado = null;
            MinigameConcluido = null;
        }
    }
}
