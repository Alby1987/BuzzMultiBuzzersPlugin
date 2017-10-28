Imports System.Runtime.InteropServices
Imports Plugin = BMBUSB.BMB_USB
Imports CLRUSB

Namespace Global.PSE
    Public Class CLR_PSE_USB
#Region "NativeExport"
        <DllExport("USBinit", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function nat_USBinit() As Int32
            Return USBinit()
        End Function
        <DllExport("USBopen", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function nat_USBopen(pDsp As IntPtr) As Int32
            Return USBopen(pDsp)
        End Function
        <DllExport("USBclose", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Sub nat_USBclose()
            USBclose()
        End Sub
        <DllExport("USBshutdown", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Sub nat_USBshutdown()
            USBshutdown()
        End Sub
        <DllExport("USBsetSettingsDir", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Sub nat_USBsetSettingsDir(dir As String)
            USBsetSettingsDir(dir)
        End Sub
        <DllExport("USBsetLogDir", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Sub nat_USBsetLogDir(dir As String)
            USBsetLogDir(dir)
        End Sub

        <DllExport("USBread8", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function nat_USBread8(addr As UInt32) As Byte
            Return USBread8(addr)
        End Function
        <DllExport("USBread16", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function nat_USBread16(addr As UInt32) As UInt16
            Return USBread16(addr)
        End Function
        <DllExport("USBread32", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function nat_USBread32(addr As UInt32) As UInt32
            Return USBread32(addr)
        End Function
        <DllExport("USBwrite8", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Sub nat_USBwrite8(addr As UInt32, value As Byte)
            USBwrite8(addr, value)
        End Sub
        <DllExport("USBwrite16", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Sub nat_USBwrite16(addr As UInt32, value As UInt16)
            USBwrite16(addr, value)
        End Sub
        <DllExport("USBwrite32", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Sub nat_USBwrite32(addr As UInt32, value As UInt32)
            USBwrite32(addr, value)
        End Sub
        <DllExport("USBsetRAM", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Sub nat_USBsetRAM(mem As IntPtr)
            USBsetRAM(mem)
        End Sub

        <DllExport("USBasync", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Sub nat_USBasync(cycles As UInt32)
            USBasync(cycles)
        End Sub

        <DllExport("USBirqCallback", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Sub nat_USBirqCallback(callback As CLR_PSE_Callbacks.CLR_CyclesCallback)
            USBirqCallback(callback)
        End Sub
        <DllExport("USBirqHandler", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function nat_USBirqHandler() As CLR_PSE_Callbacks.CLR_IRQHandler
            Return USBirqHandler()
        End Function

        <DllExport("USBfreeze", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function nat_USBfreeze(mode As CLR_PSE_FreezeMode, ByRef data As CLR_PSE_FreezeData) As Int32
            Return USBfreeze(mode, data)
        End Function
        <DllExport("USBconfigure", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Sub nat_USBconfigure()
            USBconfigure()
        End Sub
        <DllExport("USBabout", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Sub nat_USBabout()
            USBabout()
        End Sub
        <DllExport("USBtest", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function nat_USBtest() As Int32
            Return USBtest()
        End Function
#End Region

        Public Shared Function USBinit() As Int32
            Return Plugin.Init()
        End Function
        Public Shared Function USBopen(pDsp As IntPtr) As Int32
            Return Plugin.Open(pDsp)
        End Function
        Public Shared Sub USBclose()
            Plugin.Close()
        End Sub
        Public Shared Sub USBshutdown()
            Plugin.Shutdown()
        End Sub
        Public Shared Sub USBsetSettingsDir(dir As String)
            Plugin.SetSettingsDir(dir)
        End Sub
        Public Shared Sub USBsetLogDir(dir As String)
            Plugin.SetLogDir(dir)
        End Sub

        Public Shared Function USBread8(addr As UInt32) As Byte
            Return Plugin.USBread8(addr)
        End Function
        Public Shared Function USBread16(addr As UInt32) As UInt16
            Return Plugin.USBread16(addr)
        End Function
        Public Shared Function USBread32(addr As UInt32) As UInt32
            Return Plugin.USBread32(addr)
        End Function
        Public Shared Sub USBwrite8(addr As UInt32, value As Byte)
            Plugin.USBwrite8(addr, value)
        End Sub
        Public Shared Sub USBwrite16(addr As UInt32, value As UInt16)
            Plugin.USBwrite16(addr, value)
        End Sub
        Public Shared Sub USBwrite32(addr As UInt32, value As UInt32)
            Plugin.USBwrite32(addr, value)
        End Sub

        Public Shared Sub USBsetRAM(mem As IntPtr)
            Plugin.USBsetRAM(mem)
        End Sub

        Public Shared Sub USBasync(cycles As UInt32)
            Plugin.USBasync(cycles)
        End Sub

        Public Shared Sub USBirqCallback(callback As CLR_PSE_Callbacks.CLR_CyclesCallback)
            Plugin.USBirqCallback(callback)
        End Sub
        Public Shared Function USBirqHandler() As CLR_PSE_Callbacks.CLR_IRQHandler
            Return Plugin.USBirqHandler()
        End Function

        Public Shared Function USBfreeze(mode As CLR_PSE_FreezeMode, ByRef data As CLR_PSE_FreezeData) As Int32
            Return Plugin.Freeze(mode, data)
        End Function
        Public Shared Sub USBconfigure()
            Plugin.Configure()
        End Sub
        Public Shared Sub USBabout()
            Plugin.About() 'When is this called?
        End Sub
        Public Shared Function USBtest() As Int32
            Return Plugin.Test()
        End Function
    End Class
End Namespace