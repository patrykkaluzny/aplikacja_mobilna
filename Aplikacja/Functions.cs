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
    public class Functions
    {
        public void IfPathExist() // sprawdza czy istnieją foldery do ktorych zapisywane sa pliki, jezeli nie ma to tworzy je
        {
            var sdCardPath = Android.OS.Environment.ExternalStorageDirectory.Path;
            var filePath_all = System.IO.Path.Combine(sdCardPath + Path.DirectorySeparatorChar, "Aplikacja Dane");
            if (!Directory.Exists(filePath_all))
            {
                Directory.CreateDirectory(sdCardPath + Path.DirectorySeparatorChar + "Aplikacja Dane");
            }
            var filePath_ogolny = System.IO.Path.Combine(filePath_all + Path.DirectorySeparatorChar, "Location Data");
            var filePath_acc = System.IO.Path.Combine(filePath_all + Path.DirectorySeparatorChar, "Accelerometr");
            var filePath_gyro = System.IO.Path.Combine(filePath_all + Path.DirectorySeparatorChar, "Gyroscope");
            var filePath_mistake = System.IO.Path.Combine(filePath_all + Path.DirectorySeparatorChar, "Mistake");
            var filePath_lineMistake = System.IO.Path.Combine(filePath_all + Path.DirectorySeparatorChar, "Line mistake");
            var filePath_line = System.IO.Path.Combine(filePath_all + Path.DirectorySeparatorChar, "Lines");
            var filePath_StepKompass = System.IO.Path.Combine(filePath_all + Path.DirectorySeparatorChar, "Step Compass");


            if (!Directory.Exists(filePath_ogolny))
            {
                Directory.CreateDirectory(filePath_all + Path.DirectorySeparatorChar + "Location Data");
            }
            if (!Directory.Exists(filePath_acc))
            {
                Directory.CreateDirectory(filePath_all + Path.DirectorySeparatorChar + "Accelerometr");
            }
            if (!Directory.Exists(filePath_gyro))
            {
                Directory.CreateDirectory(filePath_all + Path.DirectorySeparatorChar + "Gyroscope");
            }
            if (!Directory.Exists(filePath_mistake))
            {
                Directory.CreateDirectory(filePath_all + Path.DirectorySeparatorChar + "Mistake");
            }
            if (!Directory.Exists(filePath_lineMistake))
            {
                Directory.CreateDirectory(filePath_all + Path.DirectorySeparatorChar + "Line mistake");
            }
            if (!Directory.Exists(filePath_line))
            {
                Directory.CreateDirectory(filePath_all + Path.DirectorySeparatorChar + "Lines");
            }
            if (!Directory.Exists(filePath_StepKompass))
            {
                Directory.CreateDirectory(filePath_all + Path.DirectorySeparatorChar + "Step Compass");
            }
        }
        public double DistanceBetweenLocations(double lat1, double lon1, double lat2, double lon2) //oblicza odleglosc w metrach miedzy dwoma lokacjami
        {
            Location pom1 = new Location("");
            Location pom2 = new Location("");
            pom1.Latitude = lat1;
            pom1.Longitude = lon1;
            pom2.Latitude = lat2;
            pom2.Longitude = lon2;
            return pom1.DistanceTo(pom2);

        }
        public string CurrentTime() //zwraca obecny czas
        {
            return DateTime.Now.ToString("HH:mm:ss:ffff");
        }
        public double VectorMod(double lat1, double lon1, double lat2, double lon2) // oblicza dlugosc wektora 
        {
            double A = lat1 - lat2;
            double B = lon1 - lon2;

            return Math.Sqrt((A * A) + (B * B));
        }
        public string StartTime()// zwraca czas startu 
        {
            string Time;
            return Time = DateTime.Now.ToString("yyyy:MM:d:HH:mm:ss");
        }
        public void SaveGyro(SensorEvent e, List<Data> listGyro, string time) // zapisuje dane zyroskopu do podanej listy 
        {
            if (e.Sensor.Type == SensorType.Gyroscope)
            {
                Data pom = new Data();
                float[] Gyroscope = new float[3];

                Gyroscope[0] = e.Values[0];
                Gyroscope[1] = e.Values[1];
                Gyroscope[2] = e.Values[2];

                pom.ZapisGyro(Gyroscope, time);
                listGyro.Add(pom);
            }
        }
       
        public double Average(int LatOrLon, List<Data> listNewHistory) //oblicza srednie polzenie z podanych probek 
        {
            List<double> listLatitude = new List<double>();
            List<double> listLongitude = new List<double>();
            if (LatOrLon == 0)
            {
                foreach (Data wart in listNewHistory)
                {
                    listLatitude.Add(wart.Lat());
                }

                return listLatitude.Average();
            }

            else
            {
                foreach (Data wart in listNewHistory)
                {
                    listLongitude.Add(wart.Long());
                }
                return listLongitude.Average();
            }
        }
        public double WsplczynnikA(double lat1, double lon1, double lat2, double lon2)// wspolczynnik kierunkowy prostej 
        {
            return (lat2 - lat1) / (lon2 - lon1);
        } 
        public double WsplczynnikB(double lat1, double lon1, double a)// wspolczynnik b prostej
        {
            return lat1 - (a * lon1);
        } 
        public LatLng WspolrzedneProstopadle(double a, double b, double lon, double lat) // oblicza polozenie zrzutoanego prostopadle punktu na daną prosta  
        {
            double ap = -1 / a;
            double bp = lat - ap * lon;
            double xp = (b - bp) / (ap - a);
            double yp = (ap * xp + bp);

            return new LatLng(yp, xp);
        } 
        public double WspolczynnikAP(double a) // wspoczynnik protej prostopadłej
        {
            return -1 / a;
        }
        public double MetersToDecimalDegrees(double meters, double latitude)    // Przeliczanie metrów na stopnie szerokości geograficznej    
        {
            return meters / (111320 * Math.Cos(latitude * (Math.PI / 180)));
        }
        public double MetersToLonditude(double meters)                      // Przeliczanie metrów na stopnie długości geograficznej
        {
            return (meters / 111200 );
        }
        public double[] Filtration(List<double> lista)                      // Funkcja która zwraca przefiltrowaną tablicę
        {
            var LowPassCoe = MathNet.Filtering.FIR.FirCoefficients.LowPass(20, 2, 6);  //Przypisanie współczynników do LawPass filter
            MathNet.Filtering.FIR.OnlineFirFilter LowPassFilter = new MathNet.Filtering.FIR.OnlineFirFilter(LowPassCoe);  // Definicja LawPass filter
            var HighPassCoe = MathNet.Filtering.FIR.FirCoefficients.HighPass(20, 0.25, 1);   //Przypisanie współczynników do HighPass filter
            MathNet.Filtering.FIR.OnlineFirFilter HighPassFilter = new MathNet.Filtering.FIR.OnlineFirFilter(HighPassCoe);  // Definicja HighPass filter

            double[] AfterFiltr = HighPassFilter.ProcessSamples(lista.ToArray());           //Definicja tablicy i jej filtracja HighPass
            AfterFiltr = LowPassFilter.ProcessSamples(AfterFiltr);                          //Filtracja LawPass tablicy

            return AfterFiltr;
        }
    }
}