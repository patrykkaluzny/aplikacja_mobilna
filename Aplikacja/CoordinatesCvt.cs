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
    class CoordinatesCvt : Coordinate
    {
        public Geo CretesianToGeo(Cartesian XYZ)
        {
            Geo geo = null;
            geo.phi = CartesianToGeographicalLatitude(XYZ.Y, XYZ.X);
            geo.lambda = CartesianToGeographicalLongitude(XYZ.Y, XYZ.X);
            geo.h = CartesianToh(XYZ.Y, XYZ.X, geo.phi);
            return geo;
        }
        public Cartesian GeoToCartesian(Geo plh)
        {
            Cartesian cart = null;
            cart.X = GeographicalToCartesianX(plh.phi, plh.lambda);
            cart.Y = GeographicalToCartesianY(plh.phi, plh.lambda);
            cart.Z = GeographicalToCartesianZ(plh.phi);            
            return cart;
        }
    }
}