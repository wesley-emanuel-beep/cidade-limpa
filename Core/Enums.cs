namespace CidadeLimpa.Core
{
    /// <summary>
    /// Tipos de resíduo gerados nos bairros, conforme seção 04 do GDD.
    /// </summary>
    public enum TipoLixo
    {
        /// <summary>Gerado continuamente. Baixo valor de sucata.</summary>
        Comum,
        /// <summary>Produz mais sucata bônus. Alto valor.</summary>
        Reciclavel,
        /// <summary>Acúmulo ignorado por muito tempo. Crítico para a satisfação.</summary>
        GrandeAcumulo
    }

    /// <summary>
    /// Categorias de material do minigame de reciclagem (seção 09).
    /// </summary>
    public enum CategoriaReciclavel
    {
        Papel,
        Plastico,
        Vidro,
        Metal
    }

    /// <summary>
    /// Perfil socioeconômico do bairro — afeta taxa de geração de lixo (seção 03).
    /// </summary>
    public enum TipoBairro
    {
        Comercial,
        Industrial,
        Residencial,
        Misto
    }

    /// <summary>
    /// Estados da cidade derivados do índice global de satisfação (seção 07).
    /// </summary>
    public enum EstadoCidade
    {
        /// <summary>100% — máxima eficiência e recompensas.</summary>
        Satisfeita,
        /// <summary>70% — algumas reclamações visíveis.</summary>
        Preocupada,
        /// <summary>50% — menos sucata, mais lixo.</summary>
        EmCrise,
        /// <summary>20% — crescimento travado.</summary>
        Revoltada,
        /// <summary>0% — modo de crise máxima.</summary>
        Abandonada
    }

    /// <summary>
    /// Máquina de estados de alto nível de um caminhão da frota (seção 05 / 11).
    /// </summary>
    public enum EstadoCaminhao
    {
        /// <summary>Parado no depósito, disponível para receber ordens.</summary>
        Ocioso,
        /// <summary>Deslocando-se até o bairro de destino.</summary>
        EmRota,
        /// <summary>Coletando resíduos no bairro.</summary>
        Coletando,
        /// <summary>Retornando ao depósito (cheio ou ordem concluída).</summary>
        Retornando,
        /// <summary>Descarregando no depósito e convertendo lixo em sucata.</summary>
        Descarregando
    }

    /// <summary>
    /// Categorias da loja (seção 10).
    /// </summary>
    public enum CategoriaLoja
    {
        Frota,
        Melhorias,
        Tecnologia,
        Visual
    }

    /// <summary>
    /// Tipo de efeito que um evento aleatório aplica ao jogo (seção 08).
    /// </summary>
    public enum TipoEfeitoEvento
    {
        /// <summary>Multiplica a taxa de geração de lixo (Festival, Chuva).</summary>
        MultiplicadorLixo,
        /// <summary>Multiplica a sucata ganha (Campanha Ambiental).</summary>
        MultiplicadorSucata,
        /// <summary>Aplica um delta imediato de satisfação (Mutirão Escolar).</summary>
        SatisfacaoImediata
    }
}
