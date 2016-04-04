using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STIL_NET;

namespace MeasurementSensors
{
    public class StilSensor
    {
        public delegate void SensorStatusHandler(object sender,EventArgs e);
        public event SensorStatusHandler OnSensorStatus;

        private string sensorStatus = null;
        private dll_chr m_dll_chr = null;
        private sensor m_sensor = null;
        private sensorManager m_sensor_manager = new sensorManager();
        private cAcqParamMeasurement acqParamMeasurement = new cAcqParamMeasurement();
        private cNamedEvent m_measurement_event = new cNamedEvent();
        private cNamedEvent m_exit_event = new cNamedEvent();
        private cNamedEvent m_exit_event_do = new cNamedEvent();


        public string SensorStatus
        {
            get { return sensorStatus; }
            set
            {
                sensorStatus = value;
                OnSensorStatus(this, new EventArgs());
            }

        }

        public bool Init()
        {
            m_dll_chr = new dll_chr();

            if (m_dll_chr.Init() == false)
            {
                return false;
                throw new StilException("传感器连接错误。");                
            }
            return true;
        }

        public bool Release()
        {
            if (m_sensor != null)
            {
                m_sensor.Release();
            }
            m_dll_chr.Release();
            return (true);
        }

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
                    Close();
                    result = false;
                    throw new StilException("驱动初始化失败。");                    
                    
                }
            }
            else
            {
                result = false;
                throw new StilException("错误 : 打开传感器失败,没有连接传感器或传感器损坏,请检查传感器。");                
            }
            return (result);
        }

        void OnError(object sender, cErrorEventArgs e)
        {
            throw new StilException(string.Format("错误:{0}-{1}{2}", e.Exception.ErrorType, e.Exception.FunctionName, e.Exception.ErrorDetail));
        }

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
           throw new StilException(string.Format("Event : {0}", ev.ToString()));
        }

        public bool Close()
        {
            bool result = true;

            if (m_sensor != null)
            {
                m_sensor.Close();
            }
            else
            {
                result = false;
                throw new StilException("错误 : 关闭传感器异常,没有连接传感器或传感器损坏,请检查传感器。");                
            }
            return (result);
        }
    }

    public class StilException: ApplicationException
    {
        public StilException(string message ):base(message)
        { }
    } 
}
