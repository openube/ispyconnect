using System;
using System.Linq;
using SlimDX.DirectInput;

namespace iSpyApplication.Joystick
{
    public class JoystickDevice
    {
        private SlimDX.DirectInput.Joystick _joystick;
        private JoystickState _state;
        private readonly int[] _axis = new int[21];

        public int AxisCount { get; private set; }

        public bool[] Buttons { get; private set; }

        public JoystickDevice()
        {
            int a = 0;
            while (a < 21)
            { _axis[a] = 0; a++; }

            AxisCount = 0;
        }

        private void Poll()
        {
            try
            {
                _joystick.Poll();
                _state = _joystick.GetCurrentState();
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
        }

        public string[] FindJoysticks()
        {
            var dinput = new DirectInput();
            return dinput.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AttachedOnly).Select(device => device.InstanceName + "|" + device.InstanceGuid.ToString()).ToArray();
        }

        public bool AcquireJoystick(Guid guid)
        {
            try
            {
                if (_joystick != null)
                {
                    _joystick.Unacquire();
                    _joystick = null;
                }

                var dinput = new DirectInput();
                foreach (DeviceInstance device in dinput.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AttachedOnly))
                {
                    if (device.InstanceGuid==guid)
                    {
                        _joystick = new SlimDX.DirectInput.Joystick(dinput, device.InstanceGuid);
                    }
                }

                if (_joystick != null)
                {
                    foreach (DeviceObjectInstance deviceObject in _joystick.GetObjects())
                    {
                        if ((deviceObject.ObjectType & ObjectDeviceType.Axis) != 0)
                            _joystick.GetObjectPropertiesById((int)deviceObject.ObjectType).SetRange(-100, 100);
                    }

                    _joystick.Acquire();

                    var cps = _joystick.Capabilities;
                    AxisCount = cps.AxesCount;

                    UpdateStatus();
                }               
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
                return false;
            }

            return true;
        }

        public void ReleaseJoystick()
        {
            if (_joystick!=null)
                _joystick.Unacquire();
        }

        public void UpdateStatus()
        {
            Poll();
            if (_state != null)
            {
                _axis[0] = _state.AngularAccelerationX;
                _axis[1] = _state.AngularAccelerationY;
                _axis[2] = _state.AngularAccelerationZ;
                _axis[3] = _state.AccelerationX;
                _axis[4] = _state.AccelerationY;
                _axis[5] = _state.AccelerationZ;
                _axis[6] = _state.ForceX;
                _axis[7] = _state.ForceY;
                _axis[8] = _state.ForceZ;
                _axis[9] = _state.TorqueX;
                _axis[10] = _state.TorqueY;
                _axis[11] = _state.TorqueZ;
                _axis[12] = _state.RotationX;
                _axis[13] = _state.RotationY;
                _axis[14] = _state.RotationZ;
                _axis[15] = _state.VelocityX;
                _axis[16] = _state.VelocityY;
                _axis[17] = _state.VelocityZ;
                _axis[18] = _state.X;
                _axis[19] = _state.Y;
                _axis[20] = _state.Z;

                Buttons = _state.GetButtons();
            }
        }

        public int[] Dpads
        {
            get { int[] pow = _state.GetPointOfViewControllers(); return pow; }
        }

        public int[] Axis
        {
            get
            {
                return _axis;
            }
        }
    }
}
