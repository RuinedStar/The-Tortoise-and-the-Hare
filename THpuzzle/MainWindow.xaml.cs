using GameCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace THpuzzle
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        Image selected;
        Game TTHareRacing;
        BackgroundWorker _bw;
        Tuple<int, int, int, int> result;
        DoubleAnimation _shining;
        public MainWindow()
        {
            TTHareRacing = new Game();
            _shining = new DoubleAnimation(1, 0, new Duration(new TimeSpan(0, 0, 1)));
            InitializeComponent();
            _bw = new BackgroundWorker();
            _bw.WorkerSupportsCancellation = true;
            _bw.DoWork += BackGroundAlphaBetaPruning;
            _bw.RunWorkerCompleted += BackGroundFnished;
            selected = null;
            MAP.IsEnabled = false;
            Gamingpanel.IsEnabled = false;

        }

        private void cleanPromptState()
        {
            if (selected == null)
                return;
            foreach (UIElement item in MAP.Children.OfType<Button>())
            {
                item.Opacity = 0.0;
            }
        }
        private void setPromptState()
        {
            if (selected == null)
                return;
            int col = Grid.GetColumn(selected);
            int row = Grid.GetRow(selected);
            var o = MAP.Children.OfType<Button>().First(x => Grid.GetColumn(x) == col && Grid.GetRow(x) == row);
            o.Opacity = 100.0;
            foreach (int step in TTHareRacing.getPissibleTable(col, row))
            {
                if (TTHareRacing.Map[step].isSpace())
                {
                    int scol = step % 5;
                    int srow = step / 5;
                    var b = MAP.Children.OfType<Button>().First(x => Grid.GetColumn(x) == scol && Grid.GetRow(x) == srow);
                    b.Opacity = 100.0;
                }
            }
        }

        private void image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Image img = sender as Image;
            int col = Grid.GetColumn(img);
            int row = Grid.GetRow(img);

            Console.WriteLine("Image {0} C = {1}, R = {2}", img.Name, Grid.GetColumn(img), Grid.GetRow(img));

            if (TTHareRacing.InteractionAvailable(col, row))
            {
                if (selected != null) cleanPromptState();
                selected = img;
                setPromptState();
                Console.WriteLine("select Image {0}", selected.Name);
            }
            else
            {
                Console.WriteLine("Can't select this.");
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Button bt = sender as Button;
            int col = Grid.GetColumn(bt);
            int row = Grid.GetRow(bt);
            Console.WriteLine("Button {0} C = {1}, R = {2}", bt.Name, col, row);
            Console.WriteLine(TTHareRacing.Map[col, row].Status);

            if (selected != null)
            {
                int scol = Grid.GetColumn(selected);
                int srow = Grid.GetRow(selected);

                if (TTHareRacing.InteractionMovePiece(scol, srow, col, row))
                {
                    Grid.SetColumn(selected, col);
                    Grid.SetRow(selected, row);
                    ArriveDestination(selected);

                    //Check if next turn is a computer player
                    if (!IsGameOver()) _bw.RunWorkerAsync();
                }
                
                cleanPromptState();
                selected = null;
            }
            else
            {
                Console.WriteLine("Can't select this button.");
            }
        }

        private void BackGroundAlphaBetaPruning(object sender, DoWorkEventArgs e)
        {
            result = TTHareRacing.AutoControlMovePiece();
        }

        private void BackGroundFnished(object sender, RunWorkerCompletedEventArgs e)
        {
            if (result != null) Dispatcher.Invoke(new Action(UpdateUI));
        }

        void UpdateUI()
        {
            if (result.Equals(new Tuple<int, int, int, int>(-1, -1, -1, -1))) return;

            MAP.IsEnabled = false;
            int scol = result.Item1;
            int srow = result.Item2;
            int col = result.Item3;
            int row = result.Item4;

            var s = MAP.Children.OfType<Image>().First(x => Grid.GetColumn(x) == scol && Grid.GetRow(x) == srow);
            Grid.SetColumn(s, col);
            Grid.SetRow(s, row);
            ArriveDestination(s);
            if (!IsGameOver()) _bw.RunWorkerAsync();
            MAP.IsEnabled = true;
        }

        private void GameStart(object sender, RoutedEventArgs e)
        {
            if (Difficulty.SelectedIndex == 0) //Easy
                TTHareRacing.Depth = 12;
            else if (Difficulty.SelectedIndex == 2) //Crazy
                TTHareRacing.Depth = 19;
            else  //Default, Normal
                TTHareRacing.Depth = 14;

            Controlpanel.IsEnabled = false;
            _bw.CancelAsync();
            _bw.RunWorkerAsync();
            MAP.IsEnabled = true;
            Gamingpanel.IsEnabled = true;
        }

        private void HareComputer_Click(object sender, RoutedEventArgs e)
        {
            var s = sender as Button;
            TTHareRacing.HareIsComputer = !TTHareRacing.HareIsComputer;
            if (TTHareRacing.HareIsComputer == true) s.Content = "Computer";
            else s.Content = "Manual";
        }

        private void TurtoiseComputer_Click(object sender, RoutedEventArgs e)
        {
            var s = sender as Button;
            TTHareRacing.TurtoiseIsComputer = !TTHareRacing.TurtoiseIsComputer;
            if (TTHareRacing.TurtoiseIsComputer == true) s.Content = "Computer";
            else s.Content = "Manual";
        }

        private void turnover_Click(object sender, RoutedEventArgs e)
        {
            if (TTHareRacing.InteractionStuckHandle()) _bw.RunWorkerAsync();
        }

        private void ArriveDestination(object sender)
        {
            var s = sender as Image;
            int col = Grid.GetColumn(s);
            int row = Grid.GetRow(s);
            if (TTHareRacing.Map[col, row].Status == Game.MapKind.HrDest || TTHareRacing.Map[col, row].Status == Game.MapKind.TtDest) s.Visibility = System.Windows.Visibility.Collapsed;
        }

        private bool IsGameOver()
        {
            string s = TTHareRacing.InteractionGameOver();
            bool over = false;
            switch (s)
            {
                case "turtoise": s = "Tortoises Win!"; over = true; break;
                case "hare": s = "Hares Win!"; over = true; break;
                case null: break;
            }

            if (over)
            {
                MessageBox.Show(s, "Congratulations");
                MAP.IsEnabled = false;
                Controlpanel.IsEnabled = false;
                Gamingpanel.IsEnabled = false;
                return over;
            }
            return over;
        }

        private void MAP_LayoutUpdated(object sender, EventArgs e)
        {
            string st = TTHareRacing.Turn;
            if (st == "turtoise") Info.Text = "Tortoise's turn";
            else Info.Text = "Hare's turn";
        }
    }
}
