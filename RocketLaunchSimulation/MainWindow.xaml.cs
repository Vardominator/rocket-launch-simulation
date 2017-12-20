using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Threading;
using System.Threading.Tasks;

using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;

namespace RocketLaunchSimulation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // min rocket y: 300
            // max rocket y: 37
            // say low earth orbit is 20

            InitializeComponent();
            methodSelection.SelectedIndex = 1;
            distances = new ChartValues<ObservablePoint>();
            velocities = new ChartValues<ObservablePoint>();
            thrusts = new ChartValues<ObservablePoint>();
            accelerations = new ChartValues<ObservablePoint>();

            Formatter = value => value.ToString("F2");
            
            DataContext = this;
        }

        private async void launchButton_Click(object sender, RoutedEventArgs e)
        {
            launchButton.IsEnabled = false;
            resetButton.IsEnabled = false;

            string method;
            if (methodSelection.SelectedIndex == 0)
            {
                method = "Euler";
            }
            else
            {
                method = "Runge-Kutta";
            }

            var simulation = new Simulation(double.Parse(dtTextbox.Text), int.Parse(stepsTextbox.Text), method);
            simulation.setRocketParams(double.Parse(rocketMassTextbox.Text), double.Parse(exhaustVelocityTextbox.Text), double.Parse(ejectionRateTextbox.Text), double.Parse(dragConstantTextbox.Text));
            simulation.setSimulationParams(double.Parse(initialPositionTextbox.Text), double.Parse(initialVelocityTextbox.Text));

            foreach (var rocketStatus in simulation.Run())
            {
                var time = rocketStatus.Item5;
                var distance = rocketStatus.Item1;
                var velocity = rocketStatus.Item2;
                var acceleration = rocketStatus.Item3;
                var thrust = rocketStatus.Item4;

                flightTimeLabel.Content = time.ToString("F2");
                distances.Add(new ObservablePoint(time, distance));
                velocities.Add(new ObservablePoint(time, velocity));
                accelerations.Add(new ObservablePoint(time, acceleration));
                thrusts.Add(new ObservablePoint(time, thrust));

                distanceListBox.Items.Add($"{distance}");
                distanceListBox.ScrollIntoView(distanceListBox.Items[distanceListBox.Items.Count - 1]);
                velocityListBox.Items.Add($"{velocity}");
                velocityListBox.ScrollIntoView(velocityListBox.Items[velocityListBox.Items.Count - 1]);
                thrustListBox.Items.Add($"{thrust}");
                thrustListBox.ScrollIntoView(thrustListBox.Items[thrustListBox.Items.Count - 1]);
                accelerationListBox.Items.Add($"{acceleration}");
                accelerationListBox.ScrollIntoView(accelerationListBox.Items[accelerationListBox.Items.Count - 1]);

                var canvasHeight = Map(distance, 0, 20, 300, 20);
                Canvas.SetTop(rocket, canvasHeight);

                await Task.Delay(200);

                if (Canvas.GetTop(rocket) <= 20)
                {
                    lowEarthOrbitLabel.Foreground = Brushes.Green;
                    break;
                }

            }

            resetButton.IsEnabled = true;
            launchButton.IsEnabled = true;

            if (Canvas.GetTop(rocket) > 20)
            {
                lowEarthOrbitLabel.Foreground = Brushes.Red;
            }

        }

        private void resetButton_Click(object sender, RoutedEventArgs e)
        {
            distances.Clear();
            velocities.Clear();
            thrusts.Clear();
            accelerations.Clear();

            distanceListBox.Items.Clear();
            velocityListBox.Items.Clear();
            thrustListBox.Items.Clear();
            accelerationListBox.Items.Clear();

            lowEarthOrbitLabel.Foreground = Brushes.Black;
            Canvas.SetTop(rocket, 300);
            launchButton.IsEnabled = true;
            DataContext = this;
        }

        public static double Map(double value, double from1, double to1, double from2, double to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        public ChartValues<ObservablePoint> distances { get; set; }
        public ChartValues<ObservablePoint> velocities { get; set; }
        public ChartValues<ObservablePoint> accelerations { get; set; }
        public ChartValues<ObservablePoint> thrusts { get; set; }
        public Func<double, string> Formatter { get; set; }
    }

    public class Simulation
    {
        public double dt;
        public int steps;
        public string method;

        public static double rocketMass;
        public static double exhaustVelocity;
        public static double ejectionRate;
        public static double dragConstant;

        public static double initialPosition;
        public static double initialVelocity;

        public Simulation(double timeChange, int numSteps, string method)
        {
            Console.WriteLine(method);
            dt = timeChange;
            steps = numSteps;
            this.method = method;
        }

        public void setRocketParams(double m, double ev, double er, double dc)
        {
            rocketMass = m;
            exhaustVelocity = ev;
            ejectionRate = er;
            dragConstant = dc;
        }

        public void setSimulationParams(double ip, double iv)
        {
            initialPosition = ip;
            initialVelocity = iv;
        }

        public IEnumerable<Tuple<double, double, double, double, double>> Run()
        {
            double y = initialPosition;
            double v = initialVelocity;
            double a = Acceleration(v);
            double t = 0;

            for (int i = 0; i < steps - 1; i++)
            {
                var th = Thrust(v);
                if (method.Equals("Runge-Kutta"))
                {
                    var k1 = dt * Acceleration(v);
                    var k2 = dt * Acceleration(v + k1 / 2);
                    var k3 = dt * Acceleration(v + k2 / 2);
                    var k4 = dt * Acceleration(v + k3);
                    a = Acceleration(v);

                    v = v + k1 / 6 + k2 / 3 + k3 / 3 + k4 / 6;
                    y = y + dt * v;
                }
                else if (method.Equals("Euler"))
                {
                    a = Acceleration(v);
                    v += a * dt;
                    y += v * dt;
                }
                t += dt;
                yield return new Tuple<double, double, double, double, double>(y, v, a, th, t);
            }

        }

        public static double Acceleration(double velocity)
        {
            return Thrust(velocity) - dragConstant * Math.Pow(velocity, 2) - 9.80;
        }

        public static double Thrust(double velocity)
        {
            return (velocity + exhaustVelocity) * (ejectionRate / rocketMass);
        }
    }

}
