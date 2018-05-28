using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Aplikacja
{
    class Geo
    {
        public double phi;      //latitude
        public double lambda;   //longitude
        public double h = 0;
        public void Zapis(double lat, double lon)
        {
            phi = lat;
            lambda = lon;
        }
    }
}