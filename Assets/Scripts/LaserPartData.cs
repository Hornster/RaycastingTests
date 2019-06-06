using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public class LaserPartData
    {
        public Vector2 StartPoint { get; set; }
        public Vector2 Direction { get; set; }
        public double Length { get; set; }

        public Vector2 GetEndPoint()
        {
            return StartPoint + Direction * (float)Length;
        }
    }
}
