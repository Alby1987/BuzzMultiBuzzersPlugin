Imports PSE

Namespace Config

    Enum SelectedDevice As Integer
        None = 0
        Buzzers = 1
    End Enum

    Class CLR_Config
        Private SettingsFile As FreezeDataHelper = Nothing
        Public IniFolderPath As String = "inis"
        'Params
        Public EnableLog As Boolean = False
        Public Port1SelectedDevice As SelectedDevice = SelectedDevice.Buzzers
        Public Port1Options As IConfigData = Nothing

        Public Port2SelectedDevice As SelectedDevice = SelectedDevice.Buzzers
        Public Port2Options As IConfigData = Nothing

        Public Sub Configure()
            LoadConfig()

            Dim LogToggel As New SingleToggle("Enable Logging (Developer use only)")
            LogToggel.ValueEnabled = EnableLog

            Dim DeviceOptionsString As String() = {"No Device", "Auto/Buzz! Buzzers"}
            Dim Port1Select As New ComboConfig("Device in Port 1", DeviceOptionsString)
            Port1Select.ValueSelected = Port1SelectedDevice

            Dim Port2Select As New ComboConfig("Device in Port 2", DeviceOptionsString)
            Port2Select.ValueSelected = Port2SelectedDevice

            Dim ConForm As New DynamicConfigForm()
            ConForm.AddConfigControl(LogToggel)
            ConForm.AddConfigControl(Port1Select)
            ConForm.AddConfigControl(Port2Select)
            ConForm.ShowDialog()

            If ConForm.Accepted Then
                EnableLog = LogToggel.ValueEnabled
                Port1SelectedDevice = CType(Port1Select.ValueSelected, SelectedDevice)
                Port2SelectedDevice = CType(Port2Select.ValueSelected, SelectedDevice)
            End If

            ReloadDevConfig(1, Port1SelectedDevice)
            ReloadDevConfig(2, Port2SelectedDevice)

            SetLoggingState()
            BMB_USB.CreateDevices()

            SaveConfig()
        End Sub

        Protected Sub ReloadDevConfig(Port As Integer, DeviceType As SelectedDevice)
            Dim NewData As IConfigData = Nothing
            Select Case DeviceType
                Case SelectedDevice.None
                Case SelectedDevice.Buzzers
            End Select
            Select Case Port
                Case 1
                    Port1Options = NewData
                Case 2
                    Port2Options = NewData
            End Select
        End Sub

        Public Sub LoadConfig()
            Dim IniPath As String = IniFolderPath
            If IniFolderPath.EndsWith(IO.Path.DirectorySeparatorChar) Then
                IniPath += "USB_BMB.ini"
            Else
                IniPath += IO.Path.DirectorySeparatorChar + "USB_BMB.ini"
            End If

            SettingsFile = New FreezeDataHelper

            Try
                Dim Data As Byte() = IO.File.ReadAllBytes(IniPath)
                SettingsFile.FromBytes(Data, True)
            Catch ex As Exception
                SettingsFile = New FreezeDataHelper
                Log_Info("Failed to open " & IniPath)
                SaveConfig()
            End Try

            EnableLog = ReadBoolErrorSafe("Logging", EnableLog)
            Port1SelectedDevice = CType(ReadInt32ErrorSafe("Port1.SelectedDev", Port1SelectedDevice), SelectedDevice)
            Port2SelectedDevice = CType(ReadInt32ErrorSafe("Port2.SelectedDev", Port2SelectedDevice), SelectedDevice)

            ReloadDevConfig(1, Port1SelectedDevice)
            ReloadDevConfig(2, Port2SelectedDevice)

            SetLoggingState()
            BMB_USB.CreateDevices()
        End Sub
        Protected Sub SaveConfig()
            Dim IniPath As String = IniFolderPath
            If IniFolderPath.EndsWith(IO.Path.DirectorySeparatorChar) Then
                IniPath += "USB_BMB.ini"
            Else
                IniPath += IO.Path.DirectorySeparatorChar + "USB_BMB.ini"
            End If

            SettingsFile.SetBoolValue("Logging", EnableLog, True)
            Dim int As Integer = Port1SelectedDevice
            SettingsFile.SetInt32Value("Port1.SelectedDev", int, True)
            int = Port2SelectedDevice
            SettingsFile.SetInt32Value("Port2.SelectedDev", int, True)

            If Not IsNothing(Port1Options) Then
                Port1Options.SaveConfig(1, SettingsFile)
            End If
            If Not IsNothing(Port2Options) Then
                Port2Options.SaveConfig(2, SettingsFile)
            End If

            Dim Data As Byte() = SettingsFile.ToBytes(True)
            Try
                IO.File.WriteAllBytes(IniPath, Data)
            Catch ex As Exception
                Log_Error("Failed to open " & IniPath)
            End Try
        End Sub

        Private Sub SetLoggingState()
            If EnableLog Then
                CLR_PSE_PluginLog.SetFileLevel(SourceLevels.All)
            Else
                CLR_PSE_PluginLog.SetFileLevel(SourceLevels.Critical)
            End If
        End Sub

        Private Shared Sub Log_Error(str As String)
            CLR_PSE_PluginLog.WriteLine(TraceEventType.[Error], CInt(USBLogSources.PluginInterface), str)
        End Sub
        Private Shared Sub Log_Info(str As String)
            CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, CInt(USBLogSources.PluginInterface), str)
        End Sub
        Private Shared Sub Log_Verb(str As String)
            CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, CInt(USBLogSources.PluginInterface), str)
        End Sub

#Region "SetWrappers"
        Private Function ReadBoolErrorSafe(key As String, ByVal bool As Boolean) As Boolean
            Try
                Dim ret As Boolean = bool
                SettingsFile.SetBoolValue(key, ret, False)
                Return ret
            Catch ex As Exception
                Return bool
            End Try
        End Function
        Private Function ReadInt32ErrorSafe(key As String, ByVal si As Integer) As Integer
            Try
                Dim ret As Integer = si
                SettingsFile.SetInt32Value(key, ret, False)
                Return ret
            Catch ex As Exception
                Return si
            End Try
        End Function
#End Region

    End Class
End Namespace
