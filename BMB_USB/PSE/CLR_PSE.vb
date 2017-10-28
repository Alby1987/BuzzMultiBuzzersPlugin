Imports System.Runtime.InteropServices
Imports Plugin = BMBUSB.BMB_USB
Imports CLRUSB

Namespace Global.PSE
    'Multi-in-one is not supported
    Public Enum CLR_PSE_Type As Integer
        GS = &H1
        PAD = &H2
        SPU2 = &H4
        CDVD = &H8
        DEV9 = &H10
        USB = &H20
        FW = &H40
    End Enum

    Public Class CLR_PSE
#Region "NativeExport"
        <DllExport("PS2EgetLibName", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function nat_PS2EgetLibName() As <MarshalAs(UnmanagedType.LPStr)> String
            Return PS2EgetLibName()
        End Function
        <DllExport("PS2EgetLibType", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function nat_PS2EgetLibType() As CLR_PSE_Type
            Return PS2EgetLibType()
        End Function
        <DllExport("PS2EgetLibVersion2", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Function nat_PS2EgetLibVersion2(type As CLR_PSE_Type) As Integer
            Return PS2EgetLibVersion2(type)
        End Function
        'Windows only
        <DllExport("PS2EsetEmuVersion", CallingConvention:=CallingConvention.StdCall)>
        Private Shared Sub nat_PS2EsetEmuVersion(name As IntPtr, version As Integer)
            PS2EsetEmuVersion(name, version)
        End Sub
#End Region

        'EMU Version (Windows only)
        Private Shared m_emuName As String = ""
        Private Shared m_emuVersion As New CLR_PSE_Version_PCSX2(255, 255, 255)
        Friend Shared ReadOnly Property EmuName() As String
            Get
                Return m_emuName
            End Get
        End Property
        Friend Shared ReadOnly Property EmuVersion() As CLR_PSE_Version_PCSX2
            Get
                Return m_emuVersion
            End Get
        End Property

        Public Shared Function PS2EgetLibName() As String
            Return Plugin.Name
        End Function

        Public Shared Function PS2EgetLibType() As CLR_PSE_Type
            'Last remaining constant in this class
            Return CLR_PSE_Type.USB
        End Function

        Public Shared Function PS2EgetLibVersion2(type As CLR_PSE_Type) As Integer
            Dim pluginVer As Version = GetType(Plugin).Assembly.GetName().Version
            Dim version As New CLR_PSE_Version_Plugin(CByte(pluginVer.Major), CByte(pluginVer.Minor), CByte(pluginVer.Build))
            Return version.ToInt32(type)
        End Function

        'Only Used on Windows
        Public Shared Sub PS2EsetEmuVersion(name As IntPtr, version As Integer)
            If name = IntPtr.Zero Then
                m_emuName = ""
            Else
                m_emuName = Marshal.PtrToStringAnsi(name)
            End If
            m_emuVersion = CLR_PSE_Version_PCSX2.ToVersion(version)
        End Sub
    End Class
End Namespace