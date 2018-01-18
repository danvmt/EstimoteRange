using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using EstimoteSdk.Recognition.Packets;
using EstimoteSdk.Service;

namespace EstimoteRange
{
    [Activity(Label = "Mono Office Detector", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity, BeaconManager.IServiceReadyCallback
    {
        private const string LIVING_ROOM_URL = "http://monostream.ch?r=1";
        private const string OFFICE_URL = "http://monostream.ch?r=2";

        BeaconManager beaconManager;

        RelativeLayout unkownPanel;
        RelativeLayout roomPanel;
        TextView roomLabel;
        TextView rssi1;
        TextView rssi2;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Main);

            unkownPanel = FindViewById<RelativeLayout>(Resource.Id.unknownPanel);
            roomPanel = FindViewById<RelativeLayout>(Resource.Id.roomPanel);
            roomLabel = FindViewById<TextView>(Resource.Id.roomLabel);
            rssi1 = FindViewById<TextView>(Resource.Id.rssi1);
            rssi2 = FindViewById<TextView>(Resource.Id.rssi2);

            beaconManager = new BeaconManager(this);
            beaconManager.Eddystone += OnEddystonesFound;
            beaconManager.Connect(this);
        }

        public void OnServiceReady()
        {
            try
            {
                beaconManager.StartEddystoneScanning();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }

        protected override void OnStop()
        {
            base.OnStop();

            beaconManager.StopEddystoneScanning();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            beaconManager.Disconnect();
        }

        private void OnEddystonesFound(object sender, BeaconManager.EddystoneEventArgs e)
        {
            var sortedEddystones = e.Eddystones.OrderByDescending(es => es.Rssi).ToList();

            UpdateNearestPlace(sortedEddystones);

            UpdateRSSI(sortedEddystones);
        }

        public void UpdateNearestPlace(IList<Eddystone> eddystones)
        {
            var nearestEddystone = eddystones.FirstOrDefault();

            if (nearestEddystone != null && nearestEddystone.IsUrl)
            {
                ShowCurrentRoom(nearestEddystone);
            }
            else
            {
                ShowNoRoom();
            }
        }

        private void ShowCurrentRoom(Eddystone es)
        {
            RunOnUiThread(() =>
            {
                roomPanel.Visibility = ViewStates.Visible;
                roomLabel.Text = RoomName(es);
                roomPanel.SetBackgroundColor(RoomColor(es));
            });
        }

        private void ShowNoRoom()
        {
            RunOnUiThread(() =>
            {
                roomPanel.Visibility = ViewStates.Gone;
            });
        }

        private void UpdateRSSI(IList<Eddystone> eddystones)
        {
            RunOnUiThread(() =>
            {
                rssi1.Visibility = ViewStates.Invisible;
                rssi2.Visibility = ViewStates.Invisible;

                if (eddystones.Count > 0)
                {
                    UpdateRSSILabel(rssi1, eddystones[0]);
                }

                if (eddystones.Count > 1)
                {
                    UpdateRSSILabel(rssi2, eddystones[1]);
                }
            });
        }

        private void UpdateRSSILabel(TextView textView, Eddystone es)
        {
            textView.Text = String.Format("RSSI {0}: {1} db", RoomName(es), es.Rssi);
            textView.Visibility = ViewStates.Visible;
        }

        private string RoomName(Eddystone es)
        {
            if (es.Url == LIVING_ROOM_URL) return "Living Room";
            if (es.Url == OFFICE_URL) return "Office";
            return "Unkown";
        }

        private Color RoomColor(Eddystone es)
        {
            if (es.Url == LIVING_ROOM_URL) return Color.DarkRed;
            if (es.Url == OFFICE_URL) return Color.DarkBlue;
            return Color.Transparent;
        }
    }
}