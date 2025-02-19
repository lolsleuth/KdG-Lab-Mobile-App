using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System.Collections.ObjectModel;

namespace TennisApp.Views
{
    public partial class BluetoothConnectionPage : ContentPage
    {
        private IAdapter _adapter;
        private ObservableCollection<IDevice> _devices;
        private IDevice? _selectedDevice;
        private IDevice? _connectedDevice;

        public BluetoothConnectionPage()
        {
            InitializeComponent();
            _adapter = CrossBluetoothLE.Current.Adapter;
            _devices = new ObservableCollection<IDevice>();
            deviceList.ItemsSource = _devices; // Bind the CollectionView to the devices list
        }

        private async Task<bool> EnsurePermissions()
        {
            // Check and request ACCESS_FINE_LOCATION permission
            var locationStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (locationStatus != PermissionStatus.Granted)
            {
                locationStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (locationStatus != PermissionStatus.Granted)
                {
                    await DisplayAlert("Permission Denied", "Location permission is required for Bluetooth scanning.", "OK");
                    return false;
                }
            }

            // Check and request BLUETOOTH_SCAN and BLUETOOTH_CONNECT permissions (for Android 12+)
            var bluetoothStatus = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
            if (bluetoothStatus != PermissionStatus.Granted)
            {
                bluetoothStatus = await Permissions.RequestAsync<Permissions.Bluetooth>();
                if (bluetoothStatus != PermissionStatus.Granted)
                {
                    await DisplayAlert("Permission Denied", "Bluetooth permissions are required for Bluetooth scanning.", "OK");
                    return false;
                }
            }

            return true;
        }

        private async void btnScan_Clicked(object sender, EventArgs e)
        {
            // Ensure permissions are granted
            if (!await EnsurePermissions())
            {
                return;
            }

            try
            {
                // Ensure Bluetooth is enabled
                if (!CrossBluetoothLE.Current.IsOn)
                {
                    await DisplayAlert("Error", "Bluetooth is not enabled.", "OK");
                    return;
                }

                // Clear the list of devices before scanning
                _devices.Clear();

                // Start scanning for devices
                _adapter.DeviceDiscovered += (s, a) =>
                {
                    // Add the full IDevice object to the collection
                    if (!_devices.Contains(a.Device))
                    {
                        _devices.Add(a.Device);
                    }
                };

                await _adapter.StartScanningForDevicesAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to scan: {ex.Message}", "OK");
            }
        }

        private void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get the selected device from the CollectionView
            _selectedDevice = e.CurrentSelection.FirstOrDefault() as IDevice;

            // Enable or disable the Connect button based on whether a device is selected
            btnConnect.IsEnabled = _selectedDevice != null;
        }

        private async void btnConnect_Clicked(object sender, EventArgs e)
        {
            if (_selectedDevice == null)
            {
                await DisplayAlert("Error", "No device selected.", "OK");
                return;
            }

            try
            {
                // Connect to the selected device
                await _adapter.ConnectToDeviceAsync(_selectedDevice);
                _connectedDevice = _selectedDevice;

                // Update UI
                btnConnect.IsEnabled = false;
                btnDisconnect.IsEnabled = true;
                await DisplayAlert("Success", $"Connected to {_connectedDevice.Name}", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to connect: {ex.Message}", "OK");
            }
        }

        private async void btnDisconnect_Clicked(object sender, EventArgs e)
        {
            if (_connectedDevice == null)
            {
                await DisplayAlert("Info", "No device is currently connected.", "OK");
                return;
            }

            try
            {
                // Disconnect from the connected device
                await _adapter.DisconnectDeviceAsync(_connectedDevice);
                _connectedDevice = null;

                // Update UI
                btnConnect.IsEnabled = true;
                btnDisconnect.IsEnabled = false;
                await DisplayAlert("Success", "Disconnected from device.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to disconnect: {ex.Message}", "OK");
            }
        }
    }
}