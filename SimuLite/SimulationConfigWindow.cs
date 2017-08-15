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
            config = new SimulationConfiguration();

            //these are set from the last simulation
            if (StaticInformation.Simulation != null)
            {
                config.SelectedBody = StaticInformation.Simulation.SelectedBody;
                config.SimType = StaticInformation.Simulation.SimType;

                PeString = (StaticInformation.Simulation.Periapsis / 1000).ToString();
                ApString = (StaticInformation.Simulation.Apoapsis / 1000).ToString();
                InclinationString = StaticInformation.Simulation.Inclination.ToString();
                startPointSelection = (StaticInformation.Simulation.MeanAnomalyAtEpoch != 0) ? 1 : 0;
                LatString = StaticInformation.Simulation.Latitude.ToString();
                LonString = StaticInformation.Simulation.Longitude.ToString();
            }

            SetPe(PeString);
            SetAp(ApString);
            SetInc(InclinationString);
            SetLat(LatString);
            SetLon(LonString);
        }

        internal SimulationConfiguration config;

        #region UI Properties
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


        private double _coreHoursToBuy = 0;
        private double _coreHoursToBuyCost = 0;
        private string _coreHoursBuyStr = "0";
        public string CoreHoursBuy
        {
            get { return _coreHoursBuyStr; }
            set
            {
                if (_coreHoursBuyStr != value)
                {
                    _coreHoursBuyStr = value;
                    if (double.TryParse(value, out _coreHoursToBuy))
                    {
                        Dictionary<string, string> variables = new Dictionary<string, string>();
                        variables.Add("H", _coreHoursToBuy.ToString()); //this way you can buy them in bulk at cheaper rates maybe
                                                                  //other variables? R&D level? Scientists/Engineers?
                        _coreHoursToBuyCost = MagiCore.MathParsing.ParseMath("SL_COREHR_COST", Configuration.Instance.CoreHourCost, variables);
                    }
                }
            }
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

        private string _latString = "0";

        public string LatString
        {
            get { return _latString; }
            set { _latString = value; }
        }

        private string _lonString = "0";

        public string LonString
        {
            get { return _lonString; }
            set { _lonString = value; }
        }


        private double? _trivialLimit = null;

        public double TrivialLimit
        {
            get
            {
                if (_trivialLimit == null)
                {
                    //R&D level, VAB level, SPH level
                    Dictionary<string, string> vars = new Dictionary<string, string>()
                    {
                        ["RND"] = SimuLite.GetFacilityLevel(SpaceCenterFacility.ResearchAndDevelopment).ToString(),
                        ["VAB"] = SimuLite.GetFacilityLevel(SpaceCenterFacility.VehicleAssemblyBuilding).ToString(),
                        ["SPH"] = SimuLite.GetFacilityLevel(SpaceCenterFacility.SpaceplaneHangar).ToString()
                    };

                    _trivialLimit = MagiCore.MathParsing.ParseMath("SL_TRIVIAL_LIMIT", Configuration.Instance.TrivialLimit, vars);
                }
                return _trivialLimit.GetValueOrDefault();
            }
        }

        private bool _showPurchaseOption = false;
        public bool ShowPurchaseOption
        {
            get { return _showPurchaseOption; }
            set
            {
                if (value != _showPurchaseOption)
                {
                    _showPurchaseOption = value;
                    MinimizeHeight();
                }
            }
        }
        #endregion UI Properties


        #region String Verification
        private GUIStyle regularTextField = null;
        private GUIStyle redColorTextField = null;
        private bool apOk = true, peOk = true, incOk = true;
        private bool latOk = true, lonOk = true;
        #endregion


        public override void Draw(int windowID)
        {
            //planet
            //orbit?
            //  //apoapsis/periapsis
            //  //startat ap/pe?
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

            //the stuff to purchase more core hours should be hidden until needed

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

            if (config.SimType != SimulationType.REGULAR) //orbital or landed only
            {
                GUILayout.Label("Selected Body:");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("<", GUILayout.Width(30)))
                {
                    int curIndex = FlightGlobals.Bodies.IndexOf(config.SelectedBody);
                    curIndex--;
                    config.SelectedBody = FlightGlobals.Bodies[curIndex % FlightGlobals.Bodies.Count];
                }
                GUILayout.FlexibleSpace();
                GUILayout.Label(config.SelectedBody.name);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(">", GUILayout.Width(30)))
                {
                    int curIndex = FlightGlobals.Bodies.IndexOf(config.SelectedBody);
                    curIndex++;
                    config.SelectedBody = FlightGlobals.Bodies[curIndex % FlightGlobals.Bodies.Count];
                }
                GUILayout.EndHorizontal();
            }
            if (config.SimType == SimulationType.ORBITAL) //if orbital, show orbital stuff
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
                config.MeanAnomalyAtEpoch = Math.PI * startPointSelection;
                GUILayout.EndHorizontal();
            }
            else if (config.SimType == SimulationType.LANDED)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Latitude:");
                latOk = SetLat(GUILayout.TextField(LatString, latOk ? regularTextField : redColorTextField, GUILayout.Width(100)));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Longitude:");
                lonOk = SetLon(GUILayout.TextField(LonString, lonOk ? regularTextField : redColorTextField, GUILayout.Width(100)));
                GUILayout.EndHorizontal();
            }

            #region Core Hour Purchasing
            //List complexity
            GUILayout.BeginHorizontal();
            GUILayout.Label("Complexity: ");
            GUILayout.Label(config.Complexity.ToString("N2") + " core-hours/s");
            GUILayout.EndHorizontal();

            GUILayout.Label("Max simulation time: ");
            double time = StaticInformation.RemainingCoreHours / config.Complexity;
            GUILayout.Label((config.Complexity < TrivialLimit || HighLogic.CurrentGame.Mode != Game.Modes.CAREER) ? "Trivial - Infinite Duration" : MagiCore.Utilities.GetFormattedTime(time, true));

            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER && (ShowPurchaseOption = GUILayout.Toggle(ShowPurchaseOption, "Purchase core-hours")))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Curent core-hours: ");
                GUILayout.Label(StaticInformation.RemainingCoreHours.ToString("N2") + " core-hours");
                GUILayout.EndHorizontal();

                GUILayout.Label("Additional core-hours:");
                GUILayout.BeginHorizontal();
                CoreHoursBuy = GUILayout.TextField(CoreHoursBuy, GUILayout.Width(100));
                if (_coreHoursToBuy > 0)
                {
                    if (GUILayout.Button("√" + Math.Ceiling(_coreHoursToBuyCost)))
                    {
                        //purchase the core hours!
                        if (Funding.Instance.Funds >= _coreHoursToBuyCost)
                        {
                            Funding.Instance.AddFunds(-_coreHoursToBuyCost, TransactionReasons.None);
                            StaticInformation.RemainingCoreHours += _coreHoursToBuy;
                        }
                    }
                }
                else
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Invalid");
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();

            }
            #endregion Core Hour Purchasing

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

        #region Private Methods
        private bool canStart()
        {
            bool canStart = (config?.Ship?.Count > 0);
            if (config.SimType == SimulationType.ORBITAL)
            {
                canStart &= (apOk && peOk && incOk);
            }
            else if (config.SimType == SimulationType.LANDED)
            {
                canStart &= (latOk && lonOk);
            }

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

        private bool SetLat(string value)
        {
            _latString = value;
            double lat;
            if (double.TryParse(value, out lat))
            {
                config.Latitude = lat;
                return true;
            }
            return false;
        }

        private bool SetLon(string value)
        {
            _lonString = value;
            double lon;
            if (double.TryParse(value, out lon))
            {
                config.Longitude = lon;
                return true;
            }
            return false;
        }
        #endregion Private Methods
    }
}
