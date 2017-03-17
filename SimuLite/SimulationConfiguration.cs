using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SimuLite
{
    public class SimulationConfiguration
    {
        private ShipConstruct _ship;
        /// <summary>
        /// The Ship to simulate
        /// </summary>
        public ShipConstruct Ship
        {
            get { return _ship; }
            set
            {
                _ship = value;
                CalculateComplexity();
            }
        }


        private CelestialBody _selectedBody = Planetarium.fetch.Home;
        /// <summary>
        /// The planet/moon that the simulation will take place around
        /// </summary>
        public CelestialBody SelectedBody
        {
            get { return _selectedBody ?? Planetarium.fetch.Home; }
            set
            {
                if (_selectedBody != value)
                {
                    _selectedBody = value;
                    //If not simulating at home
                    //if (value != null && value != Planetarium.fetch.Home)
                    //{
                    //    OrbitalSimulation = true;
                    //}
                    //else 
                    if (value == null)
                    {
                        _selectedBody = Planetarium.fetch.Home;
                    }
                }
            }
        }

        private double _duration;
        /// <summary>
        /// The expected length of the simulation
        /// </summary>
        public double Duration
        {
            get { return _duration; }
            set
            {
                if (_duration != value)
                {
                    _duration = value;
                    if (value <= 0)
                    {
                        _duration = 1000.0 * 365 * 86400; //one thousand Earth years
                    }
                }
            }
        }

        private double _complexity;

        public double Complexity
        {
            get { return _complexity; }
            set { _complexity = value; }
        }

        private double? _ut = null;
        /// <summary>
        /// The time to simulate at. Default is null. Null means now.
        /// </summary>
        public double? UT
        {
            get { return _ut ?? Planetarium.GetUniversalTime(); }
            private set
            {
                if (_ut != value)
                {
                    _ut = value;
                    CalculateComplexity();
                }
            }
        }

        /// <summary>
        /// Whether the given UT is relative to now (true) or absolute (false, default)
        /// </summary>
        public bool IsDeltaUT { get; private set; }

        private VesselCrewManifest _crew;
        /// <summary>
        /// The crew to use on the vessel
        /// </summary>
        public VesselCrewManifest Crew
        {
            set { _crew = value; }
            get
            {
                if (_crew != null)
                {
                    return _crew;
                }
                VesselCrewManifest manifest = KSP.UI.CrewAssignmentDialog.Instance.GetManifest();
                if (manifest == null)
                {
                    manifest = HighLogic.CurrentGame.CrewRoster.DefaultCrewForVessel(EditorLogic.fetch.ship.SaveShip(), null, true);
                }
                return manifest;
            }
        }

        #region Orbital Parameters
        private SimulationType _simType = SimulationType.REGULAR;

        public SimulationType SimType
        {
            get { return _simType; }
            set
            {
                if (_simType != value)
                {
                    _simType = value;
                    CalculateComplexity();
                }
            }
        }

        //private bool _orbitalSimulation = false;
        ///// <summary>
        ///// Whether the simulation is in orbit or on land
        ///// </summary>
        //public bool OrbitalSimulation
        //{
        //    get { return _orbitalSimulation; }
        //    set
        //    {
        //        if (_orbitalSimulation != value)
        //        {
        //            _orbitalSimulation = value;
        //            //CalculateComplexity();
        //        }
        //    }
        //}

        //private double _altitude = 0;
        ///// <summary>
        ///// The orbital altitude to orbit at. Default 0 or 1000+atmosphere height
        ///// </summary>
        //public double Altitude
        //{ 
        //    get { return _altitude; }
        //    set
        //    {
        //        if (_altitude != value)
        //        {
        //            _altitude = value;
        //            if (SelectedBody.atmosphere && _altitude < (SelectedBody.atmosphereDepth + 1000))
        //            {
        //                _altitude = SelectedBody.atmosphereDepth + 1000;
        //            }
        //            //CalculateComplexity(); //shouldn't affect it
        //        }
        //    }
        //}

        private double _inclination;
        /// <summary>
        /// The inclination for the orbit. Default 0.
        /// </summary>
        public double Inclination
        {
            get { return _inclination; }
            set
            {
                if (_inclination != value)
                {
                    _inclination = value % 360;
                    //CalculateComplexity(); //shouldn't affect it
                }
            }
        }

        private double _apoapsis = 1;

        public double Apoapsis
        {
            get { return _apoapsis; }
            set
            {
                _apoapsis = value;
                if (value < Periapsis)
                {
                    _apoapsis = Periapsis;
                }
                if (_apoapsis <= 0)
                {
                    _apoapsis = 1;
                }
            }
        }

        private double _periapsis = 0;

        public double Periapsis
        {
            get { return _periapsis; }
            set
            {
                _periapsis = value;
                if (value > Apoapsis)
                {
                    _periapsis = Apoapsis;
                }
            }
        }

        public double Eccentricity
        {
            get
            {
                return (Apoapsis - Periapsis) / (Apoapsis + Periapsis + 2*SelectedBody.Radius);
            }
        }

        private double _lan = 0;
        public double LongOfAsc
        {
            get { return _lan; }
            set
            {
                _lan = value;
            }
        }

        private double _argPe = 0;

        public double ArgPe
        {
            get { return _argPe; }
            set { _argPe = value % 360; }
        }

        private double _mEpoch = 0;

        public double MeanAnomalyAtEpoch //Is this an angle or a time? //Let's try angle
        {
            get { return _mEpoch; }
            set { _mEpoch = value % (2*Math.PI); }
        }


        #endregion Orbital Parameters

        #region Public Methods
        /// <summary>
        /// Sets the simulation time given the time string
        /// </summary>
        /// <param name="UTString">The string which is parsed to a time.</param>
        /// <param name="relative">If the time is relative to now, or absolute. Default: absolute (false)</param>
        public void SetTime(string UTString, bool relative=false)
        {
            IsDeltaUT = relative;
            double time = MagiCore.Utilities.ParseTimeString(UTString, toUT: !relative); //if relative, then we want a timespan, not a fixed (1 based) UT
            if (relative)
            {
                time += Planetarium.GetUniversalTime();
            }
            UT = time;
        }

        /// <summary>
        /// Sets the duration of the simulation given a duration string
        /// </summary>
        /// <param name="durationString">The duration, given as a string</param>
        public double SetDuration(string durationString)
        {
            Duration = MagiCore.Utilities.ParseTimeString(durationString, toUT: false);
            return Duration;
        }
        /// <summary>
        /// Calculates the cost of the simulation and caches it in Complexity
        /// </summary>
        /// <returns>The simulation cost</returns>
        public double CalculateComplexity()
        {
            try
            {
                CelestialBody Kerbin = Planetarium.fetch.Home;
                Dictionary<string, string> vars = new Dictionary<string, string>();
                vars.Add("L", Duration.ToString()); //Sim length in seconds
                vars.Add("M", SelectedBody.Mass.ToString()); //Body mass
                vars.Add("KM", Kerbin.Mass.ToString()); //Kerbin mass
                vars.Add("A", SelectedBody.atmosphere ? "1" : "0"); //Presence of atmosphere
                vars.Add("S", (SelectedBody != Planetarium.fetch.Sun && SelectedBody.referenceBody != Planetarium.fetch.Sun) ? "1" : "0"); //Is a moon (satellite)

                float out1, out2;
                vars.Add("m", Ship.GetTotalMass().ToString()); //Vessel loaded mass
                vars.Add("C", Ship.GetShipCosts(out out1, out out2).ToString()); //Vessel loaded cost
                vars.Add("dT", (UT - Planetarium.GetUniversalTime()).ToString()); //How far ahead in time the simulation is from right now (or negative for in the past)

                //vars.Add("s", SimCount.ToString()); //Number of times simulated this editor session //temporarily disabled


                CelestialBody Parent = SelectedBody;
                if (Parent != Planetarium.fetch.Sun)
                {
                    while (Parent.referenceBody != Planetarium.fetch.Sun)
                    {
                        Parent = Parent.referenceBody;
                    }
                }
                double orbitRatio = 1;
                if (Parent.orbit != null)
                {
                    if (Parent.orbit.semiMajorAxis >= Kerbin.orbit.semiMajorAxis)
                        orbitRatio = Parent.orbit.semiMajorAxis / Kerbin.orbit.semiMajorAxis;
                    else
                        orbitRatio = Kerbin.orbit.semiMajorAxis / Parent.orbit.semiMajorAxis;
                }
                vars.Add("SMA", orbitRatio.ToString());
                vars.Add("PM", Parent.Mass.ToString());

            vars.Add("T", ((int)SimType).ToString()); //simulation type (0=regular, 1=orbital, 2=landed)


            switch (SimType)
            {
                case SimulationType.REGULAR: Complexity = MagiCore.MathParsing.ParseMath(Configuration.Instance.SimComplexityRegular, vars); break;
                case SimulationType.ORBITAL: Complexity = MagiCore.MathParsing.ParseMath(Configuration.Instance.SimComplexityOrbital, vars); break;
                case SimulationType.LANDED: Complexity = MagiCore.MathParsing.ParseMath(Configuration.Instance.SimComplexityLanded, vars); break;
                default: Complexity = MagiCore.MathParsing.ParseMath(Configuration.Instance.SimComplexityRegular, vars); break;
            }
            return Complexity;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Starts the simulation with the defined parameters
        /// </summary>
        public void StartSimulation()
        {
            CalculateComplexity(); //make sure it's up to date
            SimuLite.Instance.ActivateSimulation(this);

            setGameUT();
            if (SimType == SimulationType.REGULAR)
            {
                //start new launch on launchpad/runway
                startRegularLaunch();
            }
            else
            {
                //start new launch in spaaaaacccceee
                VesselSpawner.VesselData vessel = makeVessel();
                Guid? id = null;
                if ((id = VesselSpawner.CreateVessel(vessel)) != null)
                {
                    Debug.Log("[SimuLite] Vessel added to world.");
                    //vessel exists, now switch to it
                    FlightDriver.StartAndFocusVessel(HighLogic.CurrentGame, FlightGlobals.Vessels.FindIndex(v => v.id == id));
                }
                else
                {
                    Debug.Log("[SimuLite] Failed to create vessel.");
                }
            }
        }
        #endregion Public Methods

        #region Private Methods
        

        private void setGameUT()
        {
            HighLogic.CurrentGame.flightState.universalTime = UT.GetValueOrDefault();
        }

        private void startRegularLaunch()
        {
            //save the ship to a temporary file
            string craftFile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/Ships/temp.craft";
            Ship.SaveShip().Save(craftFile);

            //start a new launch with that temp file
            FlightDriver.StartWithNewLaunch(craftFile, EditorLogic.FlagURL, EditorLogic.fetch.launchSiteName, Crew);
        }

        private VesselSpawner.VesselData makeVessel()
        {
            VesselSpawner.VesselData data = new VesselSpawner.VesselData();
            if (SimType == SimulationType.ORBITAL)
            {
                data.orbit = new Orbit(Inclination, Eccentricity, (Periapsis + Apoapsis + 2 * SelectedBody.Radius) / 2.0, LongOfAsc, ArgPe, 0, UT.Value, SelectedBody);
                data.orbit.meanAnomalyAtEpoch = MeanAnomalyAtEpoch;
                data.altitude = Periapsis;
                data.orbiting = true;
            }
            else
            {
                data.orbit = null;
                data.altitude = null;
                data.orbiting = false;
                data.latitude = 0;
                data.longitude = 0;
            }

            data.body = SelectedBody;
            data.shipConstruct = Ship;
            //data.crew = manifest.GetAllCrew();
            data.flagURL = EditorLogic.FlagURL;
            data.owned = true;
            data.vesselType = VesselType.Ship;

            foreach (ProtoCrewMember pcm in Crew?.GetAllCrew(false) ?? new List<ProtoCrewMember>())
            {
                if (pcm == null)
                {
                    continue;
                }
                VesselSpawner.CrewData crewData = new VesselSpawner.CrewData();
                crewData.name = pcm.name;
                if (data.crew == null)
                {
                    data.crew = new List<VesselSpawner.CrewData>();
                }
                data.crew.Add(crewData);
            }

            return data;
        }
        #endregion Private Methods
    }
}
