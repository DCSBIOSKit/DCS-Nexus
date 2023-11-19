using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DCS_Nexus.Communication;  // Fixed syntax

namespace DCS_Nexus
{
    public partial class App : Application
    {
        public App() 
        {
            CommunicationType dcsType = Settings.Default.DCSCommunicationType;
            CommunicationType slaveType = Settings.Default.SlaveCommunicationType;

            CommunicationManager.Start(dcsType, slaveType);
        }
    }
}
