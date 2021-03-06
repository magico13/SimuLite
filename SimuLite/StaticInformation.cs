﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimuLite
{
    public static class StaticInformation
    {
        public static SimulationConfiguration Simulation { get; set; } = null;
        public static bool IsSimulating { get; set; } = false;
        public static double RemainingCoreHours { get; set; } = 0;
        public static double CurrentComplexity { get { return Simulation?.Complexity ?? 0; } }

        public static EditorFacility LastEditor = EditorFacility.None;
        public static ConfigNode LastShip = null;
    }
}
