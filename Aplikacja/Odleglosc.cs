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
    class Odleglosc
    {
        double odleglosc;
        float kierunek;
        public void zapis(double o, float k)
        {
            odleglosc = o;
            kierunek = k;
        }
        public double getodleglosc()
            {
            return odleglosc;
            }
        public float getkierunek()
        {
            return kierunek;
        }
    }
}