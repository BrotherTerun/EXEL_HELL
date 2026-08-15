using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExcelHell.Prototype
{
    [Serializable]
    public sealed class PrototypeLevelDataset
    {
        public double[] Hours;
        public double[] Salary;
        public double[] Overtime;
        public double[] Bonus;

        public double Value(string fieldId, int record