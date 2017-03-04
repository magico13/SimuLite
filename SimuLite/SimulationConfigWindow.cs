using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SimuLite
{
    public class SimulationConfigWindow : WindowBase
    {

        public SimulationConfigWindow() : base(8234, "Simulation Configuration") { }

        internal SimulationConfiguration config = new SimulationConfiguration();

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

        private string _peString = "0";

        public string PeString
        {
            get { return _peString; }
            set
            {
                if (value != _peString)
                {
                    _peString = value;
                    double pe;
                    if (double.TryParse(value, out pe))
                    {
                        config.Periapsis = pe;
                        _peString = config.Periapsis.ToString();
                    }
                }
            }
        }

        private string _apString = "0";

        public string ApString
        {
            get { return _apString; }
            set
            {
                if (value != _apString)
                {
                    _apString = value;
                    double ap;
                    if (double.TryParse(value, out ap))
                    {
                        config.Apoapsis = ap;
                        _apString = config.Apoapsis.ToString();
                    }
                }
            }
        }

        private string _inclinationString = "0";

        public string InclinationString
        {
            get { return _inclinationString; }
            set
            {
                if (value != _inclinationString)
                {
                    _inclinationString = value;
                    double inc;
                    if (double.TryParse(value, out inc))
                    {
                        config.Inclination = inc;
                        _inclinationString = config.Inclination.ToString();
                    }
                }
            }
        }

        #endregion UI Properties


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
            GUILayout.BeginVertical();

            GUILayout.Label("Selected Body:");
            GUILayout.Label(config.SelectedBody.name);

            if (config.SelectedBody == Planetarium.fetch.Home)
            {
                config.OrbitalSimulation = GUILayout.Toggle(config.OrbitalSimulation, "Orbital Simulation");
            }
            else
            {
                config.OrbitalSimulation = true;
            }

            if (config.OrbitalSimulation)
            {
                GUILayout.Label("Apoapsis:");
                ApString = GUILayout.TextField(ApString);

                GUILayout.Label("Periapsis:");
                PeString = GUILayout.TextField(PeString);

                GUILayout.Label("Inclination:");
                InclinationString = GUILayout.TextField(InclinationString);
            }

            if (GUILayout.Button("Simulate!"))
            {
                config.StartSimulation();
            }

            GUILayout.EndVertical();

            //update the ship
            if (EditorLogic.fetch?.ship != null)
            {
                config.Ship = EditorLogic.fetch.ship;
            }


            base.Draw(windowID);
        }
    }
}
