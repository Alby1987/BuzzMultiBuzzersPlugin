Imports System.Diagnostics.CodeAnalysis
Imports System.Runtime.InteropServices

Namespace Global.PSE
    Public Enum CLR_PSE_FreezeMode As Integer
        Load = 0
        Save = 1
        Size = 2
    End Enum

    <StructLayout(LayoutKind.Sequential)>
    Public Structure CLR_PSE_FreezeData
        Public size As Integer
        <SuppressMessage("Microsoft.Security", "CA2111:PointersShouldNotBeVisible")>
        Public data As IntPtr
    End Structure

    Friend Class CLR_PSE_FreezeDataMarshal
        Public Shared Function Load(ByVal frData As CLR_PSE_FreezeData) As Byte()
            Dim ret(frData.size - 1) As Byte
            Marshal.Copy(frData.data, ret, 0, frData.size)
            Return ret
        End Function
        Public Shared Sub Save(ByRef frData As CLR_PSE_FreezeData, frBytes As Byte())
            If (frData.size < frBytes.Length) Then
                Throw New InsufficientMemoryException()
            End If
            Marshal.Copy(frBytes, 0, frData.data, frData.size)
        End Sub
    End Class
End Namespace