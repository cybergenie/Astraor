using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using STIL_NET;
/*
Example 1 Version 1.0 :
	This example shows how to launche a continuous acquisition for CCS ULTIMA.
	This example does not wait for any trigger.
	It uses a library (.NET) that facilitates programming of the acquisition.
*/


namespace Example_0
{
    class Program
    {



        static void Main(string[] args)
        {
            ConsoleKeyInfo KeyResult;

            Console.WriteLine("This example in C# shows how to launche a continuous acquisition for CCS ULTIMA (Version 1.0).");
            Console.WriteLine("This example does not wait for any trigger.");
            Console.WriteLine("The sensor is set to Altitude measuring mode.");
            Console.WriteLine("During acquisition press any key to exit, otherwise acquisition will last forever (infinite number of points).\n");

            cExample m_Example = new cExample();
            if (m_Example != null)
            {
                Console.WriteLine("Init Chr Lib");
                if (m_Example.Init())
                {
                    Console.WriteLine("Open Sensor (please wait)");
                    if (m_Example.Open(enSensorType.CCS_ULTIMA))
                    {
                        Console.WriteLine("Set acquisition parameters");
                        m_Example.SetParameter();
                        Console.WriteLine("Start Acquisition");
                        if (m_Example.StartAcquisition())
                        {
                            Thread m_thread = new Thread(new ThreadStart(m_Example.Execute));
                            m_thread.Start();

                            while (true)
                            {
                                if (Console.KeyAvailable)
                                {
                                    KeyResult = Console.ReadKey();
                                    break;
                                }
                                if (m_Example.EndExecute())
                                {
                                    break;
                                }
                            }
                            Console.WriteLine("Stop Acquisition");
                            m_Example.StopAcquisition();
                        }
                        Console.WriteLine("Close Sensor");
                        m_Example.Close();
                    }
                    Console.WriteLine("Release Chr Lib");
                    m_Example.Release();
                }
            }
            else
            {
                Console.WriteLine("cExample : Error : New");
            }
            Console.WriteLine("Acquisition is over, please press any key to exit\n");
            KeyResult = Console.ReadKey();
        }
    }



    public class cExample
    {
        //number of points to acquire
        static uint buffer_length = 20;
        //number of buffers to acquire
        static uint number_of_buffers = 5;

        private dll_chr m_dll_chr = null;
        private sensor m_sensor = null;
        private cAcqParamMeasurement acqParamMeasurement = new cAcqParamMeasurement();
        private enSensorError sError = enSensorError.MCHR_ERROR_NONE;
        private sensorManager m_sensor_manager = new sensorManager();
        private cNamedEvent m_measurement_event = new cNamedEvent();
        private cNamedEvent m_exit_event = new cNamedEvent();
        private cNamedEvent m_exit_event_do = new cNamedEvent();


