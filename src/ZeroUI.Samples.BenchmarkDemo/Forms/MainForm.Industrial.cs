using System;
using System.Drawing;
using System.Windows.Forms;
using ZeroUI.WinForms.Base;
using ZeroUI.WinForms.Bms;
using ZeroUI.WinForms.Energy;
using ZeroUI.WinForms.LifeSciences;
using ZeroUI.WinForms.Logistics;
using ZeroUI.WinForms.Overlays;
using ZeroUI.WinForms.Petrochem;
using ZeroUI.WinForms.Process;
using ZeroUI.WinForms.Theme;
using ZeroUI.WinForms.Water;

namespace ZeroUI.Samples.BenchmarkDemo.Forms
{
    public sealed partial class MainForm
    {
        private ZeroTabControl _subTabsIndustrial = null!;

        private void InitializeIndustrialVerticals(ZeroTabPage clusterTab)
        {
            _subTabsIndustrial = new ZeroTabControl
            {
                Dock = DockStyle.Fill,
                Orientation = ZeroTabOrientation.Horizontal,
                TabHeight = 36,
                TabStyle = ZeroTabStyle.Pill
            };

            // 1. Energy & Smart Grid
            var tabEnergy = new ZeroTabPage("Energy & Smart Grid", "⚡");
            var pnlEnergy = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            var splitEnergy = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 640
            };
            var sld = new SingleLineDiagram { Dock = DockStyle.Fill };
            splitEnergy.Panel1.Controls.Add(sld);

            var tabsEnergyRight = new ZeroTabControl
            {
                Dock = DockStyle.Fill,
                Orientation = ZeroTabOrientation.Horizontal,
                TabHeight = 30,
                TabStyle = ZeroTabStyle.Underline
            };
            var tabSwitchgear = new ZeroTabPage("Switchgear Faceplate", "⚡");
            tabSwitchgear.Controls.Add(new SwitchgearFaceplate { Dock = DockStyle.Fill });
            var tabBess = new ZeroTabPage("BESS 16-Cell Rack", "🔋");
            tabBess.Controls.Add(new BessRackMonitor { Dock = DockStyle.Fill });
            var tabSolar = new ZeroTabPage("Solar PV 16-String", "☀️");
            tabSolar.Controls.Add(new SolarPvMatrix { Dock = DockStyle.Fill });

            tabsEnergyRight.AddTab(tabSwitchgear);
            tabsEnergyRight.AddTab(tabBess);
            tabsEnergyRight.AddTab(tabSolar);
            splitEnergy.Panel2.Controls.Add(tabsEnergyRight);
            pnlEnergy.Controls.Add(splitEnergy);
            tabEnergy.Controls.Add(pnlEnergy);

            // 2. Oil & Gas / Petrochemical
            var tabPetro = new ZeroTabPage("Oil & Gas / Petrochem", "🛢️");
            var pnlPetro = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            var splitPetro = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 620
            };
            splitPetro.Panel1.Controls.Add(new DistillationColumn { Dock = DockStyle.Fill });

            var tabsPetroRight = new ZeroTabControl
            {
                Dock = DockStyle.Fill,
                Orientation = ZeroTabOrientation.Horizontal,
                TabHeight = 30,
                TabStyle = ZeroTabStyle.Underline
            };
            var tabEsd = new ZeroTabPage("SIS Cause & Effect Matrix", "🚨");
            tabEsd.Controls.Add(new EsdMatrix { Dock = DockStyle.Fill });
            var tabPig = new ZeroTabPage("Pipeline PIG Tracking", "🔍");
            tabPig.Controls.Add(new PipelinePigMonitor { Dock = DockStyle.Fill });

            tabsPetroRight.AddTab(tabEsd);
            tabsPetroRight.AddTab(tabPig);
            splitPetro.Panel2.Controls.Add(tabsPetroRight);
            pnlPetro.Controls.Add(splitPetro);
            tabPetro.Controls.Add(pnlPetro);

