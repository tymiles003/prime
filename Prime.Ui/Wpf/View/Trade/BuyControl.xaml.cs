﻿using System;
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

namespace Prime.Ui.Wpf.View.Trade
{
    /// <summary>
    /// Interaction logic for BuyControl.xaml
    /// </summary>
    public partial class BuyControl : UserControl
    {
        public BuyControl()
        {
            InitializeComponent();
        }

        private void CmbBuyType_Loaded(object sender, RoutedEventArgs e)
        {
            CmbBuyType.SelectedIndex = 0;
        }

        private void CmbBuyTimeInForce_Loaded(object sender, RoutedEventArgs e)
        {
            CmbBuyTimeInForce.SelectedIndex = 0;
        }
    }
}
