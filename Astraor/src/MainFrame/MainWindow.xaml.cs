using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MeasurementSensors;

namespace MainFrame
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        public List<StilSensor.SensorValue> SensorValueList;
        MeasureNoTrigger SensorValue;

        public struct strSensorValue
        {
            public string Altitude { get; set; }
            public string Counter { get; set; }
        }
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {

        }

        private void OnStartClick(object sender, RoutedEventArgs e)
        {
            dataGrid.DataContext = null;
            try
            {
                SensorValue = new MeasureNoTrigger();
                SensorValue.StartMeasure();
            }
            catch (StilException stilEx)
            {
                MessageBox.Show(stilEx.ToString());
            }


        }

        private void OnStopClick(object sender, RoutedEventArgs e)
        {
            SensorValue.StopMeasure();
            SensorValueList = SensorValue.GetValueList;
            dataGrid.ItemsSource = SensorValueList.ConvertAll(new Converter<StilSensor.SensorValue, strSensorValue>(SensorValueToString));
        }

        public strSensorValue SensorValueToString(MeasurementSensors.StilSensor.SensorValue value)
        {
            strSensorValue strValue = new strSensorValue() { Altitude = value.Altitude.ToString(), Counter = value.Counter.ToString(), };

            return strValue;
        }

        private void OnClosed(object sender, EventArgs e)
        {
            SensorValue.StopMeasure();

        }
    }
}