            // 3. Pharma & Biotech ISA-88
            var tabPharma = new ZeroTabPage("Pharma & Biotech (ISA-88)", "💊");
            var pnlPharma = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            var splitPharma = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 580
            };
            var tabsPharmaLeft = new ZeroTabControl
            {
                Dock = DockStyle.Fill,
                Orientation = ZeroTabOrientation.Horizontal,
                TabHeight = 30,
                TabStyle = ZeroTabStyle.Underline
            };
            var tabBioreactor = new ZeroTabPage("Sanitary Bioreactor Vessel", "🔬");
            tabBioreactor.Controls.Add(new BioreactorVessel { Dock = DockStyle.Fill });
            var tabCleanroom = new ZeroTabPage("ISO Cleanroom Cascade", "🛡️");
            tabCleanroom.Controls.Add(new CleanroomEnvHud { Dock = DockStyle.Fill });
            tabsPharmaLeft.AddTab(tabBioreactor);
            tabsPharmaLeft.AddTab(tabCleanroom);
            splitPharma.Panel1.Controls.Add(tabsPharmaLeft);

            var tabsPharmaRight = new ZeroTabControl
            {
                Dock = DockStyle.Fill,
                Orientation = ZeroTabOrientation.Horizontal,
                TabHeight = 30,
                TabStyle = ZeroTabStyle.Underline
            };
            var tabSfc = new ZeroTabPage("ISA-88 Batch SFC Tracker", "📋");
            tabSfc.Controls.Add(new SfcBatchTracker { Dock = DockStyle.Fill });
            var tabCip = new ZeroTabPage("CIP/SIP 4 TACT Matrix", "🧼");
            tabCip.Controls.Add(new CipValidationMatrix { Dock = DockStyle.Fill });
            tabsPharmaRight.AddTab(tabSfc);
            tabsPharmaRight.AddTab(tabCip);
            splitPharma.Panel2.Controls.Add(tabsPharmaRight);

            pnlPharma.Controls.Add(splitPharma);
            tabPharma.Controls.Add(pnlPharma);

            // 4. Water & Wastewater Treatment
            var tabWater = new ZeroTabPage("Water & Wastewater", "💧");
            var pnlWater = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            var splitWater = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 340
            };
            var splitWaterTop = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 560
            };
            splitWaterTop.Panel1.Controls.Add(new ClarifierBasin { Dock = DockStyle.Fill });
            splitWaterTop.Panel2.Controls.Add(new ChemicalDosingSkid { Dock = DockStyle.Fill });
            splitWater.Panel1.Controls.Add(splitWaterTop);
            splitWater.Panel2.Controls.Add(new HydraulicGradientChart { Dock = DockStyle.Fill });

            pnlWater.Controls.Add(splitWater);
            tabWater.Controls.Add(pnlWater);

            // 5. BMS & HVAC
            var tabBms = new ZeroTabPage("BMS & HVAC Automation", "🏢");
            var pnlBms = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            var splitBms = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 580
            };
            splitBms.Panel1.Controls.Add(new AhuSchematic { Dock = DockStyle.Fill });

            var tabsBmsRight = new ZeroTabControl
            {
                Dock = DockStyle.Fill,
                Orientation = ZeroTabOrientation.Horizontal,
                TabHeight = 30,
                TabStyle = ZeroTabStyle.Underline
            };
            var tabChiller = new ZeroTabPage("Chiller Plant COP", "❄️");
            tabChiller.Controls.Add(new ChillerPlant { Dock = DockStyle.Fill });
            var tabZone = new ZeroTabPage("7-Day Zone Scheduler", "📅");
            tabZone.Controls.Add(new ZoneScheduler { Dock = DockStyle.Fill });
            tabsBmsRight.AddTab(tabChiller);
            tabsBmsRight.AddTab(tabZone);
            splitBms.Panel2.Controls.Add(tabsBmsRight);

            pnlBms.Controls.Add(splitBms);
            tabBms.Controls.Add(pnlBms);

            // 6. Life Sciences & Lab Automation
            var tabLife = new ZeroTabPage("Life Sciences & Lab", "🔬");
            var pnlLife = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            var splitLife = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 560
            };
            splitLife.Panel1.Controls.Add(new MicroplateReader { Dock = DockStyle.Fill });

            var tabsLifeRight = new ZeroTabControl
            {
                Dock = DockStyle.Fill,
                Orientation = ZeroTabOrientation.Horizontal,
                TabHeight = 30,
                TabStyle = ZeroTabStyle.Underline
            };
            var tabCentrifuge = new ZeroTabPage("Centrifuge RCF Monitor", "🌀");
            tabCentrifuge.Controls.Add(new CentrifugeMonitor { Dock = DockStyle.Fill });
            var tabCold = new ZeroTabPage("-80°C Cold Chain MKT", "🧊");
            tabCold.Controls.Add(new ColdChainTracker { Dock = DockStyle.Fill });
            tabsLifeRight.AddTab(tabCentrifuge);
            tabsLifeRight.AddTab(tabCold);
            splitLife.Panel2.Controls.Add(tabsLifeRight);

            pnlLife.Controls.Add(splitLife);
            tabLife.Controls.Add(pnlLife);

            // 7. Intralogistics & Robotics
            var tabLogistics = new ZeroTabPage("Robotics & Logistics", "🤖");
            var pnlLogistics = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            var splitLogistics = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 560
            };
            splitLogistics.Panel1.Controls.Add(new AgvFleetCanvas { Dock = DockStyle.Fill });

            var tabsLogisticsRight = new ZeroTabControl
            {
                Dock = DockStyle.Fill,
                Orientation = ZeroTabOrientation.Horizontal,
                TabHeight = 30,
                TabStyle = ZeroTabStyle.Underline
            };
            var tabAsrs = new ZeroTabPage("ASRS Stacker Crane", "🏗️");
            tabAsrs.Controls.Add(new AsrsCraneVisualizer { Dock = DockStyle.Fill });
            var tabConveyor = new ZeroTabPage("Conveyor High-Speed Sorter", "📦");
            tabConveyor.Controls.Add(new ConveyorMergeMatrix { Dock = DockStyle.Fill });
            tabsLogisticsRight.AddTab(tabAsrs);
            tabsLogisticsRight.AddTab(tabConveyor);
            splitLogistics.Panel2.Controls.Add(tabsLogisticsRight);

            pnlLogistics.Controls.Add(splitLogistics);
            tabLogistics.Controls.Add(pnlLogistics);

            // Add all 7 vertical sub-tabs
            _subTabsIndustrial.AddTab(tabEnergy);
            _subTabsIndustrial.AddTab(tabPetro);
            _subTabsIndustrial.AddTab(tabPharma);
            _subTabsIndustrial.AddTab(tabWater);
            _subTabsIndustrial.AddTab(tabBms);
            _subTabsIndustrial.AddTab(tabLife);
            _subTabsIndustrial.AddTab(tabLogistics);

            clusterTab.Controls.Add(_subTabsIndustrial);
        }
    }
}
