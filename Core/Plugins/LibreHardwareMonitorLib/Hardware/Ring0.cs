// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace LibreHardwareMonitor.Hardware
{
    internal static class Ring0
    {
        private static KernelDriver _driver;
        private static string _filePath;
        private static Mutex _isaBusMutex;
        private static Mutex _pciBusMutex;
        private static Mutex _ecMutex;

        private static readonly StringBuilder _report = new();
        private static readonly string _driverFileName = 
            Software.OperatingSystem.Is64Bit ? "WinRing0x64.sys" : "WinRing0.sys";

        private static void InitializeKernelDriver(string serviceName)
        {
            _driver = new KernelDriver(serviceName, "WinRing0_1_2_0");
            _driver.Open();
        }

        public static void InstallKernelDriver(string serviceName, string directoryPath)
        {
            InitializeKernelDriver(serviceName);
            InstallKernelDriverCore(directoryPath);
        }

        public static void UninstallKernelDriver(string serviceName)
        {
            InitializeKernelDriver(serviceName);

            if (_driver != null)
            {
                try
                {
                    if (_driver.IsOpen)
                    {
                        _driver.Close();
                    }

                    _driver.Delete();
                    _driver = null;
                }
                catch
                {
                    _report.AppendLine("Status: Uninstalling driver failed");
                }
            }
        }

        private static string GetDriverFileName(string directoryName)
        {
            return Path.Combine(directoryName, _driverFileName);
        }

        private static void InstallKernelDriverCore(string directoryPath)
        {
            _filePath = GetDriverFileName(directoryPath);

            if (_filePath != null && ExtractDriver(_filePath))
            {
                if (_driver.Install(_filePath, out string installError))
                {
                    _driver.Open();

                    if (!_driver.IsOpen)
                        _report.AppendLine("Status: Opening driver failed after install");
                }
                else
                {
                    // install failed, try to delete and reinstall
                    _driver.Delete();

                    // wait a short moment to give the OS a chance to remove the driver
                    Thread.Sleep(2000);

                    if (_driver.Install(_filePath, out string secondError))
                    {
                        _driver.Open();

                        if (!_driver.IsOpen)
                            _report.AppendLine("Status: Opening driver failed after reinstall");
                    }
                    else
                    {
                        _report.Append("Status: Installing driver \"").Append(_filePath).Append("\" failed").AppendLine((File.Exists(_filePath) ? " and file exists" : string.Empty));
                        _report.Append("First Exception: ").AppendLine(installError);
                        _report.Append("Second Exception: ").AppendLine(secondError);
                    }
                }

                if (!_driver.IsOpen)
                {
                    _driver.Delete();
                    DeleteDriver();
                }
            }
            else
            {
                _report.AppendLine("Status: Extracting driver failed");
            }
        }

        public static bool IsOpen => _driver != null;

        public static void Open()
        {
            // no implementation for unix systems
            if (Software.OperatingSystem.IsUnix)
                return;

            if (_driver != null)
                return;

            // clear the current report
            _report.Length = 0;

            InitializeKernelDriver(GetServiceName());

            if (!_driver.IsOpen)
            {
                // driver is not loaded, try to install and open
                InstallKernelDriverCore(Path.GetDirectoryName(typeof(Ring0).Assembly.Location));
            }

            if (!_driver.IsOpen)
                _driver = null;

/*
            const string isaMutexName = "Global\\Access_ISABUS.HTP.Method";

            try
            {
#if NETFRAMEWORK
                //mutex permissions set to everyone to allow other software to access the hardware
                //otherwise other monitoring software cant access
                var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
                var securitySettings = new MutexSecurity();
                securitySettings.AddAccessRule(allowEveryoneRule);
                _isaBusMutex = new Mutex(false, isaMutexName, out _, securitySettings);
#else
                _isaBusMutex = new Mutex(false, isaMutexName);
#endif
            }
            catch (UnauthorizedAccessException)
            {
                try
                {
#if NETFRAMEWORK
                    _isaBusMutex = Mutex.OpenExisting(isaMutexName, MutexRights.Synchronize);
#else
                    _isaBusMutex = Mutex.OpenExisting(isaMutexName);
#endif
                }
                catch
                { }
            }
*/

            const string pciMutexName = "Global\\Access_PCI";

            try
            {
#if NETFRAMEWORK
                //mutex permissions set to everyone to allow other software to access the hardware
                //otherwise other monitoring software cant access
                var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
                var securitySettings = new MutexSecurity();
                securitySettings.AddAccessRule(allowEveryoneRule);
                _pciBusMutex = new Mutex(false, pciMutexName, out _, securitySettings);
#else
                _pciBusMutex = new Mutex(false, pciMutexName);
#endif
            }
            catch (UnauthorizedAccessException)
            {
                try
                {
#if NETFRAMEWORK
                    _pciBusMutex = Mutex.OpenExisting(pciMutexName, MutexRights.Synchronize);
#else
                    _pciBusMutex = Mutex.OpenExisting(pciMutexName);
#endif
                }
                catch
                { }
            }

            const string ecMutexName = "Global\\Access_EC";

            try
            {
#if NETFRAMEWORK
                //mutex permissions set to everyone to allow other software to access the hardware
                //otherwise other monitoring software cant access
                var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
                var securitySettings = new MutexSecurity();
                securitySettings.AddAccessRule(allowEveryoneRule);
                _ecMutex = new Mutex(false, ecMutexName, out _, securitySettings);
#else
                _ecMutex = new Mutex(false, ecMutexName);
#endif
            }
            catch (UnauthorizedAccessException)
            {
                try
                {
#if NETFRAMEWORK
                    _ecMutex = Mutex.OpenExisting(ecMutexName, MutexRights.Synchronize);
#else
                    _ecMutex = Mutex.OpenExisting(ecMutexName);
#endif
                }
                catch
                { }
            }
        }

        private static bool ExtractDriver(string filePath)
        {
            string resourceName = $"{nameof(LibreHardwareMonitor)}.Resources.{_driverFileName}";
            Assembly assembly = typeof(Ring0).Assembly;

            string[] names = assembly.GetManifestResourceNames();
            byte[] buffer = null;

            for (int i = 0; i < names.Length; i++)
            {
                if (names[i].Replace('\\', '.') == resourceName)
                {
                    using Stream stream = assembly.GetManifestResourceStream(names[i]);

                    if (stream != null)
                    {
                        buffer = new byte[stream.Length];
                        stream.Read(buffer, 0, buffer.Length);
                    }
                }
            }

            if (buffer == null)
                return false;
            
            try
            {
                using FileStream target = new(filePath, FileMode.Create);

                target.Write(buffer, 0, buffer.Length);
                target.Flush();
            }
            catch (IOException)
            {
                // for example there is not enough space on the disk
                return false;
            }

            // make sure the file is actually written to the file system
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (stopwatch.ElapsedMilliseconds < 2000)
            {
                try
                {
                    if (File.Exists(filePath) && new FileInfo(filePath).Length == buffer.Length)
                        return true;
                }
                catch
                { }

                Thread.Sleep(1);
            }

            // file still has not the right size, something is wrong
            return false;
        }

        private static void DeleteDriver()
        {
            try
            {
                // try to delete the driver file
                if (_filePath != null && File.Exists(_filePath))
                    File.Delete(_filePath);

                _filePath = null;
            }
            catch
            { }
        }

        private static string GetServiceName()
        {
            string name;

            try
            {
                ProcessModule processModule = Process.GetCurrentProcess().MainModule;
                if (!string.IsNullOrEmpty(processModule?.FileName))
                {
                    name = Path.GetFileNameWithoutExtension(processModule.FileName);
                    if (!string.IsNullOrEmpty(name))
                        return GetName(name);
                }
            }
            catch
            {
                // Continue with the other options.
            }

            name = GetNameFromAssembly(Assembly.GetExecutingAssembly());
            if (!string.IsNullOrEmpty(name))
                return GetName(name);

            name = GetNameFromAssembly(typeof(Ring0).Assembly);
            if (!string.IsNullOrEmpty(name))
                return GetName(name);

            name = nameof(LibreHardwareMonitor);
            return GetName(name);

            static string GetNameFromAssembly(Assembly assembly)
            {
                return assembly?.GetName().Name;
            }

            static string GetName(string name)
            {
                return $"R0{name}".Replace(" ", string.Empty).Replace(".", "_");
            }
        }

        private static string GetFilePath()
        {
            string filePath;

            try
            {
                ProcessModule processModule = Process.GetCurrentProcess().MainModule;
                if (!string.IsNullOrEmpty(processModule?.FileName))
                {
                    filePath = Path.ChangeExtension(processModule.FileName, ".sys");
                    if (TryCreate(filePath))
                        return filePath;
                }
            }
            catch
            {
                // Continue with the other options.
            }

            filePath = GetPathFromAssembly(Assembly.GetExecutingAssembly());
            if (!string.IsNullOrEmpty(filePath) && TryCreate(filePath))
                return filePath;

            filePath = GetPathFromAssembly(typeof(Ring0).Assembly);
            if (!string.IsNullOrEmpty(filePath) && TryCreate(filePath))
                return filePath;

            try
            {
                filePath = Path.GetTempFileName();
                if (!string.IsNullOrEmpty(filePath))
                {
                    filePath = Path.ChangeExtension(filePath, ".sys");
                    if (TryCreate(filePath))
                        return filePath;
                }
            }
            catch
            {
                return null;
            }

            return null;

            static string GetPathFromAssembly(Assembly assembly)
            {
                try
                {
                    string location = assembly?.Location;
                    return !string.IsNullOrEmpty(location) ? Path.ChangeExtension(location, ".sys") : null;
                }
                catch
                {
                    return null;
                }
            }

            static bool TryCreate(string path)
            {
                try
                {
                    using (File.Create(path))
                        return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public static void Close()
        {
            if (_driver != null)
            {
                _driver.Close();
                _driver = null;
            }

            if (_isaBusMutex != null)
            {
                _isaBusMutex.Close();
                _isaBusMutex = null;
            }

            if (_pciBusMutex != null)
            {
                _pciBusMutex.Close();
                _pciBusMutex = null;
            }

            if (_ecMutex != null)
            {
                _ecMutex.Close();
                _ecMutex = null;
            }
        }

        public static string GetReport()
        {
            if (_report.Length > 0)
            {
                StringBuilder r = new();
                r.AppendLine("Ring0");
                r.AppendLine();
                r.Append(_report);
                r.AppendLine();
                return r.ToString();
            }

            return null;
        }

        public static bool WaitIsaBusMutex(int millisecondsTimeout)
        {
            if (_isaBusMutex == null)
                return true;


            try
            {
                return _isaBusMutex.WaitOne(millisecondsTimeout, false);
            }
            catch (AbandonedMutexException)
            {
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        public static void ReleaseIsaBusMutex()
        {
            _isaBusMutex?.ReleaseMutex();
        }

        public static bool WaitPciBusMutex(int millisecondsTimeout)
        {
            if (_pciBusMutex == null)
                return true;


            try
            {
                return _pciBusMutex.WaitOne(millisecondsTimeout, false);
            }
            catch (AbandonedMutexException)
            {
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        public static void ReleasePciBusMutex()
        {
            _pciBusMutex?.ReleaseMutex();
        }

        public static bool WaitEcMutex(int millisecondsTimeout)
        {
            if (_ecMutex == null)
                return true;


            try
            {
                return _ecMutex.WaitOne(millisecondsTimeout, false);
            }
            catch (AbandonedMutexException)
            {
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        public static void ReleaseEcMutex()
        {
            _ecMutex?.ReleaseMutex();
        }

        public static bool ReadMsr(uint index, out uint eax, out uint edx)
        {
            if (_driver == null)
            {
                eax = 0;
                edx = 0;
                return false;
            }

            ulong buffer = 0;
            bool result = _driver.DeviceIOControl(Interop.Ring0.IOCTL_OLS_READ_MSR, index, ref buffer);
            edx = (uint)((buffer >> 32) & 0xFFFFFFFF);
            eax = (uint)(buffer & 0xFFFFFFFF);
            return result;
        }

        public static bool ReadMsr(uint index, out uint eax, out uint edx, GroupAffinity affinity)
        {
            GroupAffinity previousAffinity = ThreadAffinity.Set(affinity);
            bool result = ReadMsr(index, out eax, out edx);
            ThreadAffinity.Set(previousAffinity);
            return result;
        }

        public static bool WriteMsr(uint index, uint eax, uint edx)
        {
            if (_driver == null)
                return false;


            WriteMsrInput input = new() { Register = index, Value = ((ulong)edx << 32) | eax };
            return _driver.DeviceIOControl(Interop.Ring0.IOCTL_OLS_WRITE_MSR, input);
        }

        public static byte ReadIoPort(uint port)
        {
            if (_driver == null)
                return 0;


            uint value = 0;
            _driver.DeviceIOControl(Interop.Ring0.IOCTL_OLS_READ_IO_PORT_BYTE, port, ref value);
            return (byte)(value & 0xFF);
        }

        public static void WriteIoPort(uint port, byte value)
        {
            if (_driver == null)
                return;


            WriteIoPortInput input = new() { PortNumber = port, Value = value };
            _driver.DeviceIOControl(Interop.Ring0.IOCTL_OLS_WRITE_IO_PORT_BYTE, input);
        }

        public static uint GetPciAddress(byte bus, byte device, byte function)
        {
            return (uint)(((bus & 0xFF) << 8) | ((device & 0x1F) << 3) | (function & 7));
        }

        public static bool ReadPciConfig(uint pciAddress, uint regAddress, out uint value)
        {
            if (_driver == null || (regAddress & 3) != 0)
            {
                value = 0;
                return false;
            }

            ReadPciConfigInput input = new() { PciAddress = pciAddress, RegAddress = regAddress };

            value = 0;
            return _driver.DeviceIOControl(Interop.Ring0.IOCTL_OLS_READ_PCI_CONFIG, input, ref value);
        }

        public static bool WritePciConfig(uint pciAddress, uint regAddress, uint value)
        {
            if (_driver == null || (regAddress & 3) != 0)
                return false;


            WritePciConfigInput input = new() { PciAddress = pciAddress, RegAddress = regAddress, Value = value };
            return _driver.DeviceIOControl(Interop.Ring0.IOCTL_OLS_WRITE_PCI_CONFIG, input);
        }

        public static bool ReadMemory<T>(ulong address, ref T buffer)
        {
            if (_driver == null)
                return false;


            ReadMemoryInput input = new() { Address = address, UnitSize = 1, Count = (uint)Marshal.SizeOf(buffer) };
            return _driver.DeviceIOControl(Interop.Ring0.IOCTL_OLS_READ_MEMORY, input, ref buffer);
        }

        public static bool ReadMemory<T>(ulong address, ref T[] buffer)
        {
            if (_driver == null)
                return false;


            ReadMemoryInput input = new() { Address = address, UnitSize = (uint)Marshal.SizeOf(typeof(T)), Count = (uint)buffer.Length };
            return _driver.DeviceIOControl(Interop.Ring0.IOCTL_OLS_READ_MEMORY, input, ref buffer);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct WriteMsrInput
        {
            public uint Register;
            public ulong Value;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct WriteIoPortInput
        {
            public uint PortNumber;
            public byte Value;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ReadPciConfigInput
        {
            public uint PciAddress;
            public uint RegAddress;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct WritePciConfigInput
        {
            public uint PciAddress;
            public uint RegAddress;
            public uint Value;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ReadMemoryInput
        {
            public ulong Address;
            public uint UnitSize;
            public uint Count;
        }
    }
}
