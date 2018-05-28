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

    [Activity(Label = "Aplikacja", MainLauncher = true, Icon = "@drawable/icon", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation, ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class MainActivity : Activity, ILocationListener, ISensorEventListener, IOnMapReadyCallback, IOnMapClick
    {
        Functions Funkcja = new Functions();                                    // Klasa Funkcja, przechowująca niektóre definicję funkcji
  
        List<LatLng> listLocationFromSD = new List<LatLng>();                   // Lista, do której zapisywane są lokacje z pliku
        List<Data> listHistory = new List<Data>();                              // Lista, która przechowuje dane o bieżącej lokalizacji, gyroskop, akcelerometr itd.
        List<Data> listAcc = new List<Data>();                                  // Lista przechowująca dane z akcelerometru
        List<Data> listGyro = new List<Data>();                                 // Lista przechowująca dane z żyroskopu 
        List<Data> listNewHistory = new List<Data>();                           // Lista, do której dodaje się 10 lokalizacji do błędu "Mistake"                              
        List<LatLng> listMarkerLong = new List<LatLng>();                       // Lista pozycji markerów zaznaczonych długim przyciśnięciem na mapie                   //
        List<double> listAccXFilter = new List<double>();                       // Lista przyspieszenia wzdłuż osi X do przefiltrowania
        List<double> listAccYFilter = new List<double>();                       // Lista przyspieszenia wzdłuż osi Y do przefiltrowania
        List<double> listAccZFilter = new List<double>();                       // Lista przyspieszenia wzdłuż osi Z do przefiltrowania                         
        List<double> odleglosc = new List<double>();                            // Lista przechowująca długości kolejnych kroków
        List<double> listOrientation = new List<double>();                      // Lista przechowująca dane kalibracyjne
        List<Odleglosc> listOdleglosc = new List<Odleglosc>();                  // Lista potrzebna do rysowania markerów kroków
        List<double> listCompass = new List<double>();

        PolylineOptions polyLine = new PolylineOptions();                       // Zmienna polyLine do rysowania linii na mapie
        GoogleMap gM;                                                           // Mapa        
        Location currentLocation;                                               // Aktualna lokalizacja    
        LocationManager locationManager;                                        // Zarządza pobieranie lokalizacji   
        Criteria criteriaForLocationService;                                    // Potrzebne do lokalizacji
        SensorManager mSensorManager;                                           // Potrzebne do inicjalizowania sensorów.

        public float[] mGravity = new float[3], mGeomagnetic = new float[3],    // Zmienne globalne porzebne do wyznaczania przyspieszenia, żyroskopu itd.
            mGyroscope = new float[3], mAccelerometer = new float[3];      
        public float Compass = 0f;                                              // Zmienna globalna Kompass

        public string TAG                                                       // Potrzbne do lokalizacji
        {
            get;
            private set;
        }

        TextView locationText;                                                  // Deklaracje różnych przycisków, map, pól edycyjnych itd
        MapFragment mapFragment;
        Button clearButton;
        Button mistakeButton;
        Button loadButton;
        Button startButton;
        Button saveButton;
        Button centerButton;
        Button mapType;
        Button lineButton;
        Button _3Button;
        Button stepButton;
        Button _5Button;
        Button backButton;
        Button saveLine;
        Button loadLine;
        EditText PathText;


        LatLng currentLocationStep=new LatLng(0,0);                             // Lokalizacja początkowa drogi liczonej ze stepów

        string locationProvider;                                                // Potrzebne do lokalizacji
        string currentTime;                                                     // Aktualny czas            
        string startTime;                                                       // Czas startu aplikacji                                                
        static int probki = 10;                                                 // Liczba próbek do liczenia błędu "Mistake"    
        int licznik = 0;                                                        // Licznik dotychczasowych lokalizacji potrzebnych do liczenia błędu "Mistake"
        bool mistakeButtonClicked = false;                                      // Zmienna boolowska potrzebna do liczenia błędu "Mistake"
        double currentMarkerLat, currentMarkerLon;                              // Zmienne potrzbne do liczenie błedu "Mistake" - pozycja markera, który został naniesiony przez użytkownika
        double FiltrX = 1000, FiltrY = 1000, FiltrZ = 1000;                     // Zmienne, które przypisuje się do list do filtracji, liczba 1000 to zabezpieczenie przed możliwymi błędami przy starcie             
        bool IsStepAdd = false;                                                 // Jeżeli krok jest dodadny to zmienia się na true. potrzebne przy niektórych warunkach
        int licznikkrokow = 0;                                                  // Licznik kroków, potrzebny do różnych warunków
        bool startSteps = false;                                                // Rysowanie kroków jeżeli będzie rozpoznana pierwsza lokalizacja
        int stepCounter = 0;                                                    // Zmienna potrzebna do liczenia kroków systemowych
        float[] R = new float[9], I = new float[9];                             // Zmienne potrzebne do macierzy rotacji

        System.Timers.Timer LocationTimer = new System.Timers.Timer();          // Timer zbierania aktualnej lokalizacji
        System.Timers.Timer FiltrTimer = new System.Timers.Timer();             // Timer do filtracji
        System.Timers.Timer StepTimer = new System.Timers.Timer();              // Timer do krokomierza        

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);                   // Zabezpieczenie przed wyłączaniem się ekranu                
            SetContentView(Resource.Layout.Main);
            locationText = FindViewById<TextView>(Resource.Id.Location);        // Przypisanie zmiennych do przycisków, map itd. 
            clearButton = FindViewById<Button>(Resource.Id.Clear);
            mistakeButton = FindViewById<Button>(Resource.Id.Mistake);
            loadButton = FindViewById<Button>(Resource.Id.Load);
            startButton = FindViewById<Button>(Resource.Id.Start);
            saveButton = FindViewById<Button>(Resource.Id.Save);
            PathText = FindViewById<EditText>(Resource.Id.textLoad);
            centerButton = FindViewById<Button>(Resource.Id.Center);
            saveLine = FindViewById<Button>(Resource.Id._1);
            loadLine = FindViewById<Button>(Resource.Id._2);
            mapType = FindViewById<Button>(Resource.Id.MapChange);
            backButton = FindViewById<Button>(Resource.Id.Back);
            lineButton = FindViewById<Button>(Resource.Id.Line);
            _3Button = FindViewById<Button>(Resource.Id._3);
            stepButton = FindViewById<Button>(Resource.Id._4);
            _5Button = FindViewById<Button>(Resource.Id.Filtr);
            mapFragment = (MapFragment)FragmentManager.FindFragmentById(Resource.Id.map);
            mapFragment.GetMapAsync(this);

            backButton.Enabled = false;                                         // Początkowe włączenie/wyłączenie przycisków, tekst w polach edycyjnych itd
            mistakeButton.Enabled = false;
            saveButton.Enabled = false;
            startButton.Enabled = true;
            lineButton.Enabled = false;
            saveLine.Enabled = false;
            stepButton.Enabled = false;
            PathText.Text = null;
            centerButton.Enabled = false;
            mistakeButton.Enabled = false;
            backButton.Enabled = false;
            _3Button.Enabled = false;
            _5Button.Enabled = false;
            locationText.Text = "Brak Lokalizacji";

            mSensorManager = (SensorManager)GetSystemService(SensorService);    // Potrzebne do prawidłowego działania sensorów

            InitializeLocationManager();                                        // Wywoływanie funkcji
            Timer();
            startTime = Funkcja.StartTime();
            ClearMap();
            BackButton();
            LineButton();
            Funkcja.IfPathExist();
            LoadFromSd();
            MistakeButtonClick();
            SaveLine();
            Start();
            Save();
            Center();
            LoadLine();          
            startStep();          

            LocationTimer.Enabled = true;                                    // Załączenie timerów   
            FiltrTimer.Enabled = true;
            StepTimer.Enabled = true;
        }
        private void InitializeLocationManager()                            // Funkcja do działania lokalizacji
        {
            locationManager = (LocationManager)GetSystemService(LocationService);
            criteriaForLocationService = new Criteria
            {
                Accuracy = Accuracy.Fine
            };
            IList<string> acceptableLocationProviders = locationManager.GetProviders(criteriaForLocationService, true);
            if (acceptableLocationProviders.Any())
            {
                locationProvider = acceptableLocationProviders.First();
            }
            else
            {
                locationProvider = string.Empty;
            }
            Log.Debug(TAG, "Using " + locationProvider + ".");
        }
        protected override void OnResume()                                  // Funkcja systemowa - działanie po wznowieniu
        {
            base.OnResume();

            if (currentLocation != null)
            {
                LatLngBounds trasa = new LatLngBounds(new LatLng(currentLocation.Latitude - 0.0005, currentLocation.Longitude - 0.0005),
                new LatLng(currentLocation.Latitude + 0.0005, currentLocation.Longitude + 0.0005));
                gM.MoveCamera(CameraUpdateFactory.NewLatLngBounds(trasa, 0));

            }
        }
        public void Start()                                                 // Funkcja która się wywołuje po naciśnięciu przycisku Start
        {
            startButton.Click += (object sender, EventArgs e) =>
            {
                gM.Clear();                                                 // Czyszczenie list
                listLocationFromSD.Clear();
                listHistory.Clear();
                listMarkerLong.Clear();
                listOrientation.Clear();
                listAcc.Clear();
                listGyro.Clear();
                listOdleglosc.Clear();
                odleglosc.Clear();
                stepButton.Text = "Step";

                locationManager.RequestLocationUpdates(locationProvider, 0, 0, this);                    // Rejestracja sensorów
                mSensorManager.RegisterListener(this, mSensorManager.GetDefaultSensor(SensorType.MagneticField), SensorDelay.Fastest);
                mSensorManager.RegisterListener(this, mSensorManager.GetDefaultSensor(SensorType.Accelerometer), SensorDelay.Normal);
                mSensorManager.RegisterListener(this, mSensorManager.GetDefaultSensor(SensorType.LinearAcceleration), SensorDelay.Fastest);
                mSensorManager.RegisterListener(this, mSensorManager.GetDefaultSensor(SensorType.Gyroscope), SensorDelay.Fastest);
                mSensorManager.RegisterListener(this, mSensorManager.GetDefaultSensor(SensorType.StepCounter), SensorDelay.Normal);

                startTime = Funkcja.StartTime();

                loadButton.Enabled = false;
                saveButton.Enabled = true;
                startButton.Enabled = false;
                PathText.Enabled = false;
                loadLine.Enabled = false;
                lineButton.Enabled = false;
            };
        }
        public void Save()                                                  // Funkcja zapisująca wszystko do pliku
        {
            saveButton.Click += (object sender, EventArgs e) =>
            {
                locationManager.RemoveUpdates(this);
               
                SavetoSd();
                SaveFiltration();

                currentLocation = null;
                loadButton.Enabled = true;
                startButton.Enabled = true;
                mistakeButton.Enabled = false;
                saveButton.Enabled = false;
                loadLine.Enabled = true;                
                PathText.Enabled = true;
                centerButton.Enabled = false;               
                startSteps = false;
                locationText.Text = "Brak Lokalizacji";
                mistakeButton.Text = "MISTAKE";
                listOrientation.Clear();
                licznikkrokow = 0;
            };
        }
        protected override void OnStart()                                   // Funkcja systemowa - działanie na start
        {
            base.OnStart();
        }
        protected override void OnPause()                                   // Funkcja systemowa - działanie na pauze
        {
            base.OnPause();
        }
        protected override void OnStop()                                    // Funkcja systemowa - działanie na zatrzymanie aplikacji
        {
            base.OnStop();
            if (listHistory.Count > 0)
            {
                SavetoSd();
            }
        }
        public void MistakeButtonClick()                                    // Funkcja potrzebna do liczenia błędu "Mistake"
        {
            mistakeButton.Click += (object sender, EventArgs e) =>          // Obsługa przycisku Mistake
            {
                licznik = 0;
                listNewHistory.Clear();
                mistakeButton.Text = ("MISTAKE");
                mistakeButtonClicked = true;
            };
        }
        public void Mistake()                                               // Funkcja potrzebna do liczenia błędu "Mistake"
        {
            // funkcja licząca blad na podstawie zebranych probek z gpsu i polozenia zaznaczanego na mapie przez uzytkonika
            if (mistakeButtonClicked)
            {
                if (licznik < probki)
                {
                    currentTime = Funkcja.CurrentTime();
                    Data pom = new Data();
                    pom.Zapis(currentLocation);
                    listNewHistory.Add(pom);
                    RunOnUiThread(() =>
                    {
                        mistakeButton.Text = (licznik + "/" + probki);
                    });
                    licznik++;
                }
                else // jezeli zebrala juz odpowiednią ilosc probek, liczy z nich srednią, rysuje marker sredniego polozenia, na podsatwie zaznaczonego markera przez uzytkownika oblicza odleglosc tego markera od markera sredniego 
                {
                    Funkcja.IfPathExist();
                    mistakeButtonClicked = false;

                    List<string> lines = new List<string>();
                    double Radius = Funkcja.DistanceBetweenLocations(currentMarkerLat, currentMarkerLon, Funkcja.Average(0, listNewHistory), Funkcja.Average(1, listNewHistory));

                    lines.Add(currentMarkerLat.ToString() + ";" + currentMarkerLon.ToString() + ";" + Radius.ToString());

                    var sdCardPath = Android.OS.Environment.ExternalStorageDirectory.Path;
                    var filePath_mistake = System.IO.Path.Combine(sdCardPath + Path.DirectorySeparatorChar + "Aplikacja Dane" + Path.DirectorySeparatorChar + "Mistake" + Path.DirectorySeparatorChar, DateTime.Now.ToString("yyyy:MM:d:HH:mm:ss") + ".txt");
                    System.IO.File.WriteAllLines(filePath_mistake, lines);

                    MarkerOptions AverageMarker = new MarkerOptions();
                    MarkerOptions DeviationMarker = new MarkerOptions();

                    AverageMarker.SetPosition(new LatLng(Funkcja.Average(0, listNewHistory), Funkcja.Average(1, listNewHistory)));
                    AverageMarker.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueViolet));
                    AverageMarker.SetTitle("średnie połozenie");

                    RunOnUiThread(() =>
                    {
                        mistakeButton.Text = (Radius.ToString().Substring(0, 4));
                        gM.AddMarker(AverageMarker);
                    });
                    lines.Clear();
                }
            }
        }
        public void Timer()                                                 // Funkcja do Timerów
        {
            LocationTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            LocationTimer.Interval = 1000;
            FiltrTimer.Elapsed += new ElapsedEventHandler(OnTimedEventFiltr);
            FiltrTimer.Interval = 50;
            StepTimer.Elapsed += new ElapsedEventHandler(InRealTimeSteper);
            StepTimer.Interval = 200;
        }   
        private void OnTimedEvent(object source, ElapsedEventArgs e)        // Funkcja potrzebna do Timera lokalizacji
        {
            if (currentLocation != null)
            {
                currentTime = Funkcja.CurrentTime();
                Data pom = new Data();
                pom.Zapis(currentLocation, Compass, currentTime);
                listHistory.Add(pom);
                Mistake();
            }
            RunOnUiThread(() =>
            {
                if (listMarkerLong.Count > 3)
                {
                    saveLine.Enabled = true;
                    lineButton.Enabled = true;
                }
                else
                {
                    saveLine.Enabled = false;
                    lineButton.Enabled = false;
                }
                if (listLocationFromSD.Count > 2)
                {
                    lineButton.Enabled = true;

                }
                else { lineButton.Enabled = false; }
            });
        }
        private void OnTimedEventFiltr(object source, ElapsedEventArgs e)   // Funkcja potrzebna do Timera filtrów 
        {
            if (((FiltrX != 1000) & (FiltrY != 1000)) & (FiltrZ != 1000))
            {
                listAccXFilter.Add(FiltrX);
                listAccYFilter.Add(FiltrY);
                listAccZFilter.Add(FiltrZ);
                listCompass.Add(Compass);
            }
        }
        public void OnLocationChanged(Location location)                    // Funkcja na zmianę lokacji - przypisywanie nowych Markerów na mape
        {
            MarkerOptions Marker = new MarkerOptions();
            currentLocation = location;
            if (currentLocation != null)
            {                
                centerButton.Enabled = true;
                Marker.SetPosition(new LatLng(currentLocation.Latitude, currentLocation.Longitude));
                if (mistakeButtonClicked)
                {
                    Marker.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueYellow));
                    Marker.SetRotation(Compass + 180);
                    gM.AddMarker(Marker);
                }
                else if (!mistakeButtonClicked)
                {
                    Marker.SetRotation(Compass + 180);
                    gM.AddMarker(Marker);
                }
                locationText.Text = (currentLocation.Latitude.ToString() + "; " + currentLocation.Longitude.ToString());
                if (listHistory.Count() < 1)
                {
                    LatLngBounds trasa = new LatLngBounds(new LatLng(currentLocation.Latitude - 0.0005, currentLocation.Longitude - 0.0005), new LatLng(currentLocation.Latitude + 0.0005, currentLocation.Longitude + 0.0005));
                    gM.MoveCamera(CameraUpdateFactory.NewLatLngBounds(trasa, 0));
                }
            }
        }
        public void OnSensorChanged(SensorEvent e)                          // Funkcja na zmianę sensorów
        {
            if (currentLocation != null)
            {
                Compass = (float)CompassWork(e);

                Funkcja.SaveGyro(e, listGyro, Funkcja.CurrentTime());
                SaveAcc(e);
                OnStepTaken(e);
                Compass = (float)CompassWork(e);

            }
        }
        private double CompassWork(SensorEvent e)                             // Funkcja potrzebna do działania kompasu. 
        {
            float alpha = 0.97f, Azimuth = 0f;

            if (e.Sensor.Type == SensorType.Accelerometer)
            {
                mGravity[0] = mGravity[0] * alpha + (1 - alpha) * e.Values[0];
                mGravity[1] = mGravity[1] * alpha + (1 - alpha) * e.Values[1];
                mGravity[2] = mGravity[2] * alpha + (1 - alpha) * e.Values[2];
            }
            if (e.Sensor.Type == SensorType.MagneticField)
            {
                mGeomagnetic[0] = mGeomagnetic[0] * alpha + (1 - alpha) * e.Values[0];
                mGeomagnetic[1] = mGeomagnetic[1] * alpha + (1 - alpha) * e.Values[1];
                mGeomagnetic[2] = mGeomagnetic[2] * alpha + (1 - alpha) * e.Values[2];

            }
            bool success = SensorManager.GetRotationMatrix(R, I, mGravity, mGeomagnetic);
            if (success)
            {
                float[] orientation = new float[3];
                SensorManager.GetOrientation(R, orientation);
                Azimuth = (float)(orientation[0] * (180 / Math.PI));
                Compass = (Azimuth + 360) % 360;
                SensorManager.GetRotationMatrix(R, I, mGravity, mGeomagnetic);
                FiltrX = 0;
                FiltrY = 0;
                FiltrZ = 0;

                for (int i = 0; i < 3; i++)
                {
                    FiltrX += I[i] * mGravity[i];
                    FiltrY += I[i + 3] * mGravity[i];
                    FiltrZ += I[i + 6] * mGravity[i];
                }
                FiltrX -= mGravity[0];
                FiltrY -= mGravity[1];
                FiltrZ -= mGravity[2];
            }
            return Compass;
        }
        private void SavetoSd()                                             // Zapisywanie do pamięci urządzenia
        {
            Funkcja.IfPathExist();
            List<string> lines = new List<string>();
            List<string> acc = new List<string>();
            List<string> gyro = new List<string>();
            List<string> orient = new List<string>();

            var sdCardPath = Android.OS.Environment.ExternalStorageDirectory.Path;
            var filePath_ogolny = System.IO.Path.Combine(sdCardPath + Path.DirectorySeparatorChar + "Aplikacja Dane"
               + Path.DirectorySeparatorChar + "Location Data" + Path.DirectorySeparatorChar, startTime + ".txt");
            var filePath_acc = System.IO.Path.Combine(sdCardPath + Path.DirectorySeparatorChar + "Aplikacja Dane"
                + Path.DirectorySeparatorChar + "Accelerometr" + Path.DirectorySeparatorChar, startTime + ".txt");
            var filePath_gyro = System.IO.Path.Combine(sdCardPath + Path.DirectorySeparatorChar + "Aplikacja Dane"
                + Path.DirectorySeparatorChar + "Gyroscope" + Path.DirectorySeparatorChar, startTime + ".txt");
            var filePath_step = System.IO.Path.Combine(sdCardPath + Path.DirectorySeparatorChar + "Aplikacja Dane"
                + Path.DirectorySeparatorChar + "Gyroscope" + Path.DirectorySeparatorChar, startTime + "STEPY.txt");

            foreach (var Data in listHistory)
            {

                lines.Add(Data.Accu() + ";" + Data.Long().ToString() + ";" + Data.Lat().ToString() + ";" + Data.Direction() + ";" + Data.Time());
            }
            System.IO.File.WriteAllLines(filePath_ogolny, lines);

            foreach (var Data in listGyro)
            {
                float[] gyro_tab = new float[3];
                gyro_tab = Data.Gyroscope();

                gyro.Add(gyro_tab[0].ToString() + ";" + gyro_tab[1].ToString() + ";" + gyro_tab[2].ToString() + ";" + Data.Time());
            }
            System.IO.File.WriteAllLines(filePath_gyro, gyro);

            foreach (var Data in listAcc)
            {
                float[] acc_tab = new float[3];
                acc_tab = Data.Accelerometr();
                acc.Add(acc_tab[0].ToString() + ";" + acc_tab[1].ToString() + ";" + acc_tab[2].ToString() + ";" + Data.Time());

            }
            System.IO.File.WriteAllLines(filePath_acc, acc);
       
            var filePath_accZ_step = System.IO.Path.Combine(sdCardPath + Path.DirectorySeparatorChar + "Aplikacja Dane"
              + Path.DirectorySeparatorChar + "Step Compass" + Path.DirectorySeparatorChar, startTime + "StepSystem.txt"); 
            System.IO.File.WriteAllText(filePath_accZ_step, stepCounter.ToString());

        }
        public void LoadLine()                                              // Funkcja ładująca zapisane linie z pamieci 
        {            
            loadLine.Click += (object sender, EventArgs e) =>
            {
                listMarkerLong.Clear();
                gM.Clear();
                double lat, lon;
                var sdCardPath = Android.OS.Environment.ExternalStorageDirectory.Path;
                string filePath = System.IO.Path.Combine(sdCardPath + Path.DirectorySeparatorChar + "Aplikacja Dane"
               + Path.DirectorySeparatorChar + "Lines" + Path.DirectorySeparatorChar, PathText.Text.ToString() + ".txt");
                if (File.Exists(filePath))
                {
                    if (System.IO.Path.Combine(sdCardPath + Path.DirectorySeparatorChar + "Aplikacja Dane"
               + Path.DirectorySeparatorChar + "Lines" + Path.DirectorySeparatorChar, PathText.Text.ToString() + ".txt")
               != System.IO.Path.Combine(sdCardPath + Path.DirectorySeparatorChar + "Aplikacja Dane"
               + Path.DirectorySeparatorChar + "Lines" + Path.DirectorySeparatorChar, ".txt"))
                    {
                        string[] text = System.IO.File.ReadAllLines(filePath);
                        for (int i = 0; i < text.Length; i++)
                        {
                            string linia = text[i];
                            string[] pom = linia.Split(';');
                            lat = double.Parse(pom[0].Replace(',', '.'), CultureInfo.InvariantCulture.NumberFormat);
                            lon = double.Parse(pom[1].Replace(',', '.'), CultureInfo.InvariantCulture.NumberFormat);
                            listMarkerLong.Add(new LatLng(lat, lon));
                        }
                        LatLngBounds trasa = new LatLngBounds(new LatLng(listMarkerLong.First().Latitude - 0.01, listMarkerLong.First().Longitude - 0.01), new LatLng(listMarkerLong.Last().Latitude + 0.01, listMarkerLong.Last().Longitude + 0.01));
                        gM.MoveCamera(CameraUpdateFactory.NewLatLngBounds(trasa, 0));
                        filePath = null;
                        PolylineOptions poliLine = new PolylineOptions();
                        foreach (var poli in listMarkerLong)
                        {
                            poliLine.Add(new LatLng(poli.Latitude, poli.Longitude));
                        }
                        poliLine.InvokeColor(-16711936);
                        gM.AddPolyline(poliLine);
                    }
                    else //jezeli nie ma takiego plku to wyswtli sie ostrzezenie
                    {
                        var loadDialog = new AlertDialog.Builder(this);
                        loadDialog.SetMessage("Nieprawodłowa nazwa pliku");
                        loadDialog.SetNeutralButton("OK", delegate{ });
                        loadDialog.Show();
                    }
                }
                else //jezeli nie ma takiego plku to wyswtli sie ostrzezenie
                {
                    var loadDialog = new AlertDialog.Builder(this);
                    loadDialog.SetMessage("Nieprawodłowa nazwa pliku");
                    loadDialog.SetNeutralButton("OK", delegate{ });
                    loadDialog.Show();
                }
            };
        }
        public void LoadFromSd()                                            // Funckja ladujaca zapisane trasy uzytkownika z pamieci, zapisuje je do listy listLocationFromSD aby wykorztsac je przy obliczaniu bledu linii
        {
            loadButton.Click += (object sender, EventArgs e) =>
            {
                listLocationFromSD.Clear();
                locationManager.RemoveUpdates(this);
                var sdCardPath = Android.OS.Environment.ExternalStorageDirectory.Path;
                string filePath = System.IO.Path.Combine(sdCardPath + Path.DirectorySeparatorChar + "Aplikacja Dane"
               + Path.DirectorySeparatorChar + "Location Data" + Path.DirectorySeparatorChar, PathText.Text.ToString() + ".txt");
                if (File.Exists(filePath))
                {
                    if (System.IO.Path.Combine(sdCardPath + Path.DirectorySeparatorChar + "Aplikacja Dane"
               + Path.DirectorySeparatorChar + "Location Data" + Path.DirectorySeparatorChar, PathText.Text.ToString() + ".txt")
               != System.IO.Path.Combine(sdCardPath + Path.DirectorySeparatorChar + "Aplikacja Dane"
               + Path.DirectorySeparatorChar + "Location Data" + Path.DirectorySeparatorChar, ".txt"))
                    {
                        PolylineOptions linia = new PolylineOptions();
                        string[] line;
                        LatLng first = new LatLng(0, 0);
                        LatLng last = new LatLng(0, 0);
                        double lat, lon;
                        float dir;
                        int acc;
                        string[] text = System.IO.File.ReadAllLines(filePath);

                        for (int i = 0; i < text.Length; i++)
                        {
                            line = text[i].Split(';');
                            acc = Convert.ToInt32(line[0]);
                            lat = double.Parse(line[2].Replace(',', '.'), CultureInfo.InvariantCulture.NumberFormat);
                            lon = double.Parse(line[1].Replace(',', '.'), CultureInfo.InvariantCulture.NumberFormat);
                            dir = float.Parse(line[3]);
                            linia.Add(new LatLng(lat, lon));
                            gM.AddPolyline(linia);

                            listLocationFromSD.Add(new LatLng(lat, lon));

                            if (i == 0)
                            {
                                MarkerOptions pom = new MarkerOptions();
                                first = new LatLng(lat, lon);
                                pom.SetPosition(first);
                                pom.SetTitle("Początek trasy");
                                pom.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueAzure));
                                gM.AddMarker(pom);
                            }
                            if (i == text.Length - 1)
                            {
                                last = new LatLng(lat, lon);
                            }
                        }
                        LatLngBounds trasa = new LatLngBounds(new LatLng(first.Latitude - 0.01, first.Longitude - 0.01), new LatLng(last.Latitude + 0.01, last.Longitude + 0.01));
                        gM.MoveCamera(CameraUpdateFactory.NewLatLngBounds(trasa, 0));
                        filePath = null;
                    }
                    else
                    {
                        var loadDialog = new AlertDialog.Builder(this);
                        loadDialog.SetMessage("Nieprawodłowa nazwa pliku");
                        loadDialog.SetNeutralButton("OK", delegate{});
                        loadDialog.Show();
                    }
                }
                else
                {
                    var loadDialog = new AlertDialog.Builder(this);
                    loadDialog.SetMessage("Nieprawodłowa nazwa pliku");
                    loadDialog.SetNeutralButton("OK", delegate { });
                    loadDialog.Show();
                }
            };
        }
        public void ClearMap()                                              // Funckja do obsługi przycisku czyszczenia mapy
        {
            clearButton.Click += (object sender, EventArgs e) =>
            {
                gM.Clear();
                listMarkerLong.Clear();
                mistakeButton.Enabled = false;
                backButton.Enabled = false;
            };
        }
        public void OnProviderDisabled(string provider)                     // Funkcja, która muszą być - potrzebne do interfejsu
        {

        }           
        public void OnProviderEnabled(string provider)                      // Funkcja, która muszą być - potrzebne do interfejsu
        {

        }
        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)   // Funkcja, która muszą być - potrzebne do interfejsu
        {

        }
        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)                // Funkcja, która muszą być - potrzebne do interfejsu
        {

        }
        public void OnMapReady(GoogleMap gMap)                              // Obsługa fragmentu mapy
        {
            gM = gMap;          
            if (gM != null)
            {
                gM.MapClick += (object sender, GoogleMap.MapClickEventArgs e) => //na dotniecie mapy dodaje sie marker do obliczania blędu punktu 
                {
                    MarkerOptions pom = new MarkerOptions();
                    pom.SetPosition(e.Point);
                    currentMarkerLat = e.Point.Latitude;
                    currentMarkerLon = e.Point.Longitude;
                    pom.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueBlue));
                    mistakeButton.Enabled = true;
                    gM.AddMarker(pom);
                };
                gM.MapLongClick += (object sender, GoogleMap.MapLongClickEventArgs e) => // na dlugie dotniecie rysuje sie linia wedlug ktorej oblicza sie bląd linii, musi zawierac conajmnije punkty zeby mozna bylo liczyc bląd linii
                {
                    PolylineOptions poliLine = new PolylineOptions();
                    MarkerOptions pom = new MarkerOptions();

                    listMarkerLong.Add(new LatLng(e.Point.Latitude, e.Point.Longitude));

                    foreach (var poli in listMarkerLong)
                    {
                        poliLine.Add(new LatLng(poli.Latitude, poli.Longitude));
                    }
                    poliLine.InvokeColor(-16711936);

                    gM.AddPolyline(poliLine);

                    if (listMarkerLong.Count > 0)
                    {
                        backButton.Enabled = true;
                    }
                    pom.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueGreen));
                    pom.SetPosition(new LatLng(listMarkerLong.First().Latitude, listMarkerLong.First().Longitude));
                    gM.AddMarker(pom);
                };
            }
            MapTypChange();
        }
        public void BackButton()                                            // Umozliwa cofnięcie narysowanej lini o 1 element
        {
            backButton.Click += (object sender, EventArgs e) =>
            {
                MarkerOptions pom = new MarkerOptions();
                PolylineOptions poliLine = new PolylineOptions();
                gM.Clear();
                listMarkerLong.Remove(listMarkerLong.Last());
                if (listMarkerLong.Count > 1)
                {
                    lineButton.Enabled = true;
                }
                else
                {
                    lineButton.Enabled = true;
                }
                foreach (var poli in listMarkerLong)
                {
                    poliLine.Add(new LatLng(poli.Latitude, poli.Longitude));
                }
                poliLine.InvokeColor(-16711936);
                gM.AddPolyline(poliLine);
                if (listMarkerLong.Count < 1)
                {
                    backButton.Enabled = false;
                }

                if (listMarkerLong.Count > 0)
                {
                    pom.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueGreen));
                    pom.SetPosition(new LatLng(listMarkerLong.First().Latitude, listMarkerLong.First().Longitude));
                    gM.AddMarker(pom);
                }
            };
        }
        public void LineButton()                                            // Obłsuga przycisku do liczenia błędu od linii
        {
            lineButton.Click += (object sender, EventArgs e) =>
            {
                lineButton.Text = "LINE";
                LineMistake();
            };
        }
        public void Center()                                                // Centrowanie mapy
        {
            {
                centerButton.Click += (object sender, EventArgs e) =>
                {
                    LatLngBounds trasa = new LatLngBounds(new LatLng(currentLocation.Latitude - 0.0005, currentLocation.Longitude - 0.0005), new LatLng(currentLocation.Latitude + 0.0005, currentLocation.Longitude + 0.0005));
                    gM.MoveCamera(CameraUpdateFactory.NewLatLngBounds(trasa, 0));
                };
            }
        }
        public void MapTypChange()                                          // Zmiana typu Mapy
        {
            mapType.Click += (object sender, EventArgs e) =>
            {
                if (gM.MapType == GoogleMap.MapTypeNormal)
                {
                    gM.MapType = GoogleMap.MapTypeSatellite;
                }
                else
                {
                    gM.MapType = GoogleMap.MapTypeNormal;
                }
            };
        }
        public void SaveLine()                                              // Zapis linii do pliku, mozna uruchomic gdy linia ma conajmnije 4 punkty 
        {
            saveLine.Click += (object sender, EventArgs e) =>
            {
                Funkcja.IfPathExist();
                List<string> lines = new List<string>();
                var sdCardPath = Android.OS.Environment.ExternalStorageDirectory.Path;
                var filePath_line = System.IO.Path.Combine(sdCardPath + Path.DirectorySeparatorChar + "Aplikacja Dane"
                   + Path.DirectorySeparatorChar + "Lines" + Path.DirectorySeparatorChar, DateTime.Now.ToString("yyyy:MM:d:HH:mm:ss") + ".txt");

                foreach (LatLng Data in listMarkerLong)
                {
                    lines.Add(Data.Latitude.ToString() + ";" + Data.Longitude.ToString());
                }
                System.IO.File.WriteAllLines(filePath_line, lines);
            };
        }
        public void OnStepTaken(SensorEvent e)                              // Funckja do liczenia krokow przez wbudowany steper
        {
            if (e.Sensor.Type == SensorType.StepCounter)
            {
                stepCounter++;
            }
        }
        public double[] MinMaxFunction(double[] ListAcc)                    // Funckja do zliczenia kolejnych kroków i ich długości, przekazuje się przefiltrowaną tablicę
        {
            List<double> temp = new List<double>();
            List<double> pom = new List<double>();
            List<double> pom2 = new List<double>();
            List<double> maxy = new List<double>();
            List<double> miny = new List<double>();
            List<double> kompasList = new List<double>();           
          
            string start = "0";

            for (int i = 25; i < ListAcc.Count() - 1; i++)
            {
                if (i > 0)
                {
                    if (ListAcc[i] < ListAcc[i - 1])                 //MIN
                    {
                        if (ListAcc[i + 1] > ListAcc[i])
                        {
                            pom.Add(ListAcc[i]);
                            temp.Add(pom.Min());
                            miny.Add(pom.Min());                           
                            if (start == "0")
                                start = "min";
                            pom.Clear();
                        }
                        else
                        {
                            pom.Add(ListAcc[i]);
                        }
                    }
                    if (ListAcc[i] > ListAcc[i - 1])                 //MAX
                    {
                        if (ListAcc[i + 1] < ListAcc[i])
                        {
                            pom.Add(ListAcc[i]);
                            temp.Add(pom.Max());
                            maxy.Add(pom.Max());                            
                            if (start == "0")
                                start = "max";
                            pom.Clear();
                        }
                        else
                        {
                            pom.Add(ListAcc[i]);
                        }
                    }
                }
                else
                {
                    pom.Add(ListAcc[i]);
                }
            }
            pom.Clear();
            temp.Clear();            
            double mn = 0, mx = 0;
            if ((miny.Count>3) & (maxy.Count>3))
            {
                double min = miny[0], max = maxy[0];
                for (int i=1;i<4;i++)
                {
                    if (miny[i]<min)
                    {
                        mn = i;
                        min = miny[i];
                    }
                    if(maxy[i]>max)
                    {
                        mx = i;
                        max = maxy[i];
                    }
                }
                if(start=="max")
                {
                    if(mx>mn)
                    {
                        start = "min";
                    }
                }
                if ((mn % 2) == 0)
                    mn = 0;
                else
                    mn = 1;
                if ((mx % 2) == 0)
                    mx = 0;
                else
                    mx = 1;

                for (int i = 0; i < miny.Count; i++)                                // Dodanie do listy tylko tych różnic które są na tyle duże, że reprezentują kroki
                {
                    if((i%2)==mn)
                    {
                        pom.Add(miny[i]);
                        
                    }
                }
                for (int i = 0; i < maxy.Count; i++)                                // Dodanie do listy tylko tych różnic które są na tyle duże, że reprezentują kroki
                {
                    if ((i % 2) == mx)
                    {
                        pom2.Add(maxy[i]);
                      
                    }
                }
                if(start=="max")
                {
                    if (pom.Count == pom2.Count)
                    {
                        for (int i = 0; i < pom2.Count; i++)
                        {
                            if (Math.Abs(pom2[i] - pom[i]) > 0.75) 
                            {
                                temp.Add(pom2[i]);
                                temp.Add(pom[i]);
                             
                            }                           
                        }
                    }
                    else
                    {
                        for (int i=0;i<pom2.Count-1;i++)
                        {
                            if (Math.Abs(pom2[i] - pom[i]) > 0.75)
                            {
                                temp.Add(pom2[i]);                               
                                temp.Add(pom[i]);
                              
                            }
                        }
                    }
                }
                if (start == "min")
                {
                    if (pom.Count == pom2.Count)
                    {
                        for (int i = 0; i < pom.Count; i++)
                        {
                            if (Math.Abs(pom2[i] - pom[i]) > 0.75)
                            {
                                temp.Add(pom[i]);
                                temp.Add(pom2[i]);
                                                             
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < pom.Count-1; i++)
                        {
                            if (Math.Abs(pom2[i] - pom[i]) > 0.75)
                            {
                                temp.Add(pom[i]);                           
                                temp.Add(pom2[i]);
                               
                            }
                        }
                    }
                }
            }
            kompasList.Clear();
            for(int i=0;i<ListAcc.Count();i++)
            {
                for(int j=0;j<temp.Count;j++)
                {
                    if(temp[j]==ListAcc[i])
                    {
                        kompasList.Add(listCompass[i]);
                    }
                }
            }                     
            double temporary, odl = 0;                                              // Liczenie długości kolejnych kroków
            double K = 0.36, pierw = 0.25;            
           
            for (int i = 0; i < temp.Count - 1; i++)
            {
               
                temporary = (Math.Pow(Math.Abs(temp[i]) + Math.Abs(temp[i + 1]), pierw)) * K;
                odl += temporary;
                odleglosc.Add(temporary);
                
            }
            int steplost = odleglosc.Count-licznikkrokow;                // liczba kroków niedodanych 
            if(steplost>0)
            {
                listOdleglosc.Clear();
                for (int i = steplost; i > 0; i--)
                {
                    Odleglosc pom123 = new Odleglosc();
                    pom123.zapis(odleglosc[odleglosc.Count - i], (float)kompasList[kompasList.Count - i]);
                    listOdleglosc.Add(pom123);
                }
                IsStepAdd = true;
                licznikkrokow = odleglosc.Count;
            }            
            return temp.ToArray();
        }
        public void SaveAcc(SensorEvent e)                                  // Zapisuje dane z akcelerometru
        {
            Data pom = new Data();
            if (e.Sensor.Type == SensorType.Accelerometer)
            {
                float[] Accelerometer = new float[3];
                Accelerometer[0] = e.Values[0];
                Accelerometer[1] = e.Values[1];
                Accelerometer[2] = e.Values[2];
                pom.ZapisAcc(Accelerometer, currentTime);
                listAcc.Add(pom);
            }   
        }
        public void SaveFiltration()                                        // Funkcja zapisująca Maxima i minima przefiltrowanej tablicy oraz dlugości kroków
        { 
            var sdCardPath = Android.OS.Environment.ExternalStorageDirectory.Path;
            var filePathMinMax = System.IO.Path.Combine(sdCardPath + Path.DirectorySeparatorChar + "Aplikacja Dane"
                + Path.DirectorySeparatorChar + "Step Compass" + Path.DirectorySeparatorChar, startTime + "MinMax.txt");
            List<string> linesMinMax = new List<string>();
            double[] MaxMinFiltrz = MinMaxFunction(Funkcja.Filtration(listAccZFilter));
            double odl = 0;
            for (int i = 0; i < MaxMinFiltrz.Count(); i++)
            {
                linesMinMax.Add(MaxMinFiltrz[i].ToString() + ";" + odleglosc[i].ToString());
                odl += odleglosc[i];
            }
            linesMinMax.Add(MaxMinFiltrz.Count().ToString());
            linesMinMax.Add("Łączna przebyta odległość: " + odl.ToString());
            System.IO.File.WriteAllLines(filePathMinMax, linesMinMax);                       
        }
        public void LineMistake()                                           // Funkcja do liczenia błędu Linii
        {
            int Id = 0;   //Id akutalnhie wykorztwanego wektora

            LatLng PktRzut = new LatLng(0, 0);  // polozenie punktu rzutowanego na obecny wketor
            LatLng PktRzutPre = new LatLng(0, 0); // polozenie punktu rzutowanego na poprzedni wketor 
            LatLng PktRzutNext = new LatLng(0, 0);  // polozenie punktu rzutowanego na kolejny wktor

            List<blad_lini_wektor> wektor = new List<blad_lini_wektor>(); // lista przechwoujaca wektory 
            for (int i = 0; i < listMarkerLong.Count() - 1; i++)   // dodawanie wketorow na z listy przechowującej połozenia dlugich kliknięć
            {
                Coordinate PomCoo = new Coordinate();
                LatLng poczatek = new LatLng(listMarkerLong[i].Latitude, listMarkerLong[i].Longitude);
                LatLng koniec = new LatLng(listMarkerLong[i + 1].Latitude, listMarkerLong[i + 1].Longitude);
                double aW = PomCoo.CartesianA(poczatek.Latitude, poczatek.Longitude, koniec.Latitude, koniec.Longitude);   // obiczanie wposlczynnikow prostej na ktorej lezy wktor 
                double bW = PomCoo.CartesianB(poczatek.Latitude, poczatek.Longitude, aW);
              
                blad_lini_wektor pom = new blad_lini_wektor();
                double VectorLength = Funkcja.DistanceBetweenLocations(poczatek.Latitude, poczatek.Longitude, koniec.Latitude, koniec.Longitude); // obliczanie dlugosci wektora 
                pom.zapisz(poczatek, koniec, VectorLength, aW, bW);
                wektor.Add(pom);
            }
            List<string> test = new List<string>();
            List<string> zapis = new List<string>();
            List<LatLng> zapisRzut = new List<LatLng>();
            double NewVectorLength = 0;  //odleglosc miedzy punktem a zrzutowanym punktem na obecnym wektorze  
            double NewVectorLengthNext = 0;//odleglosc miedzy punktem a zrzutowanym punktem na nastepnym wektorze   
            double NewVectorLengthPre = 0; //odleglosc miedzy punktem a zrzutowanym punktem na poprzednim wektorze  
            double VectorPoczatekPktRzutNext = 1000000; //odleglosc miedzy poczatekiem koljengo wektora a punktem rzutowanym na nim 
            double VectorPoczatekPktRzutPre = 1000000; //odleglosc miedzy poczatekiem koljengo wektora a punktem rzutowanym na nim 
            double VectorKoniecPktRzutNext = 1000000; //odleglosc miedzy poczatekiem koljengo wektora a punktem rzutowanym na nim 
            double VectorKoniecPktRzutPre = 1000000; //odleglosc miedzy poczatekiem koljengo wektora a punktem rzutowanym na nim 
            double Distance = 0, Average = 0;

            foreach (LatLng Data in listLocationFromSD)
            {
                Coordinate PomCoo = new Coordinate();
                PktRzut = PomCoo.CartesianWspolrzedneProstopadle(wektor[Id].A(), wektor[Id].B(), Data.Longitude, Data.Latitude);
                //zapis.Add(wektor[Id].Koniec().Latitude.ToString() + ";" + wektor[Id].Koniec().Longitude.ToString() + ";" + wektor[Id].Poczatek().Latitude.ToString()+ ";" + wektor[Id].Poczatek().Longitude.ToString() + ";" + wektor[Id].A() + ";" + wektor[Id].B() + ";" + Data.Latitude.ToString() + ";" + Data.Longitude.ToString()+";"+ PktRzut.Latitude.ToString() + ";" + PktRzut.Longitude.ToString());
                if (Id != 0) //jezeli nie jest to pierwszy wektor w liscie obliczam parametry poprzedniego wketora 
                {
                    PktRzutPre = PomCoo.CartesianWspolrzedneProstopadle(wektor[Id - 1].A(), wektor[Id - 1].B(), Data.Longitude, Data.Latitude);
                    NewVectorLengthPre = Funkcja.DistanceBetweenLocations(Data.Latitude, Data.Longitude, PktRzutPre.Latitude, PktRzutPre.Longitude);
                    VectorPoczatekPktRzutPre = Funkcja.DistanceBetweenLocations(wektor[Id - 1].Poczatek().Latitude, wektor[Id - 1].Poczatek().Longitude, PktRzutPre.Latitude, PktRzutPre.Longitude);
                    VectorPoczatekPktRzutPre = Funkcja.DistanceBetweenLocations(wektor[Id - 1].Koniec().Latitude, wektor[Id - 1].Koniec().Longitude, PktRzutPre.Latitude, PktRzutPre.Longitude);

                }
                if (Id < wektor.Count() - 1) //jezeli nie jest to ostatnoi wektor w liscie to obliczam parametry nestepnego wketora 
                {
                    PktRzutNext = PomCoo.CartesianWspolrzedneProstopadle(wektor[Id + 1].A(), wektor[Id + 1].B(), Data.Longitude, Data.Latitude);
                    NewVectorLengthNext = Funkcja.DistanceBetweenLocations(Data.Latitude, Data.Longitude, PktRzutNext.Latitude, PktRzutNext.Longitude);
                    VectorPoczatekPktRzutNext = Funkcja.DistanceBetweenLocations(wektor[Id + 1].Poczatek().Latitude, wektor[Id + 1].Poczatek().Longitude, PktRzutNext.Latitude, PktRzutNext.Longitude);
                    VectorKoniecPktRzutNext = Funkcja.DistanceBetweenLocations(wektor[Id + 1].Koniec().Latitude, wektor[Id + 1].Koniec().Longitude, PktRzutNext.Latitude, PktRzutNext.Longitude);


                }

                NewVectorLength = Funkcja.DistanceBetweenLocations(Data.Latitude, Data.Longitude, PktRzut.Latitude, PktRzut.Longitude);  //parametry obecnego wketora 
                double VectorPoczatekPktRzut = Funkcja.DistanceBetweenLocations(wektor[Id].Poczatek().Latitude, wektor[Id].Poczatek().Longitude, PktRzut.Latitude, PktRzut.Longitude);

                if (wektor[Id].Dlugosc() >= VectorPoczatekPktRzut)   //jezeli dlugosc wektora jest wieksza od odleglosci miedzy punktem rzutowanym a koncem wketora to wykonaja sie dalesze wrunki 
                {
                    if (Id == 0) //jezeli jest to pierwszy wektor to nie moze sie cofac 
                    {
                        if ((VectorPoczatekPktRzutNext < wektor[Id + 1].Dlugosc()) & (NewVectorLength > NewVectorLengthNext) & (VectorKoniecPktRzutNext < wektor[Id + 1].Dlugosc()))
                        {
                            Id++;   // wektor preskoczy na nastepny
                            zapisRzut.Add(PktRzutNext);// zapisze  sie punkt rzutowany na nowy wketor

                        }
                        else // w innym raizie nic sie nie zmiania, zapisauje sie punkt zrzutowany na obecny wektor
                        {
                            zapisRzut.Add(PktRzut);
                        }

                    }
                    else if (Id == wektor.Count() - 1) ////jezeli jest to ostatni wektor to nie moze isc do koljnego wketora 
                    {
                        if ((VectorPoczatekPktRzutPre < wektor[Id - 1].Dlugosc()) & (NewVectorLength > NewVectorLengthPre) & (VectorKoniecPktRzutPre < wektor[Id - 1].Dlugosc()))
                        {
                            Id--;   // wektor przeskoczy na poprzedni
                            zapisRzut.Add(PktRzutPre);// zapisze  sie punkt rzutowany na poprzedni wketor

                        }
                        else // w innym raizie nic sie nie zmiania, zapisauje sie punkt zrzutowany na obecny wektor
                        {
                            zapisRzut.Add(PktRzut);
                        }
                    }
                    else // gdy nie jestesmy na krancach listy wektorów możemy poruszac sie do przodu i do tyłu
                    {
                        if ((VectorPoczatekPktRzutNext < wektor[Id + 1].Dlugosc()) & (NewVectorLength > NewVectorLengthNext) & (VectorKoniecPktRzutNext < wektor[Id + 1].Dlugosc()))
                        {
                            Id++;   // wektor preskoczy na nastepny
                            zapisRzut.Add(PktRzutNext);// zapisze  sie punkt rzutowany na nowy wketor

                        }
                        else if ((VectorPoczatekPktRzutPre < wektor[Id - 1].Dlugosc()) & (NewVectorLength > NewVectorLengthPre) & (VectorKoniecPktRzutPre < wektor[Id - 1].Dlugosc()))
                        {
                            Id--;   // wektor przeskoczy na poprzedni
                            zapisRzut.Add(PktRzutPre);// zapisze  sie punkt rzutowany na poprzedni wketor

                        }

                        else // jezeli nie zmianiamy id wektora bo najblizej jest do obecnego
                        {
                            zapisRzut.Add(PktRzut);

                        }

                    }


                }
                else //jezeli dlugosc wektora jest mnijesza od odleglosci miedzy punktem rzutowanym a koncem wketora to funkcja przeskoczy na kolejny wktor wzgledem ktorego przypisze rzutowany punkt 
                {
                    Id++;
                    if (Id < wektor.Count()) // tylko wtedy gdy nie jest to ostatni element listy 
                    {
                        zapisRzut.Add(PktRzutNext);
                    }
                    else //jeslei jest ostatni to przerwie 
                    {
                        break;
                    }
                }

            }


            for (int i = 0; i < zapisRzut.Count(); i++)  //rysownie lini 
            {
                PolylineOptions poliLine = new PolylineOptions();
                poliLine.Add(zapisRzut[i]);
                poliLine.Add(listLocationFromSD[i]);
                poliLine.InvokeColor(-2000000);
                gM.AddPolyline(poliLine);
                Distance = Funkcja.DistanceBetweenLocations(zapisRzut[i].Latitude, zapisRzut[i].Longitude, listLocationFromSD[i].Latitude, listLocationFromSD[i].Longitude);
                Average = Average + Distance;
                zapis.Add(Distance.ToString());
            }
            Average = Average / listLocationFromSD.Count(); //liczenie sredniej 
            zapis.Add("srednia wynosi: " + Average.ToString());
            zapis.Add(zapisRzut.Count().ToString() + ";" + listLocationFromSD.Count().ToString());
            RunOnUiThread(() =>
            {
                lineButton.Text = (Average.ToString().Substring(0, 4));
            });

            var sdCardPath = Android.OS.Environment.ExternalStorageDirectory.Path;
            var filePath_lineMistake = System.IO.Path.Combine(sdCardPath + Path.DirectorySeparatorChar + "Aplikacja Dane"
              + Path.DirectorySeparatorChar + "Line mistake" + Path.DirectorySeparatorChar, DateTime.Now.ToString("yyyy:MM:d:HH:mm:ss") + ".txt");

            System.IO.File.WriteAllLines(filePath_lineMistake, zapis);

        }
        public double Orientation()                                         // Wyznaczenie orientacje czyli kierunek z dwóch współrzędnych
        {
            double kierunek = 0;
            //listOrientation[0] = Compass;
            double y = listHistory[listHistory.Count() - 1].Lat() - listHistory[listHistory.Count() - 2].Lat();
            double x = listHistory[listHistory.Count() - 1].Long() - listHistory[listHistory.Count() - 2].Long();
            if (x != 0)
            {
                kierunek = Math.Atan2(y, x) + 180 + 90;
            }
            kierunek = kierunek % 360;
            listOrientation.Add(kierunek);
            return kierunek;
        }
        public void InRealTimeSteper(object source, ElapsedEventArgs e)     // Funkcja do zliczania kroków w czasie rzeczywistym
        {
            odleglosc.Clear();
            double[] MaxMinFiltrz = MinMaxFunction(Funkcja.Filtration(listAccZFilter));
            
            RunOnUiThread(() =>
            {
                _3Button.Text = ((odleglosc.Count).ToString());
            });

            RunOnUiThread(() =>
            {
                if (currentLocation == null)
                {
                    stepButton.Enabled = false;
                }
                else
                {
                    stepButton.Enabled = true;
                }
            });
            if (startSteps)
            {
                if (IsStepAdd)
                {
                    RunOnUiThread(() =>
                    {
                        for (int i=0;i<listOdleglosc.Count;i++)
                        {
                            currentLocationStep = AddStep(currentLocationStep, listOdleglosc[i].getodleglosc(), listOdleglosc[i].getkierunek());
                        }
                        
                    });
                    IsStepAdd = false;
                }
            }
        }
        public void startStep()                                             // Obsługa przycisku do rozpoczęcia namierzania za pomocą kroków
        {
            stepButton.Click += (object sender, EventArgs e) =>
            {                
                currentLocationStep = new LatLng(currentLocation.Latitude, currentLocation.Longitude);
                MarkerOptions a = new MarkerOptions();
                a.SetPosition(new LatLng(currentLocation.Latitude, currentLocation.Longitude));
                a.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueGreen));
                gM.AddMarker(a);
                startSteps = true;                
            };
        }        
        public LatLng AddStep(LatLng Location, double distance, float kierunek)  // Funkcja zwracająca lokacje ostatniego kroku, rysuje marker kroku na mapie
        {
            // Location - lokacja ostatniego zrobionego kroku
            // distance - długośc ostatniego kroku
            // kierunek - kierunek ostatniego kroku
            Geo geo = new Geo();
            Coordinate PomCoo = new Coordinate();
            double[] Coefficients = new double[2];
            double lat, lon;
            double latdistance = distance * Math.Cos((kierunek-90) * (Math.PI / 180));
            double londistance = distance * Math.Sin((kierunek-90) * (Math.PI / 180));
            lat = Funkcja.MetersToDecimalDegrees(latdistance, Location.Latitude);
            lon = Funkcja.MetersToLonditude(londistance);
            lat += Location.Latitude;
            lon += Location.Longitude;           
            Coefficients = PomCoo.PerpedicularCartesianCoefficents(Location.Latitude, Location.Longitude, lat, lon);

            LatLng nowePolozenie = Funkcja.WspolrzedneProstopadle(Coefficients[0], Coefficients[1], Location.Longitude, Location.Latitude);
                    
            MarkerOptions pom = new MarkerOptions();
            pom.SetPosition(nowePolozenie);
            pom.SetRotation(kierunek + 180);
            pom.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueOrange));
            gM.AddMarker(pom);

            return new LatLng(nowePolozenie.Latitude, nowePolozenie.Longitude);
        }     
        public Direction GPSdirection()
        {
            if ((currentLocation!=null & listHistory.Count>1))
            {
                Direction PomDir = new Direction();
                Coordinate PomCoo = new Coordinate();
                double angle=0;
                double[] Coefficients = new double[2];     
                LatLng position1 = new LatLng(listHistory[listHistory.Count-2].Lat(), listHistory[listHistory.Count - 2].Long());
                LatLng position2 = new LatLng(listHistory[listHistory.Count - 1].Lat(), listHistory[listHistory.Count - 1].Long());
                Coefficients = PomCoo.CartesianCoefficients(position1, position2);
                PomDir.angle = Direction.DegreeBearing(position1.Latitude, position1.Longitude, position2.Latitude, position2.Longitude);
                PomDir.accuracy = listHistory[listHistory.Count - 2].Accuracy();
                return PomDir;
            }       
            else
            {
                return null;
            }
            
        }
    }
}

