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
using Android.Locations;

namespace Aplikacja
{

    public class Data
    {

        string mAccu, mDirection;
        double mAccuracy;
        double mLong, mLat;
        double gravity_z;
        float[] mAccelerometer = new float[3], mGyroscope = new float[3];

        string mTime;
        public void Zapis(Location l, float d, string t)
        {

            mDirection = d.ToString();
            mLong = l.Longitude;
            mLat = l.Latitude;
            mAccu = l.Accuracy.ToString();
            mAccuracy = l.Accuracy;
            mTime = t;
        }
        public void ZapisAcc(float[] a, string time)
        {
            mAccelerometer[0] = a[0];
            mAccelerometer[1] = a[1];
            mAccelerometer[2] = a[2];
            mTime = time;
        }
        public void ZapisAcc_gravity_Z(double gravity)
        {           
            gravity_z = gravity;           
        }
        public void ZapisGyro(float[] g, string time)
        {
            mGyroscope[0] = g[0];
            mGyroscope[1] = g[1];
            mGyroscope[2] = g[2];
            mTime = time;
        }
        public double Long()
        {
            return mLong;
        }
        public double Lat()
        {
            return mLat;
        }
        public string Accu()
        {
            return mAccu;
        }
        public string Direction()
        {
            return mDirection;
        }
        public string Time()
        {
            return mTime;
        }
        public float[] Accelerometr()
        {
            return mAccelerometer;
        }
        public float[] Gyroscope()
        {
            return mGyroscope;
        }
        public void Zapis(Location l)
        {
            mLong = l.Longitude;
            mLat = l.Latitude;
        }
        public double LinearAccelerator_z()
        {
            
            return gravity_z;
        }
        public double Accuracy()
        {
            return mAccuracy;
        }
    }
}