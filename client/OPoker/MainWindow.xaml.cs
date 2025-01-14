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
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OPoker {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged {

        private readonly MyTcpClient Client;
        private Room _MyRoom;
        private string username = "";
        private string room_id = "";
        private Image _Card1;
        private Image _Card2;
        private Image _Card3;
        private Image _Card4;
        private Image _Card5;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyname) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }

        public Room MyRoom {
            get => _MyRoom;
            set { _MyRoom = value; OnPropertyChanged("MyRoom"); }
        }
        public Image Card1 {
            get => _Card1;
            set { _Card1 = value; OnPropertyChanged("Card1"); }
        }
        public Image Card2 {
            get => _Card2;
            set { _Card2 = value; OnPropertyChanged("Card2"); }
        }
        public Image Card3 {
            get => _Card3;
            set { _Card3 = value; OnPropertyChanged("Card3"); }
        }
        public Image Card4 {
            get => _Card4;
            set { _Card4 = value; OnPropertyChanged("Card4"); }
        }
        public Image Card5 {
            get => _Card5;
            set { _Card5 = value; OnPropertyChanged("Card5"); }
        }
        public int PlayerNo { get; set; }


        public MainWindow() {
            Client = new MyTcpClient();
            Client.Connect();
            MyRoom = new Room();
            InitializeComponent();
        }

        private void BtnJoin_Click(object sender, RoutedEventArgs e) {
            if (string.IsNullOrEmpty(UsernameInput.Text)) {
                Username.Text = "User Name is Empty. Enter your User Name below";
            } else if (string.IsNullOrEmpty(RoomidInput.Text)) {
                RoomBlock.Text = "Room ID is Empty. Enter the Room ID below";
            } else {
                username = UsernameInput.Text;
                room_id = RoomidInput.Text;
                MyRoom = Client.JoinRoom(username, room_id);
                if (MyRoom is null) {
                    RoomBlock.Text = "Room ID is not valid. Enter the valid Room ID below";
                } else {
                    ButtonOptions.Visibility = Visibility.Collapsed;
                    RoomBlock.Text = "Your shareable Room ID is";
                    MainView.Visibility = Visibility.Visible;
                    ListenSocket();
                }
            }
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e) {
            if (string.IsNullOrEmpty(UsernameInput.Text)) {
                Username.Text = "User Name is Empty. Enter your User Name below";
            } else {
                username = UsernameInput.Text;
                MyRoom = Client.CreateRoom(username);
                if (MyRoom is null) {
                    Trace.WriteLine("MyRoom is Null, BtnCreate_Click.MainWindow");
                } else {
                    room_id = MyRoom.Room_id;
                    RoomBlock.Text = "Your shareable Room ID is";
                    RoomidInput.Text = room_id;
                    ButtonOptions.Visibility = Visibility.Collapsed;
                    MainView.Visibility = Visibility.Visible;
                    StartBtn.Visibility = Visibility.Visible;
                    PlayerNo = 1;
                    ListenSocket();
                }
            }
        }

        private void ListenSocket() {
            var bw = new BackgroundWorker();
            bw.DoWork += (sender, args) => {
                UpdateRoom(Client.RcvMsg());
            };
        }
        public void UpdateRoom(Room room) {
            MyRoom = room;
            // update cards on table
            Trace.WriteLine("Received room:" + MyRoom);
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e) {
            Client.Start(username, room_id);
        }

        private void BtnRaise_Click(object sender, RoutedEventArgs e) {
            int amt;
            try {
                amt = int.Parse(RaisedValue.Text);
            } catch (Exception) { return; }
            var bet = new RaiseCmd {
                username = username,
                room = room_id,
                amt = amt,
            };
            byte[] msg = JsonSerializer.SerializeToUtf8Bytes(bet);
            Client.SendMsg(msg);
        }

        private void BtnFold_Click(object sender, RoutedEventArgs e) {
            var fold = new Command {
                kind = "FOLD",
                username = username,
                room = room_id
            };
            byte[] msg = JsonSerializer.SerializeToUtf8Bytes(fold);
            Client.SendMsg(msg);
        }
    }
}
