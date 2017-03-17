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
            config = new SimulationConfiguration() { SimType = (StaticInformation.Simulation?.SimType).GetValueOrDefault() };

            //these are set from the last simulation
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

        private string _peString = (StaticInformation.Simulation?.Periapsis/1000)?.ToString() ?? "75";

        public string PeString
        {
            get { return _peString; }
            set { _peString = value; }
        }

        private string _apString = (StaticInformation.Simulation?.Apoapsis / 1000)?.ToString() ?? "75";

        public string ApString
        {
            get { return _apString; }
            set { _apString = value; }
        }

        private string _inclinationString = StaticInformation.Simulation?.Inclination.ToString() ?? "0";

        public string InclinationString
        {
            get { return _inclinationString; }
            set { _inclinationString = value; }
        }

        private int startPointSelection = (StaticInformation.Simulation?.MeanAnomalyAtEpoch ?? 0) > 0 ? 1 : 0; //this is kind of awful looking
                                                                                                               //It just means 0 if it was defined as 0 last time, 1 if defined as not 0, or 0 if not defined at all

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

            SimulationType currentType = config.SimType;
            config.SimType = (SimulationType)GUILayout.SelectionGrid((int)config.SimType, new string[] { "Regular", "Orbital", "Landed" }, 3);

            if (config.SimType != currentType)
            {
                MinimizeHeight();
            }

            if (config.SimType != SimulationType.REGULAR)
            {
                GUILayout.Label("Selected Body:");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("<", GUILayout.Width(10)))
                {
                    int curIndex = FlightGlobals.Bodies.IndexOf(config.SelectedBody);
                    curIndex--;
                    config.SelectedBody = FlightGlobals.Bodies[curIndex % FlightGlobals.Bodies.Count];
                }
                GUILayout.FlexibleSpace();
                GUILayout.Label(config.SelectedBody.name);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(">", GUILayout.Width(10)))
                {
                    int curIndex = FlightGlobals.Bodies.IndexOf(config.SelectedBody);
                    curIndex++;
                    config.SelectedBody = FlightGlobals.Bodies[curIndex % FlightGlobals.Bodies.Count];
                }
                GUILayout.EndHorizontal();
            }
            else if (config.SimType == SimulationType.ORBITAL)
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

                config.MeanAnomalyAtEpoch = Math.PI * startPointSelection; //0 if Pe, PI if Ap
            }
            else
            {
                //landed. TODO: Implement
            }


            bool startable = canStart();
            if (startable && GUILayout.Button("Simulate!"))
            {
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

        private bool canStart()
        {
            bool canStart = (config?.Ship?.Count > 0);
            if (config.SimType == SimulationType.ORBITAL)
            {
                canStart &= (apOk && peOk && incOk);
            }
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
    }
}
