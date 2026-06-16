namespace CidadeLimpa.Core
{
    /// <summary>
    /// Implementado por qualquer fonte que contribua para o índice global de
    /// satisfação (tipicamente um bairro). Mantém o <see cref="SatisfacaoManager"/>
    /// desacoplado das classes concretas de gameplay.
    /// </summary>
    public interface IFonteSatisfacao
    {
        /// <summary>Nível de limpeza local normalizado (0 = lotado de lixo, 1 = limpo).</summary>
        float Limpeza01 { get; }

        /// <summary>Peso da fonte no cálculo global (tipicamente a população do bairro).</summary>
        float PesoPopulacional { get; }
    }
}
