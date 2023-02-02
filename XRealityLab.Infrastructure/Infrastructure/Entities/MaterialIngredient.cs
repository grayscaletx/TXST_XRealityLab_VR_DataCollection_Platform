using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class MaterialIngredient
    {
        public int MaterialIngredientId { get; set; }
        public int MaterialId { get; set; }
        public int IngredientId { get; set; }
        public int CaseSize { get; set; }
        public int CaseQty { get; set; }
        public int PalletAmount { get; set; }
        public int PalletFactor { get; set; }

        public virtual Ingredient Ingredient { get; set; }
        public virtual Material Material { get; set; }
    }
}
