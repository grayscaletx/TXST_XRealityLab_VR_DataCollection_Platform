using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class PackagingLineForm
    {
        public PackagingLineForm()
        {
            PackagingLineFormPallets = new HashSet<PackagingLineFormPallet>();
        }

        public int PackagingLineFormId { get; set; }
        public int MaterialId { get; set; }
        public int CountryId { get; set; }
        public int NoPalletToCreated { get; set; }
        public int PalletsLot { get; set; }
        public DateTime FormCreationDate { get; set; }
        public string Notes { get; set; }
        public DateTime RegistrationDateTime { get; set; }
        public DateTime? CompletionDateTime { get; set; }
        public int Status { get; set; }

        public virtual Material1 Material { get; set; }
        public virtual ICollection<PackagingLineFormPallet> PackagingLineFormPallets { get; set; }
    }
}
