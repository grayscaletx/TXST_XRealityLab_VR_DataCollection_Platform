using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ARI.Common
{
    public enum RoleType
    {
        Administrator = 1,
        ChiefOperator = 2,
        LogisticsSupervisor = 3,
        PackagingManager = 4,
        ProductionManager = 5,
        QualityAssuranceDepartment = 6,
        EquipmentManager = 7,
        Security = 8,
        IT = 9,
        NoRole = 10
    }

    public enum CMRType
    {
        Nedelea = 1,
        Chiajna = 2,
        Lernut = 3,
        Roman = 4
    }

    public enum OrderStatus
    {
        Created = 0,
        Picking,
        Completed,
        Cancel
    }

    public enum PackagingLineStatus
    {
        [Description("Άκυρο")]
        Cancel = -1,
        [Description("Δημιουργήθηκε")]
        Created = 0,
        [Description("Επικαιροποιήθηκε")]
        Completed,
    }

    public enum PalletStatus
    {
        [Description("Αναμονή για φόρτωση χαρτοκιβωτίων")]
        PalletBoxLoading = 0,
        [Description("Δημιουργήθηκε")]
        Created,
        [Description("Προστέθηκε στην αποθήκη")]
        AddedToWarehouse,
        [Description("Επιλέχθηκε για φόρτωση")]
        SelectedForLoad,
        [Description("Επιβεβαίωθηκε για φόρτωση")]
        ConfirmedForLoad,
        [Description("Φορτώθηκε")]
        Loaded,
        [Description("Άκυρο")]
        Cancel = -1,
    }

    public enum WarehouseArea
    {
        [Description("ΣΑΛΑ 1")]
        Sala1 = 1,
        [Description("ΣΑΛΑ 1 ΨΥΓΕΙΟ")]
        Sala1Psigeio,
        [Description("ΠΡΟΕΚΤΑΣΗ")]
        Proektasi,
        [Description("ΣΑΛΑ 2 ΔΕΞΙΑ ΠΤΕΡΥΓΑ")]
        Sala2DeksiaPteriga,
        [Description("ΣΑΛΑ 2 ΑΡΙΣΤΕΡΗ ΠΤΕΡΥΓΑ")]
        Sala2AristeriPteriga,
        [Description("ΣΑΛΑ 2 ΔΩΜΑΤΙΟ")]
        Sala2Domatio,
        [Description("ΜΑΚΙΟ")]
        Makio,
        [Description("ΕΙΚΟΝΙΚΗ ΑΠΟΘΗΚΗ")]
        Eikoniki
    }
}
