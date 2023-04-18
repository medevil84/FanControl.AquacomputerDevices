using FanControl.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanControl.AquacomputerDevices.Devices
{
    internal class AllDevices
    {
        private AllDevices() { }

        public static IEnumerable<IAquacomputerDevice> GetDevices()
        {
            yield return new HighFlowNextDevice();
            yield return new QuadroDevice();
            yield return new OctoDevice();
        }

        public static IEnumerable<int> GetSupportedProductIds()
        {
            return GetDevices().Select(x => x.GetProductId());
        }

        public static IAquacomputerDevice GetDevice(HidLibrary.HidDevice id, IPluginLogger logger)
        {
            return GetDevices().FirstOrDefault(x => x.GetProductId().Equals(id.Attributes.ProductId)).AssignDevice(id, logger);
        }
    }

    public class DeviceBaseSensor<T> : Plugins.IPluginSensor
    {
        private readonly string _description;
        private readonly string _fieldName;
        private readonly Func<float?> _lambda_get;
        private readonly Action _lambda_update;

        internal DeviceBaseSensor(string fieldName, string description, Func<float?> lambda_get, Action lambda_update = null)
        {
            _fieldName = fieldName;
            _description = description;
            _lambda_get = lambda_get ?? new Func<float?>(() =>  null);
            _lambda_update = lambda_update ?? new Action(() => { });
        }

        public string Id => _fieldName;
        public string Name => _description;
        public float? Value => _lambda_get();

        public void Update() => _lambda_update();
    }

    public class DeviceBaseControlSensor<T> : DeviceBaseSensor<T>, Plugins.IPluginControlSensor
    {
        private readonly Action<float?> _lambda_set;
        private readonly Action _lambda_reset;
        
        internal DeviceBaseControlSensor(string fieldName, string description, Func<float?> lambda_get, Action lambda_update, Action<float?> lambda_set, Action lambda_reset) : 
            base(fieldName, description, lambda_get, lambda_update)
        {
            _lambda_set = lambda_set ?? new Action<float?>((x) => { });
            _lambda_reset = lambda_reset ?? new Action(() => { });
        }

        public void Reset() => _lambda_reset();

        public void Set(float val) => _lambda_set(val);
    }
}
