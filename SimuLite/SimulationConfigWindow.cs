using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SimuLite
{
    public class SimulationConfigWindow : WindowBase
    {
        public SimulationConfigWindow() : base(8234, "Simulation Configuration")
        {
            config = new SimulationConfiguration() { OrbitalSimulation = (StaticInformation.Simulation?.OrbitalSimulation ?? false) };

            //these are set from the last simulation
            if (StaticInformation.Simulation != null)
            {
                PeString = (StaticInformation.Simulation.Periapsis / 1000).ToString();
                ApString = (StaticInformation.Simulation.Apoapsis / 1000).ToString();
                InclinationString = StaticInformation.Simulation.Inclination.ToString();
                startPointSelection = (StaticInformation.Simulation.MeanAnomalyAtEpoch > 0) ? 1 : 0;
            }

            SetPe(PeString);
            SetAp(ApString);
            SetInc(InclinationString);
        }

        internal SimulationConfiguration config;

        #region UI Properties
        /// <summary>
        /// Whether to show advanced options (time options and such)
        /// </summary>
        public bool ShowAdvanced { get; set; }

        private string _durationString = "15m";

        public string DurationString
        {
            get { return _durationString; }
            set
            {
                if (value != _durationString)
                {
                    _durationString = value;
                    config.SetDuration(_durationString); //update viewmodel
                }
            }
        }

        private string _peString = "75";

        public string PeString
        {
            get { return _peString; }
            set { _peString = value; }
        }

        private string _apString = "75";

        public string ApString
        {
            get { return _apString; }
            set { _apString = value; }
        }

        private string _inclinationString = "0";

        public string InclinationString
        {
            get { return _inclinationString; }
            set { _inclinationString = value; }
        }

        private int startPointSelection = 0;

        private string _coreHoursBuy = "0";
        public string CoreHoursBuy
        {
            get { return _coreHoursBuy; }
            set { _coreHoursBuy = value; }
        }

        public CelestialBody SelectedBody
        {
            get
            {
                return config?.SelectedBody;
            }
            set
            {
                config.SelectedBody = value;
            }
        }
        #endregion UI Properties


        #region String Verification
        private GUIStyle regularTextField = null;
        private GUIStyle redColorTextField = null;
        private bool apOk = true, peOk = true, incOk = true;
        #endregion

        public override void Draw(int windowID)
        {
            //planet
            //orbit?
            //  //altitude
            //  //inclination
            //else(landed)
            //  //latitude
            //  //longitude
            //if Home, launchsite

            //time
            //is relative time?

            //complexity

            //?expected duration?
            //?expected core hour usage?

            if (regularTextField == null || redColorTextField == null)
            {
                regularTextField = new GUIStyle(GUI.skin.textField);
                redColorTextField = new GUIStyle(GUI.skin.textField);
                redColorTextField.normal.textColor = Color.red;
                redColorTextField.focused.textColor = Color.red;
                redColorTextField.active.textColor = Color.red;
            }
            GUILayout.BeginVertical();

            GUILayout.Label("Selected Body:");
            GUILayout.Label(config.SelectedBody.name);

            if (config.SelectedBody == Planetarium.fetch.Home)
            {
                bool newValue = GUILayout.Toggle(config.OrbitalSimulation, "Orbital Simulation");
                if (newValue != config.OrbitalSimulation)
                {
                    MinimizeHeight();
                    config.OrbitalSimulation = newValue;
                }
            }
            else
            {
                config.OrbitalSimulation = true;
            }

            if (config.OrbitalSimulation)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Apoapsis (km):");
                apOk = SetAp(GUILayout.TextField(ApString, apOk ? regularTextField : redColorTextField, GUILayout.Width(100)));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Periapsis (km):");
                peOk = SetPe(GUILayout.TextField(PeString, peOk ? regularTextField : redColorTextField, GUILayout.Width(100)));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Inclination (deg):");
                incOk = SetInc(GUILayout.TextField(InclinationString, incOk ? regularTextField : redColorTextField, GUILayout.Width(100)));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Start at: ");
                startPointSelection = GUILayout.SelectionGrid(startPointSelection, new string[2] { "Periapsis", "Apoapsis" }, 2);
                GUILayout.EndHorizontal();
            }

            #region Core Hour Purchasing
            //List complexity
            GUILayout.BeginHorizontal();
            GUILayout.Label("Complexity: ");
            GUILayout.Label(config.Complexity.ToString("N2") + " core-hours/s");
            GUILayout.EndHorizontal();

            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Core-hours: ");
                GUILayout.Label(StaticInformation.RemainingCoreHours.ToString("N2") + " core-hours");
                GUILayout.EndHorizontal();

                GUILayout.Label("Max simulation time: ");
                double time = StaticInformation.RemainingCoreHours / config.Complexity;
                GUILayout.Label(MagiCore.Utilities.GetFormattedTime(time, true));

                GUILayout.Label("Purchase core-hours:");
                GUILayout.BeginHorizontal();
                CoreHoursBuy = GUILayout.TextField(CoreHoursBuy, GUILayout.Width(100));
                double coreHours = 0;
                if (double.TryParse(CoreHoursBuy, out coreHours))
                {
                    Dictionary<string, string> variables = new Dictionary<string, string>();
                    variables.Add("H", coreHours.ToString()); //this way you can buy them in bulk at cheaper rates maybe
                    //other variables? R&D level? Scientists/Engineers?
                    double cost = MagiCore.MathParsing.ParseMath(Configuration.CoreHourCost, variables);
                    if (GUILayout.Button("√"+Math.Ceiling(cost)))
                    {
                        //purchase the core hours!
                        if (Funding.Instance.Funds >= cost)
                        {
                            Funding.Instance.AddFunds(-cost, TransactionReasons.None);
                            StaticInformation.RemainingCoreHours += coreHours;
                        }
                    }
                }
                else
                {
                    GUILayout.Label("Invalid");
                }
                GUILayout.EndHorizontal();

            }
            #endregion Core Hour Purchasing

            bool startable = canStart();
            if (startable && GUILayout.Button("Simulate!"))
            {
                config.MeanAnomalyAtEpoch = Math.PI * startPointSelection; //0 if Pe, 180 if Ap
                config.StartSimulation();
            }
            else if (!startable)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Invalid Parameters");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            //update the ship
            if (EditorLogic.fetch?.ship != null)
            {
                config.Ship = EditorLogic.fetch.ship;
            }


            base.Draw(windowID);
        }

        #region Private Methods
        private bool canStart()
        {
            bool canStart = ((!config.OrbitalSimulation || (apOk && peOk && incOk)) && config?.Ship?.Count > 0);
            canStart &= config.Complexity >= 0 && (StaticInformation.RemainingCoreHours > config.Complexity);
            return canStart;
        }

        private bool SetPe(string value)
        {
            _peString = value;
            double pe;
            if (double.TryParse(value, out pe))
            {
                config.Periapsis = pe * 1000;
                return config.Periapsis == pe*1000;
            }
            return false;
        }

        private bool SetAp(string value)
        {
            _apString = value;
            double ap;
            if (double.TryParse(value, out ap))
            {
                config.Apoapsis = ap * 1000;
                return config.Apoapsis == ap*1000;
            }
            return false;
        }

        private bool SetInc(string value)
        {
            _inclinationString = value;
            double inc;
            if (double.TryParse(value, out inc))
            {
                config.Inclination = inc;
                return true;
            }
            return false;
        }
        #endregion Private Methods
    }
}