        //-------------------------------------------------------------------------------------------------------------------------------------------------
        public bool Init()
        {
            m_dll_chr = new dll_chr();

            if (m_dll_chr.Init() == false)
            {
                Console.WriteLine("cExample : Error : DLL Init failed");
                return (false);
            }
            //Display DLL(s) versions               
            Console.WriteLine(string.Format("DLL_CHR.DLL :\t\t {0}", m_sensor_manager.DllChrVersion));
            Console.WriteLine(string.Format("STILSensors.DLL :\t {0}", m_sensor_manager.DllSensorsVersion));
            return (true);
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        public bool Release()
        {
            if (m_sensor != null)
            {
                m_sensor.Release();
            }
            m_dll_chr.Release();
            return (true);
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        public bool Open(enSensorType sensorType)
        {
            bool result = true;

            //open sensor
            m_sensor = m_sensor_manager.OpenUsbConnection("", sensorType, null, null);
            if (m_sensor != null)
            {
                m_sensor.OnError += new sensor.ErrorHandler(OnError);
                //get automatic parameters
                if (acqParamMeasurement.Init(m_sensor) == enSensorError.MCHR_ERROR_NONE)
                {
                    // Set buffer size (should be > 0)
                    acqParamMeasurement.BufferLength = buffer_length;
                    // Set Number of acquisition buffers per data (should be > 1)
                    acqParamMeasurement.NumberOfBuffers = number_of_buffers;
                    //set altitude and counter buffering enabled
                    acqParamMeasurement.EnableBufferAltitude.Altitude = true;
                    acqParamMeasurement.EnableBufferAltitude.Counter = true;
                    //set timeout acquisition : should be at least = ((BufferLength * averaging) / rate) + 100
                    acqParamMeasurement.Timeout = 2000;
                    //event type (here end of measurements) and callback function
                    acqParamMeasurement.EnableEvent.EventEndBuffer = true;
                    m_sensor.OnEventMeasurement += new sensor.OnEventMeasurementHandler(FuncEventMeasurement);
                }
                else
                {
                    Console.WriteLine("cExample : Open : DLL Init failed");
                    Close();
                    result = false;
                }
            }
            else
            {
                Console.WriteLine("cExample : Error : Open (No sensor or bad sensor)");
                result = false;
            }
            return (result);
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        void OnError(object sender, cErrorEventArgs e)
        {
            Console.WriteLine("cExample : Error : {0}-{1}{2}", e.Exception.ErrorType, e.Exception.FunctionName, e.Exception.ErrorDetail);
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        public bool Close()
        {
            bool result = true;

            if (m_sensor != null)
            {
                m_sensor.Close();
            }
            else
            {
                Console.WriteLine("cExample : Error : Close (No sensor or bad sensor)");
                result = false;
            }
            return (result);
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        public bool StartAcquisition()
        {
            bool result = true;

            if (m_sensor != null)
            {
                sError = m_sensor.StartAcquisition_Measurement(acqParamMeasurement);
                if (sError != enSensorError.MCHR_ERROR_NONE)
                {
                    Console.WriteLine(string.Format("cExample : Error : StartAcquisition : {0}", sError.ToString()));
                }
            }
            else
            {
                Console.WriteLine("cExample : Error : StartAcquisition (No sensor or bad sensor)");
                result = false;
            }
            return (result);
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        public bool StopAcquisition()
        {
            bool result = true;

            if (m_sensor != null)
            {
                m_sensor.StopAcquisition_Measurement();
            }
            else
            {
                Console.WriteLine("cExample : Error : StopAcquisition (No sensor or bad sensor)");
                result = false;
            }
            return (result);
        }
        //---------------------------------------------------------------------------------
        public bool SetParameter()
        {
            //set 500hz acquisition frequency
            m_sensor.ScanRate = (enFixedScanRates)enFixedScanRates_CCS_ULTIMA.CCS_ULTIMA_500HZ;
            //set averaging = 1 for acquisition
            m_sensor.Averaging = 1;
            return (true);
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        public void Execute()
        {
            float[] Altitude = new float[acqParamMeasurement.BufferLength];
            float[] Counter = new float[acqParamMeasurement.BufferLength];
            float[] BufferNullFloat = null;
            uint Len = 0;

            while (m_exit_event.Wait(0) == false)
            {
                if (m_measurement_event.Wait(10))
                {
                    sError = m_sensor.GetAltitudeAcquisitionData(ref Altitude, ref BufferNullFloat, ref Counter, ref BufferNullFloat, ref BufferNullFloat, ref Len);
                    if (sError == enSensorError.MCHR_ERROR_NONE)
                    {
                        for (uint idx = 0; idx < Len; idx++)
                        {
                            Console.WriteLine(string.Format("[{0:D3}] Altitude  = {1:F2} (Counter : {2})", idx, (float)Altitude[idx], Counter[idx]));
                        }
                    }
                    else
                    {
                        Console.WriteLine(string.Format("FuncEventMeasurement : Error : GetAltitudeAcquisitionData : {0}", sError.ToString()));
                    }
                }
            }
            m_exit_event_do.Set();
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        public bool EndExecute()
        {
            return (m_exit_event_do.Wait(10));
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------------
        public void FuncEventMeasurement(sensor.enSensorAcquisitionEvent ev)
        {

            switch (ev)
            {
                case sensor.enSensorAcquisitionEvent.EV_END_ACQUIRE:
                    m_exit_event.Set();
                    break;
                case sensor.enSensorAcquisitionEvent.EV_END_BUFFER:
                    m_measurement_event.Set();
                    break;
                case sensor.enSensorAcquisitionEvent.EV_END_MEASUREMENT:
                    m_measurement_event.Set();
                    break;
                default:
                    break;
            }
            Console.WriteLine(string.Format("Event : {0}", ev.ToString()));
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------------
    }
}
