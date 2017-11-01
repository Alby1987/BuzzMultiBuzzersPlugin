Imports BMBUSB.USB
Imports System.Runtime.InteropServices
Imports PSE

Class BMB_USB

#If DEBUG Then
    Private Const libraryName As String = "Buzz! MultiBuzzers Plugin DEBUG"
#Else
    Private Const libraryName As String = "Buzz! MultiBuzzers Plugin"
#End If

    Private Shared LogFolderPath As String = "logs"
    Private Shared IniFolderPath As String = "inis"
    Private Shared PluginConf As New Config.CLR_Config
    '2195220e0524e7c289229b36ee1ef2da8dedaf01

    Private Const SaveStateV As Int32 = 3

    Shared qemu_ohci As OHCI.OHCI_State = Nothing
    Shared usb_device1 As USB_Device = Nothing
    Shared usb_device2 As USB_Device = Nothing

    Shared sudoPtr As CLR_PSE_SudoPtr
    Shared iopRam As IntPtr = IntPtr.Zero
    Shared irqHandle As GCHandle

    Public Shared ReadOnly Property Name() As String
        Get
            Return libraryName
        End Get
    End Property

    Private Shared Sub Reset()
        If (Not IsNothing(qemu_ohci)) Then
            qemu_ohci.hard_reset()
            remaining = 0
        End If
    End Sub
    Private Shared Sub DestroyDevices()
        '//FIXME something throws an null ptr exception?
        'Destroy in reverse order
        If (Not IsNothing(qemu_ohci) AndAlso Not IsNothing(qemu_ohci.rhport(PLAYER_TWO_PORT).port.dev)) Then
            qemu_ohci.rhport(PLAYER_TWO_PORT).port.dev.handle_destroy()
            qemu_ohci.rhport(PLAYER_TWO_PORT).port.dev = Nothing
        ElseIf (Not IsNothing(usb_device2)) Then
            usb_device2.handle_destroy()
        End If

        If (Not IsNothing(qemu_ohci) AndAlso Not IsNothing(qemu_ohci.rhport(PLAYER_ONE_PORT).port.dev)) Then
            qemu_ohci.rhport(PLAYER_ONE_PORT).port.dev.handle_destroy()
            qemu_ohci.rhport(PLAYER_ONE_PORT).port.dev = Nothing
        ElseIf (Not IsNothing(usb_device1)) Then '//maybe redundant
            usb_device1.handle_destroy()
        End If

        usb_device1 = Nothing
        usb_device2 = Nothing
    End Sub

    Public Shared Sub CreateDevices()
        If IsNothing(qemu_ohci) Then
            Return
        End If
        DestroyDevices()
        'switch for p0 and p1, which will recreate the devices
        Dim pConfig As Config.CLR_Config = DirectCast(PluginConf, Config.CLR_Config)
        Select Case pConfig.Port1SelectedDevice
            Case Config.SelectedDevice.Buzzers
                usb_device1 = New USB_Buzzer(0)
            Case Else
                'already null
        End Select

        Select Case pConfig.Port2SelectedDevice
            Case Config.SelectedDevice.Buzzers
                usb_device2 = New USB_Buzzer(1)
            Case Else
                'already null
        End Select

        qemu_ohci.rhport(PLAYER_ONE_PORT).port.attach(usb_device1)
        qemu_ohci.rhport(PLAYER_TWO_PORT).port.attach(usb_device2)
    End Sub

    Private Shared Sub LogInit()
        Dim logSources As New Dictionary(Of UShort, String)()
        Dim sources As IEnumerable(Of USBLogSources) = [Enum].GetValues(GetType(USBLogSources)).Cast(Of USBLogSources)()

        For Each source As USBLogSources In sources
            logSources.Add(CUShort(source), source.ToString())
        Next

        CLR_PSE_PluginLog.Open(LogFolderPath, "USB_BMB.log", "USB_BMB", logSources)

        For Each source As USBLogSources In sources
            CLR_PSE_PluginLog.SetSourceLogLevel(SourceLevels.All, (CUShort(source)))
        Next

        CLR_PSE_PluginLog.SetSourceUseStdOut(False, USBLogSources.PluginInterface)
        CLR_PSE_PluginLog.SetSourceUseStdOut(False, USBLogSources.USBInterface)
        CLR_PSE_PluginLog.SetSourceUseStdOut(False, USBLogSources.USBPort)
        CLR_PSE_PluginLog.SetSourceUseStdOut(False, USBLogSources.USBDevice)
        CLR_PSE_PluginLog.SetSourceUseStdOut(True, USBLogSources.USBBuzzers)
        CLR_PSE_PluginLog.SetSourceUseStdOut(False, USBLogSources.OHCI)
        ''USB code present in plugin interface layer
    End Sub

    'Private Shared Sub ErrorCatcher3Million(sender As Object, e As UnhandledExceptionEventArgs)
    '    CLR_PSE_PluginLog.MsgBoxError(CType(e.ExceptionObject, Exception))
    'End Sub

    Public Shared Function Init() As Int32
        Try
            LogInit()
            PluginConf.LoadConfig()
            Dim pluginVer As Version = GetType(BMB_USB).Assembly.GetName().Version
            Log_Info(Name & " plugin version " & pluginVer.Major & "." & pluginVer.Minor)
            Log_Info("Initializing " & Name)

            'Initialize here.
            qemu_ohci = New OHCI.OHCI_State(&H1F801600, 2)
            CreateDevices()
            'AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf ErrorCatcher3Million
            Return 0
        Catch e As Exception
            CLR_PSE_PluginLog.MsgBoxError(e)
            Return -1
        End Try
    End Function
    Public Shared Sub Shutdown()
        Try
            'RemoveHandler AppDomain.CurrentDomain.UnhandledException, AddressOf ErrorCatcher3Million
            'Yes, we close things in the Shutdown routine, and
            'don't do anything in the close routine.
            DestroyDevices()
            CLR_PSE_PluginLog.Close()
            'The UnmanagedMemoryStream 
            'currently will do NOTHING to free this memory
            If Not IsNothing(qemu_ohci.Ram) Then
                qemu_ohci.Ram.Close()
                qemu_ohci.Ram.Dispose()
                qemu_ohci.Ram = Nothing
            End If
            If Not IsNothing(sudoPtr) Then
                sudoPtr.Close()
                sudoPtr.Dispose() 'Won't free Mem
                sudoPtr = Nothing
            End If
            If irqHandle.IsAllocated Then
                'allow garbage collection
                irqHandle.Free()
            End If
        Catch e As Exception
            CLR_PSE_PluginLog.MsgBoxError(e)
            Throw
        End Try
    End Sub
    Public Shared Function Open(hWnd As IntPtr) As Int32
        Try
            Log_Info("Opening " & CLR_PSE.PS2EgetLibName())

            If (Utils.IsAWindow(hWnd)) Then
                ''OK
            ElseIf (hWnd <> IntPtr.Zero AndAlso Utils.IsAWindow(Marshal.ReadIntPtr(hWnd))) Then
                hWnd = Marshal.ReadIntPtr(hWnd)
            Else
                Throw New Exception("Invalid hWnd")
            End If

            '// Take care of anything else we need on opening, other then initialization.

            If (Not IsNothing(usb_device1)) Then
                usb_device1.open(hWnd)
            End If
            If (Not IsNothing(usb_device2)) Then
                usb_device2.open(hWnd)
            End If
            Return 0
        Catch e As Exception
            CLR_PSE_PluginLog.MsgBoxError(e)
            Return -1
        End Try
    End Function
    Public Shared Sub Close()
        Try
            Log_Info("Closing " & CLR_PSE.PS2EgetLibName())
            'Close in reverse Order
            If (Not IsNothing(usb_device2)) Then
                usb_device2.close()
            End If

            If (Not IsNothing(usb_device1)) Then
                usb_device1.close()
            End If
        Catch e As Exception
            CLR_PSE_PluginLog.MsgBoxError(e)
            Throw
        End Try
    End Sub
    Public Shared Function USBread8(addr As UInt32) As Byte
        Log_USB_Error("(USB) Invalid 8 bit read at address " & addr.ToString("X"))
        Return 0
    End Function
    Public Shared Function USBread16(addr As UInt32) As UInt16
        Log_USB_Error("(USB) Invalid 16 bit read at address " & addr.ToString("X"))
        Return 0
    End Function
    Public Shared Function USBread32(addr As UInt32) As UInt32
        Try
            Dim returnval As UInt32 = qemu_ohci.mem_read(addr)
            Log_USB_Verb("USB:R32: 32 bit read  at address :0x" & addr.ToString("X8") & ": got  :" & "0x" & returnval.ToString("X8"))
            Return returnval
        Catch e As Exception
            CLR_PSE_PluginLog.MsgBoxError(e)
            Throw
        End Try
    End Function

    Public Shared Sub USBwrite8(addr As UInt32, value As Byte)
        Log_USB_Error("(USB) Invalid 8 bit write at address " & addr.ToString("X"))
    End Sub
    Public Shared Sub USBwrite16(addr As UInt32, value As UInt16)
        Log_USB_Error("(USB) Invalid 16 bit write at address " & addr.ToString("X"))
    End Sub
    Public Shared Sub USBwrite32(addr As UInt32, value As UInt32)
        Try
            Log_USB_Verb("USB:W32: 32 bit write at address :0x" & addr.ToString("X8") & ": with :0x" & value.ToString("X8"))
            qemu_ohci.mem_write(addr, value)
        Catch e As Exception
            CLR_PSE_PluginLog.MsgBoxError(e)
            Throw
        End Try
    End Sub
    Public Shared Sub USBirqCallback(callback As CLR_PSE_Callbacks.CLR_CyclesCallback)
        qemu_ohci.USBirq = callback
    End Sub

    Public Shared Function _USBirqHandler() As Int32 'void
        '// This is our USB irq handler, so if an interrupt gets triggered,
        '// deal with it here.
        Log_USB_Verb("USB:IRQ: irq Called")
        If GetIrq() > 0 Then
            Return 1
        End If
        Return 0
    End Function

    Public Shared Function GetIrq() As Int32
        Dim irq As Int32 = 0
        Dim pp As UInt32 = qemu_ohci.GetIrqAddr()
        If Not IsNothing(usb_device1) Then
            If CType(usb_device1, USB_Buzzer).getIrq > 0 Then
                irq = 1
            End If
        End If
        If Not IsNothing(usb_device2) Then
            If CType(usb_device2, USB_Buzzer).getIrq > 0 Then
                irq = 1
            End If
        End If
        Return irq
    End Function

    Public Shared Function USBirqHandler() As CLR_PSE_Callbacks.CLR_IRQHandler
        Try
            ' Pass our handler to pcsx2.
            If irqHandle.IsAllocated Then
                'allow garbage collection
                irqHandle.Free()
            End If
            Log_Verb("USB:IRQ: Get irq Handler")
            Dim fp As New CLR_PSE_Callbacks.CLR_IRQHandler(AddressOf _USBirqHandler)
            irqHandle = GCHandle.Alloc(fp)
            'prevent GC
            Return fp
        Catch e As Exception
            CLR_PSE_PluginLog.MsgBoxError(e)
            Throw
        End Try
    End Function

    Public Shared Sub USBsetRAM(mem As IntPtr)
        Try
            Log_USB_Info("USB:*SR: Setting ram")
            iopRam = mem

            If Not IsNothing(qemu_ohci.Ram) Then
                qemu_ohci.Ram.Close()
                qemu_ohci.Ram.Dispose()
                qemu_ohci.Ram = Nothing
            End If
            If Not IsNothing(sudoPtr) Then
                sudoPtr.Close()
                sudoPtr.Dispose() 'Won't free Mem
                sudoPtr = Nothing
            End If

            sudoPtr = New CLR_PSE_SudoPtr(mem)
            sudoPtr.Initialize(IOPMEMSIZE)
            Dim strm As New IO.UnmanagedMemoryStream(sudoPtr, 0, IOPMEMSIZE, IO.FileAccess.ReadWrite)

            qemu_ohci.Ram = strm 'IOPram
            Reset()
        Catch e As Exception
            CLR_PSE_PluginLog.MsgBoxError(e)
            Throw
        End Try
    End Sub
    Public Shared Sub SetSettingsDir(dir As String)
        PluginConf.IniFolderPath = dir
    End Sub
    Public Shared Sub SetLogDir(dir As String)
        LogFolderPath = dir
        'CLR_PSE_PluginLog.Close()
        'LogInit()
    End Sub

    Shared remaining As Int64 = 0
    Public Shared Sub USBasync(cycles As UInteger)
        Try
            If FULL_DEBUG Then
                ''Should output to something else?
                Log_USB_Verb("USB:ASC: Async :" & cycles)
            End If

            remaining += cycles
            'Overflow check (like that will ever happen)
            If Int64.MaxValue - remaining < qemu_ohci.clocks Then
                qemu_ohci.clocks = 0
            Else
                qemu_ohci.clocks += remaining
            End If
            'end overflow check

            If (qemu_ohci.eof_timer > 0) Then
                While (remaining >= qemu_ohci.eof_timer) And (qemu_ohci.eof_timer > 0)
                    remaining -= Convert.ToInt64(qemu_ohci.eof_timer)
                    qemu_ohci.eof_timer = 0
                    qemu_ohci.frame_boundary()
                End While
                If ((remaining > 0) AndAlso qemu_ohci.eof_timer > 0) Then
                    Dim m As Int64 = CLng(qemu_ohci.eof_timer)
                    If (remaining < m) Then
                        m = remaining
                    End If
                    qemu_ohci.eof_timer = CULng(qemu_ohci.eof_timer - m)
                    remaining -= m
                End If
            Else
                remaining = 0 ' I assume this won't break anything
            End If
        Catch e As Exception
            CLR_PSE_PluginLog.MsgBoxError(e)
            Throw
        End Try
    End Sub

    Const PaddedSize As Integer = 10 * 1000 '~10kb a little more then 2* what we use for 1 port

    Public Shared Function Freeze(mode As CLR_PSE_FreezeMode, ByRef data As CLR_PSE_FreezeData) As Int32
        Try
            Select Case mode
                Case CLR_PSE_FreezeMode.Size
                    data.size = FreezeSize()
                Case CLR_PSE_FreezeMode.Save
                    CLR_PSE_FreezeDataMarshal.Save(data, FreezeSave())
                Case CLR_PSE_FreezeMode.Load
                    Return FreezeLoad(CLR_PSE_FreezeDataMarshal.Load(data))
            End Select
            Return 0
        Catch e As Exception
            CLR_PSE_PluginLog.MsgBoxError(e)
            Return -1
        End Try
    End Function

    Private Shared Function FreezeSave() As Byte()
        Log_Info("SavingState")
        Dim sd As New FreezeDataHelper()
        sd.SetInt32Value("Version", SaveStateV, True)
        sd.SetInt64Value("OHCI.remaining", remaining, True)
        qemu_ohci.Freeze(sd, True)

        Dim sdBytes As Byte() = sd.ToBytes()

        Log_Info("Pre Padding Size = " & sdBytes.Length & " bytes")

        Dim PaddedData(PaddedSize - 1) As Byte
        Utils.memcpy(PaddedData, 0, sdBytes, 0, sdBytes.Length)
        Return PaddedData
    End Function

    Private Shared Function FreezeLoad(data As Byte()) As Int32
        Log_Info("LoadingState")
        Log_Info("Size = " & data.Length & " bytes")
        Try
            If Not IsNothing(qemu_ohci) AndAlso (IsNothing(qemu_ohci.Ram) And Not (iopRam = IntPtr.Zero)) Then
                Log_Error("HACK, Reloading lost pointer to IOP")
                'When a diffrent type of plugin gets swapped, this plugin gets shutdown/reloaded
                'during shutdown, the CLR plugin gets unloaded And loses its refrence to 
                'IOP memory
                USBsetRAM(iopRam)
            End If

            Dim sd As New FreezeDataHelper()
            sd.FromBytes(data)
            Dim ssV As Int32 = 0
            sd.SetInt32Value("Version", ssV, False)
            If ssV <> SaveStateV Then
                Log_Error("Warning, SaveState verison does not match")
            End If
            sd.SetInt64Value("OHCI.remaining", remaining, False)
            qemu_ohci.Freeze(sd, False)

            Return 1
        Catch err As Exception
            Log_Error("Load Failed: " & err.Message & err.StackTrace)
            Return -1
        End Try
    End Function
    Private Shared Function FreezeSize() As Int32
        Log_Info("SizingState")
        'Dim data As Byte() = FreezeSave()
        'PluginLog.ErrorWriteLine("Size = " & data.Length & " bytes")
        'Return data.Length
        Return PaddedSize
    End Function

    Public Shared Sub Configure()
        PluginConf.Configure()
    End Sub
    Public Shared Sub About()
        MsgBox("A plugin using CLR")
    End Sub
    Public Shared Function Test() As Int32
        '// 0 if the plugin works, non-0 if it doesn't
        Return 0
    End Function

    Private Shared Sub Log_Error(str As String)
        CLR_PSE_PluginLog.WriteLine(TraceEventType.[Error], CInt(USBLogSources.PluginInterface), str)
    End Sub
    Private Shared Sub Log_Info(str As String)
        CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, CInt(USBLogSources.PluginInterface), str)
    End Sub
    Private Shared Sub Log_Verb(str As String)
        CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, CInt(USBLogSources.PluginInterface), str)
    End Sub

    Private Shared Sub Log_USB_Error(str As String)
        CLR_PSE_PluginLog.WriteLine(TraceEventType.[Error], (USBLogSources.USBInterface), str)
    End Sub
    Private Shared Sub Log_USB_Info(str As String)
        CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (USBLogSources.USBInterface), str)
    End Sub
    Private Shared Sub Log_USB_Verb(str As String)
        CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (USBLogSources.USBInterface), str)
    End Sub
End Class
