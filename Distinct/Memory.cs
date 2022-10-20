using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Distinct {

    internal class Memory {

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, IntPtr lpNumberOfBytesWritten);

        private Process _process;

        public void GetProcess(string procName) {
            _process = Process.GetProcessesByName(procName)[0];
        }

        public IntPtr GetModuleBase(string moduleName) {
            if (moduleName.Contains(".exe"))  {
                if (_process.MainModule != null) {
                    return _process.MainModule.BaseAddress;
                }
            }

            foreach (ProcessModule module in _process.Modules) {
                if (module.ModuleName == moduleName) {
                    return module.BaseAddress;
                }
            } return IntPtr.Zero;
        }

        public IntPtr ReadPointer(IntPtr address, int offset) {
            byte[] buffer = new byte[4];
            ReadProcessMemory(_process.Handle, IntPtr.Add(address, offset), buffer, buffer.Length, IntPtr.Zero);
            return new IntPtr(BitConverter.ToInt32(buffer, 0));
        }

        public byte[] ReadBytes(IntPtr address, int offset, int bytes) {
            byte[] buffer = new byte[bytes];
            ReadProcessMemory(_process.Handle, IntPtr.Add(address, offset), buffer, buffer.Length, IntPtr.Zero);
            return buffer;
        }

        public void WriteBytes(IntPtr address, int offset, byte[] newBytes) {
            WriteProcessMemory(_process.Handle, IntPtr.Add(address, offset), newBytes, newBytes.Length, IntPtr.Zero);
        }

    }

}