namespace _4RTools.Utils
{
    using System;
    using System.Runtime.InteropServices;

    public class FxHook : IDisposable
    {
        const int nBytes = 5;

        private IntPtr processHandle;
        IntPtr addr;
        Protection old;
        byte[] src = new byte[5];
        byte[] dst = new byte[5];

        public FxHook(IntPtr handle, IntPtr source, IntPtr destination)
        {
            processHandle = handle;
            VirtualProtectEx(source, nBytes, Protection.PAGE_EXECUTE_READWRITE, out old);
            int written = 0;
            WriteProcessMemory(handle, source, src, nBytes, ref written);
            // Marshal.Copy(source, src, 0, nBytes);
            dst[0] = 0xE9;
            var dx = BitConverter.GetBytes((int)destination - (int)source - nBytes);
            Array.Copy(dx, 0, dst, 1, nBytes-1);
            addr = source;
        }
        public FxHook(IntPtr handle, IntPtr source, Delegate destination) :
            this(handle, source, Marshal.GetFunctionPointerForDelegate(destination)) {
        }
	
        public void Install() {
            int written = 0;
            WriteProcessMemory(processHandle, addr, dst, nBytes, ref written);
            // Marshal.Copy(dst, 0, addr, nBytes);
        }

        public void Uninstall() {
            int written = 0;
            WriteProcessMemory(processHandle, addr, src, nBytes, ref written);
            // Marshal.Copy(src, 0, addr, nBytes);
        }

        public void Dispose() {
            Uninstall();
            Protection x;
            VirtualProtectEx(addr, nBytes, old, out x);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, Protection flNewProtect, out Protection lpflOldProtect);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool VirtualProtectEx(IntPtr lpAddress, uint dwSize, Protection flNewProtect, out Protection lpflOldProtect);
        
        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);

        public enum Protection {
            PAGE_NOACCESS = 0x01,
            PAGE_READONLY = 0x02,
            PAGE_READWRITE = 0x04,
            PAGE_WRITECOPY = 0x08,
            PAGE_EXECUTE = 0x10,
            PAGE_EXECUTE_READ = 0x20,
            PAGE_EXECUTE_READWRITE = 0x40,
            PAGE_EXECUTE_WRITECOPY = 0x80,
            PAGE_GUARD = 0x100,
            PAGE_NOCACHE = 0x200,
            PAGE_WRITECOMBINE = 0x400
        }
    }
}