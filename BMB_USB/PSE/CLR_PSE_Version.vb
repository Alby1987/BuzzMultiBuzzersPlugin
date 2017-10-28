Namespace Global.PSE
    Friend Structure CLR_PSE_Version_Plugin
        Private Enum CLR_PSE_Type_Version As Integer
            GS = &H6
            PAD = &H2
            SPU2 = &H5
            SPU2_NewIOP_DMA = &H6
            CDVD = &H5
            DEV9 = &H3
            DEV9_NewIOP_DMA = &H4
            'Not supported by PCSX2
            USB = &H3
            FW = &H2
        End Enum

        Private _VersionHi As Byte
        'Plugin = PATCH       
        'private CLR_Type_Version _VersionMid;  //Plugin = API Version 
        Private _VersionLo As Byte
        'Plugin = MAJOR(rev)  
        Private _VersionLower As Byte
        'Plugin = MINOR(build)
        Public Sub New(major As Byte, minor As Byte, patch As Byte)
            _VersionHi = patch
            '_VersionMid = 0;
            _VersionLo = major
            _VersionLower = minor
        End Sub

        Public ReadOnly Property Major() As Byte
            Get
                Return _VersionLo
            End Get
        End Property
        Public ReadOnly Property Minor() As Byte
            Get
                Return _VersionLower
            End Get
        End Property
        Public ReadOnly Property Patch() As Byte
            Get
                Return _VersionHi
            End Get
        End Property

        Public Function ToInt32(type As CLR_PSE_Type) As Integer
            Dim version As CLR_PSE_Type_Version = 0
            Select Case type
                Case CLR_PSE_Type.GS
                    version = CLR_PSE_Type_Version.GS
                    Exit Select
                Case CLR_PSE_Type.PAD
                    version = CLR_PSE_Type_Version.PAD
                    Exit Select
                Case CLR_PSE_Type.SPU2
                    version = CLR_PSE_Type_Version.SPU2
                    Exit Select
                Case CLR_PSE_Type.CDVD
                    version = CLR_PSE_Type_Version.CDVD
                    Exit Select
                Case CLR_PSE_Type.DEV9
                    version = CLR_PSE_Type_Version.DEV9
                    Exit Select
                Case CLR_PSE_Type.USB
                    version = CLR_PSE_Type_Version.USB
                    Exit Select
                Case CLR_PSE_Type.FW
                    version = CLR_PSE_Type_Version.FW
                    Exit Select
                Case Else
                    Exit Select
            End Select
            Return (CInt(Patch) << 24 Or version << 16 Or CInt(Major) << 8 Or Minor)
        End Function
    End Structure

    Friend Structure CLR_PSE_Version_PCSX2
        Private _VersionHi As Byte
        'PCSX2 = MAJOR
        Private _VersionMid As Byte
        'PCSX2 = MINOR
        Private _VersionLo As Byte
        'PCSX2 = PATCH
        'private byte _VersionLower; //PCSX2 = UNUSED
        Public Sub New(major As Byte, minor As Byte, patch As Byte)
            _VersionHi = major
            _VersionMid = minor
            '_VersionLower = 0;
            _VersionLo = patch
        End Sub

        Public ReadOnly Property Major() As Byte
            Get
                Return _VersionHi
            End Get
        End Property
        Public ReadOnly Property Minor() As Byte
            Get
                Return _VersionMid
            End Get
        End Property
        Public ReadOnly Property Patch() As Byte
            Get
                Return _VersionLo
            End Get
        End Property

        Public Function ToInt32() As Integer
            Return (_VersionHi << 24 Or _VersionMid << 16 Or _VersionLo << 8 Or 0)
        End Function
        Public Shared Function ToVersion(ver As Integer) As CLR_PSE_Version_PCSX2
            Dim major As Byte = CByte((ver >> 24) And &HFF)
            Dim minor As Byte = CByte((ver >> 16) And &HFF)
            Dim patch As Byte = CByte((ver >> 8) And &HFF)
            'least significant byte unused
            Return New CLR_PSE_Version_PCSX2(major, minor, patch)
        End Function

        Public Shared Operator <(ver1 As CLR_PSE_Version_PCSX2, ver2 As CLR_PSE_Version_PCSX2) As Boolean
            If ver1.Major < ver2.Major Then
                Return True
            ElseIf Not (ver1.Major = ver2.Major) Then
                Return False
            End If
            If ver1.Minor < ver2.Minor Then
                Return True
            ElseIf Not (ver1.Minor = ver2.Minor) Then
                Return False
            End If
            If ver1.Patch < ver2.Patch Then
                Return True
            End If
            Return False
        End Operator
        Public Shared Operator >(ver1 As CLR_PSE_Version_PCSX2, ver2 As CLR_PSE_Version_PCSX2) As Boolean
            If ver1.Major > ver2.Major Then
                Return True
            ElseIf Not (ver1.Major = ver2.Major) Then
                Return False
            End If
            If ver1.Minor > ver2.Minor Then
                Return True
            ElseIf Not (ver1.Minor = ver2.Minor) Then
                Return False
            End If
            If ver1.Patch > ver2.Patch Then
                Return True
            End If
            Return False
        End Operator

        Public Shared Operator =(ver1 As CLR_PSE_Version_PCSX2, ver2 As CLR_PSE_Version_PCSX2) As Boolean
            Return ver1.Major = ver2.Major And ver1.Minor = ver2.Minor And ver1.Patch = ver2.Patch
        End Operator
        Public Shared Operator <>(ver1 As CLR_PSE_Version_PCSX2, ver2 As CLR_PSE_Version_PCSX2) As Boolean
            Return Not (ver1 = ver2)
        End Operator

        Public Shared Operator >=(ver1 As CLR_PSE_Version_PCSX2, ver2 As CLR_PSE_Version_PCSX2) As Boolean
            Return (ver1 > ver2) OrElse (ver1 = ver2)
        End Operator
        Public Shared Operator <=(ver1 As CLR_PSE_Version_PCSX2, ver2 As CLR_PSE_Version_PCSX2) As Boolean
            Return (ver1 < ver2) OrElse (ver1 = ver2)
        End Operator

        Public Overrides Function Equals(obj As Object) As Boolean
            If Not (TypeOf obj Is CLR_PSE_Version_PCSX2) Then
                Return False
            End If

            Return Me = CType(obj, CLR_PSE_Version_PCSX2)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return ToInt32()
        End Function
    End Structure
End Namespace
