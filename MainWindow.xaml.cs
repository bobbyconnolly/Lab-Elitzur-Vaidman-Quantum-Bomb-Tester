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

namespace Lab_Elitzur_Vaidman_Quantum_Bomb_Tester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private QuantumExperiment _experiment;

        public MainWindow()
        {
            InitializeComponent();
            _experiment = new QuantumExperiment(this.canvas);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _experiment.Run(isExperimentWithBomb: chkWithBomb.IsChecked ?? false);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
