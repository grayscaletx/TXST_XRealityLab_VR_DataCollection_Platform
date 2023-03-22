using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace XRealityLab.Common
{
    public enum RoleType
    {
        [Description("Administrator")]
        Administrator = 1,
        [Description("DirectorIT")]
        DirectorOfIT,
        [Description("LabDirector")]
        LabDirector,
        [Description("PrincipleInvestigator")]
        PrincipleInvestigator,
        [Description("ResearchAssistant")]
        ResearchAssistant
    }
    public enum Rank
    {
        [Description("Ms.")]
        Ms = 1,
        [Description("Mr.")]
        Mr,
        [Description("Dr.")]
        Dr,
        [Description("Mx.")]
        Mx,
    }
    public enum Color
    {
        [Description("#5BB79E")]
        C1 = 1,
        [Description("#4F91CD")]
        C2,
        [Description("#9F78B9")]
        C3,
        [Description("#E1BD42")]
        C4,
        [Description("#D07F39")]
        C5,
        [Description("#CE5543")]
        C6,
        [Description("#76CB8E")]
        C7,
        [Description("#364658")]
        C8,
        [Description("#A5B0B1")]
        C9,
        [Description("#53A564")]
        C10
    }

    //TODO
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
}
