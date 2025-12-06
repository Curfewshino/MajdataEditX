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
using System.Windows.Shapes;

namespace MajdataEdit
{
    /// <summary>
    /// ConnectShare.xaml 的交互逻辑
    /// </summary>
    public partial class ConnectShare : Window
    {
        Action<string, int> connectFunc;
        public ConnectShare(Action<string, int> connectFunc)
        {
            InitializeComponent();
            this.connectFunc = connectFunc;
        }

        private void Connect_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                connectFunc(ConnectIP.Text, int.Parse(ConnectPort.Text));
            }
            catch (Exception)
            {
                MessageBox.Show(MainWindow.GetLocalizedString("ConnectFailed"), MainWindow.GetLocalizedString("Error"));
            }
            Close();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
