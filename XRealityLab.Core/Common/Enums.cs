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
