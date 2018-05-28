using Android.App;
using Android.Widget;
using Android.OS;
using System;
using System.Linq;
using Android.Content;
using Android.Hardware;
using Android.Views;
using Android.Runtime;
using Android.Locations;
using System.Collections.Generic;
using Android.Util;
using Android;
using System.Globalization;
using System.Timers;
using System.IO;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Content.PM;

namespace Aplikacja
{
    public class blad_lini_wektor
    {
        LatLng mPoczatek;
        LatLng mKoniec;
        double mDlugosc, mA, mB;
        public void zapisz(LatLng poczatek, LatLng koniec, double dlugosc,double a, double b)
        {
            mPoczatek = poczatek;
            mKoniec = koniec;
            mDlugosc = dlugosc;
            mA = a;
            mB = b;

        }
        public LatLng Poczatek()
        {
            return mPoczatek;
        }
        public LatLng Koniec()
        {
            return mKoniec;
        }
        public double Dlugosc()
        {
            return mDlugosc;
        }
        public double A()
        {
            return mA;
        }
        public double B()
        {
            return mB;
        }
    }
}