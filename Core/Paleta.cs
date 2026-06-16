using UnityEngine;

namespace CidadeLimpa.Core
{
    /// <summary>
    /// Paleta de cores oficial do projeto (GDD seção 12 — Estilo Visual &amp; Arte).
    /// Centralizada para garantir consistência entre código gerado, UI e Scene Builder.
    /// </summary>
    public static class Paleta
    {
        // ---- Paleta Bauhaus / Suíça (primárias + preto + off-white) ----

        /// <summary>#F4F1EA — off-white de fundo, "papel".</summary>
        public static readonly Color Papel = Hex("F4F1EA");
        /// <summary>#111111 — preto de texto/contornos, "tinta".</summary>
        public static readonly Color Tinta = Hex("111111");
        /// <summary>#1E5AFF — azul primário.</summary>
        public static readonly Color Azul = Hex("1E5AFF");
        /// <summary>#E5342B — vermelho primário.</summary>
        public static readonly Color Vermelho = Hex("E5342B");
        /// <summary>#FFC400 — amarelo primário.</summary>
        public static readonly Color Amarelo = Hex("FFC400");
        /// <summary>#C9C5BC — cinza neutro.</summary>
        public static readonly Color Cinza = Hex("C9C5BC");
        /// <summary>#13B872 — verde (positivo/dinheiro).</summary>
        public static readonly Color Eco = Hex("13B872");

        // Aliases de compatibilidade com o código existente.
        /// <summary>Alias de <see cref="Azul"/> (destaque/ação).</summary>
        public static readonly Color Eletrico = Hex("1E5AFF");
        /// <summary>Alias de <see cref="Vermelho"/> (perigo/alerta).</summary>
        public static readonly Color Alerta = Hex("E5342B");

        /// <summary>
        /// Converte um código hexadecimal ("RRGGBB" ou "#RRGGBB") em <see cref="Color"/>.
        /// Retorna magenta em caso de erro para tornar problemas visíveis.
        /// </summary>
        public static Color Hex(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return Color.magenta;

            if (ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var cor))
                return cor;

            Debug.LogWarning($"[Paleta] Hex inválido: '{hex}'. Usando magenta.");
            return Color.magenta;
        }

        /// <summary>
        /// Cor representativa do estado da cidade, usada para tingir o ambiente
        /// (limpo → eco, crise → alerta), conforme "Estados Visuais da Cidade".
        /// </summary>
        public static Color CorDoEstado(EstadoCidade estado)
        {
            switch (estado)
            {
                case EstadoCidade.Satisfeita: return Eco;
                case EstadoCidade.Preocupada: return Color.Lerp(Eco, Cinza, 0.5f);
                case EstadoCidade.EmCrise: return Cinza;
                case EstadoCidade.Revoltada: return Color.Lerp(Cinza, Alerta, 0.5f);
                case EstadoCidade.Abandonada: return Alerta;
                default: return Cinza;
            }
        }

        /// <summary>Cor associada a cada categoria de material reciclável.</summary>
        public static Color CorReciclavel(CategoriaReciclavel categoria)
        {
            switch (categoria)
            {
                case CategoriaReciclavel.Papel: return Eletrico;     // azul
                case CategoriaReciclavel.Plastico: return Alerta;    // vermelho
                case CategoriaReciclavel.Vidro: return Eco;          // verde
                case CategoriaReciclavel.Metal: return Hex("FFC400");// amarelo
                default: return Cinza;
            }
        }
    }
}
