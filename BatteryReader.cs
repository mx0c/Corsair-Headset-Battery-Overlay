using HidApiAdapter;

namespace voidProApp
{
    static class BatteryReader
    {
        const int VOID_BATTERY_MICUP = 128;

        static public string getBatteryStatusViaHID()
        {
            var devices = HidDeviceManager.GetManager().SearchDevices(0x1b1c, 0x0a14);
            HidDevice device = null;

            foreach (var d in devices)
            {
                var id = d.Path().Split('&')[6].Substring(0,4);
                if (id == "0001")
                {
                    device = d;
                    break;
                }
            }

            if (device == null)
                return "couldn't find device";

            device.Connect();

            //write Buffer
            byte[] data_request = { 0xC9, 0x64 };
            int res = device.Write(data_request);

            //read Buffer
            byte[] data_read = new byte[5];
            device.Read(data_read, 5);

            device.Disconnect();

            //If Charging
            if (data_read[4] == 0 || data_read[4] == 4 || data_read[4] == 5)
            {
                return "Battery Charging";
            }

            //If MicUp
            if (data_read[2] > VOID_BATTERY_MICUP)
            {
                return (data_read[2] - VOID_BATTERY_MICUP).ToString() + "%";
            }

            return data_read[2].ToString() + "%";
        }

    }
}
