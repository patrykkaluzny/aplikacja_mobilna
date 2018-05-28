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
    class Coordinate
    {
        double a = 6378137;     // Promień równikowy
        double b = 6356750;     // Promień biegunowy
        double h = 0;           // Odległość między rzutowanymi punktami 
        public double Eccentricity() // Współczynnik e^2
        {
            return ((a * a - b * b) / (a * a));
        }
        public double Normal(double lat)    // Normalna
        {
            return (a / (Math.Sqrt(1 - (Eccentricity() * Math.Sin(Radians(lat))* Math.Sin(Radians(lat))))));
        }
        public double pDistance(double Y, double X)
        {
            return (Math.Sqrt(X * X + Y * Y));
        }
        public double Radians(double angle)
        {
            return (angle * Math.PI / 180);
        }
        public double Degrees(double radians)
        {
            return (180 * radians / Math.PI);
        }
        public double GeographicalToCartesianX(double lat, double lon)
        {         
            return (Normal(lat) * Math.Cos(Radians(lat)) * Math.Cos(Radians(lon)));
        }
        public double GeographicalToCartesianY(double lat, double lon)
        {
            return (Normal(lat) * Math.Cos(Radians(lat)) * Math.Sin(Radians(lon)));
        }
        public double GeographicalToCartesianZ(double lat)
        {
           return ((1 - Eccentricity()) * Normal(lat) * Math.Sin(Radians(lat))); ;
        }
        public double CartesianToGeographicalLongitude(double Y, double X)
        {
            return (Degrees(Math.Atan(Y / X)));
        }
        public double CartesianToGeographicalLatitude(double Y, double X)
        {
            double tanphi = Math.Sqrt((a * a - pDistance(Y, X) * pDistance(Y, X)) / ((pDistance(Y, X) * pDistance(Y, X)) * (1 - Eccentricity())));
            return Degrees(Math.Atan(tanphi));
            //return Degrees(Math.Atan((GeographicalToCartesianZ(lat) + (Normal(lat) * Eccentricity() * Math.Sin(Radians(lat)))) / (pDistance(Y, X))));           
        }
        public double CartesianToh(double Y, double X, double lat)
        {
            return ((pDistance(Y, X) / Math.Cos(Radians(lat))) - Normal(Radians(lat)));
        }
        public double[] PerpedicularCartesianCoefficents(double lat1, double lon1, double lat2, double lon2)
        {           
            Functions pom = new Functions();
            double X1 = GeographicalToCartesianX(lat1, lon1);
            double Y1 = GeographicalToCartesianY(lat1, lon1);
            double X2 = GeographicalToCartesianX(lat2, lon2);
            double Y2 = GeographicalToCartesianY(lat2, lon2);
            double a1 = pom.WsplczynnikA(Y1, X1, Y2, X2);
            double b1 = pom.WsplczynnikB(Y1, X1, a1);
            double ap = pom.WspolczynnikAP(a1);
            double bp = pom.WsplczynnikB( lat2, lon2, ap);
            double[] Coefficients = new double[2];
            Coefficients[0] = ap;
            Coefficients[1] = bp;
            return Coefficients;
        }
        public double CartesianA(double lat1, double lon1, double lat2, double lon2)
        {
            double X1 = GeographicalToCartesianX(lat1, lon1);
            double Y1 = GeographicalToCartesianY(lat1, lon1);
            double X2 = GeographicalToCartesianX(lat2, lon2);
            double Y2 = GeographicalToCartesianY(lat2, lon2);
            return (Y2 - Y1) / (X2 - X1);
        }
        public double CartesianB(double lat1, double lon1, double a)
        {
            double X1 = GeographicalToCartesianX(lat1, lon1);
            double Y1 = GeographicalToCartesianY(lat1, lon1);
            return Y1 - (a * X1);
        }
        public LatLng CartesianWspolrzedneProstopadle(double a, double b, double lon, double lat)
        {
            double X1 = GeographicalToCartesianX(lat, lon);
            double Y1 = GeographicalToCartesianY(lat, lon);
            double ap = -1 / a;
            double bp = Y1 - ap * X1;
            double xp = (b - bp) / (ap - a);
            double yp = (ap * xp + bp);
            double Xgeo = CartesianToGeographicalLongitude(yp, xp);
            double Ygeo = CartesianToGeographicalLatitude(yp, xp);
            return new LatLng(Ygeo, Xgeo);
        }
        public double[] CartesianCoefficients(LatLng loc1, LatLng loc2)
        {
            double[] coe = new double[2];
            coe[0] = CartesianA(loc1.Latitude, loc1.Longitude, loc2.Latitude, loc2.Longitude);
            coe[1] = loc1.Latitude - (a*loc1.Longitude);
            return coe;

        }
    }
}