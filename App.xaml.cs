using System.Collections.Generic;
using System.Windows;
using DCS_Nexus.Communication;

namespace DCS_Nexus
{
    public partial class App : Application
    {
        public App()
        {
            CommunicationType dcsType = Settings.Default.DCSCommunicationType;
            List<CommunicationType> slaveTypes = Settings.Default.SlaveCommunicationTypes;

            CommunicationManager.Start(dcsType, slaveTypes.ToArray());
        }
    }
}
